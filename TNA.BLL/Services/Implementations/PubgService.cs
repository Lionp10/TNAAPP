using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using TNA.BLL.Config;
using TNA.BLL.DTOs;
using TNA.BLL.Services.Interfaces;
using TNA.DAL.Entities;
using TNA.DAL.Repositories.Interfaces;
using System.Globalization;

namespace TNA.BLL.Services.Implementations
{
    public partial class PubgService : IPubgService
    {
        private readonly IClanRepository _clanRepository;
        private readonly IClanMemberRepository _clanMemberRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IPlayerMatchRepository _playerMatchRepository;
        private readonly IPlayerLifetimeRepository _playerLifetimeRepository;
        private readonly IRecentGameStatsRepository _recentGamesStatsRepository;
        private readonly IMapper _mapper;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PubgOptions _options;
        private readonly ILogger<PubgService> _logger;

        private readonly TimeSpan _playersRateInterval = TimeSpan.FromSeconds(6);
        private readonly SemaphoreSlim _playersRateSemaphore = new(1, 1);
        private DateTime _lastPlayersRequest = DateTime.MinValue;

        const string clanId = "clan.eb61293ee8c94a53be40c0d23d6e118d";

        public PubgService(
            IClanRepository clanRepository,
            IClanMemberRepository clanMemberRepository,
            IMatchRepository matchRepository,
            IPlayerMatchRepository playerMatchRepository,
            IPlayerLifetimeRepository playerLifetimeRepository,
            IRecentGameStatsRepository recentGamesStatsRepository,
            IMapper mapper,
            IHttpClientFactory httpClientFactory,
            IOptions<PubgOptions> options,
            ILogger<PubgService> logger)
        {
            _clanRepository = clanRepository ?? throw new ArgumentNullException(nameof(clanRepository));
            _clanMemberRepository = clanMemberRepository ?? throw new ArgumentNullException(nameof(clanMemberRepository));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _playerMatchRepository = playerMatchRepository ?? throw new ArgumentNullException(nameof(playerMatchRepository));
            _playerLifetimeRepository = playerLifetimeRepository ?? throw new ArgumentNullException(nameof(playerLifetimeRepository));
            _recentGamesStatsRepository = recentGamesStatsRepository ?? throw new ArgumentNullException(nameof(recentGamesStatsRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ClanDTO?> GetOrUpdateClanAsync(CancellationToken cancellationToken = default)
        {
            var clan = await _clanRepository.GetByClanIdAsync(clanId, cancellationToken);
            if (clan is null)
            {
                _logger.LogWarning("GetOrUpdateClanAsync: clan with ClanId {ClanId} not found in DB.", clanId);
                return null;
            }

            if (clan.DateOfUpdate.Date != DateTime.Now.Date)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

                    var requestUrl = $"{_options.BaseUrl}/clans/{clan.ClanId}";
                    _logger.LogInformation("Requesting PUBG API for clan {ClanId} at {Url}", clan.ClanId, requestUrl);

                    using var response = await client.GetAsync(requestUrl, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                        if (doc.RootElement.TryGetProperty("data", out var data) &&
                            data.TryGetProperty("attributes", out var attributes))
                        {
                            if (attributes.TryGetProperty("clanName", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                                clan.ClanName = nameEl.GetString() ?? clan.ClanName;

                            if (attributes.TryGetProperty("clanTag", out var tagEl) && tagEl.ValueKind == JsonValueKind.String)
                                clan.ClanTag = tagEl.GetString() ?? clan.ClanTag;

                            if (attributes.TryGetProperty("clanLevel", out var levelEl) && levelEl.ValueKind == JsonValueKind.Number)
                                clan.ClanLevel = levelEl.GetInt32();

                            if (attributes.TryGetProperty("clanMemberCount", out var countEl) && countEl.ValueKind == JsonValueKind.Number)
                                clan.ClanMemberCount = countEl.GetInt32();

                            clan.DateOfUpdate = DateTime.Now;

                            var updateResult = await _clanRepository.UpdateAsync(clan, cancellationToken);
                            if (updateResult)
                                _logger.LogInformation("Clan {ClanId} updated successfully in DB.", clan.ClanId);
                            else
                                _logger.LogWarning("Clan {ClanId} update returned false.", clan.ClanId);
                        }
                        else
                        {
                            _logger.LogWarning("PUBG API response for clan {ClanId} did not contain expected data.attributes.", clan.ClanId);
                        }
                    }
                    else
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning("PUBG API returned non-success for clan {ClanId}. Status: {StatusCode}. Body: {Body}", clan.ClanId, (int)response.StatusCode, body);
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP request failed when calling PUBG API for clan {ClanId}.", clan.ClanId);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse JSON from PUBG API for clan {ClanId}.", clan.ClanId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in GetOrUpdateClanAsync for clan {ClanId}.", clan.ClanId);
                }
            }

            var clanDto = _mapper.Map<ClanDTO>(clan);
            return clanDto;
        }

        public async Task UpdateStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var members = await _clanMemberRepository.GetActiveMembersAsync(cancellationToken);
            if (members is null || members.Count == 0)
            {
                _logger.LogInformation("UpdateStatisticsAsync: no active clan members found.");
                return;
            }

            var memberPlayerIds = members.Select(m => m.PlayerId).ToHashSet(StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation("UpdateStatisticsAsync: found {Count} active members. Processing matches...", members.Count);

            var clientWithKey = _httpClientFactory.CreateClient();
            clientWithKey.DefaultRequestHeaders.Accept.Clear();
            clientWithKey.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
            clientWithKey.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var clientNoKey = _httpClientFactory.CreateClient();
            clientNoKey.DefaultRequestHeaders.Accept.Clear();
            clientNoKey.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

            foreach (var member in members)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var encodedPlayerId = WebUtility.UrlEncode(member.PlayerId);
                    var playerUrl = $"{_options.BaseUrl}/players/{encodedPlayerId}";
                    _logger.LogDebug("Requesting player {PlayerId} at {Url}", member.PlayerId, playerUrl);

                    using var playerResponse = await SendPlayersRequestWithRateLimitAsync(clientWithKey, playerUrl, cancellationToken);
                    if (!playerResponse.IsSuccessStatusCode)
                    {
                        var body = await playerResponse.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("PUBG players API returned non-success for player {PlayerId}. Status: {StatusCode}. Body: {Body}",
                            member.PlayerId, (int)playerResponse.StatusCode, body);
                        continue;
                    }

                    using var playerStream = await playerResponse.Content.ReadAsStreamAsync(cancellationToken);
                    using var playerDoc = await JsonDocument.ParseAsync(playerStream, cancellationToken: cancellationToken);

                    if (!(playerDoc.RootElement.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("relationships", out var relationships) &&
                        relationships.TryGetProperty("matches", out var matchesRel) &&
                        matchesRel.TryGetProperty("data", out var matchesArray) &&
                        matchesArray.ValueKind == JsonValueKind.Array &&
                        matchesArray.GetArrayLength() > 0))
                    {
                        _logger.LogDebug("No matches array found for player {PlayerId}", member.PlayerId);
                        continue;
                    }

                    foreach (var matchEl in matchesArray.EnumerateArray())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!matchEl.TryGetProperty("id", out var matchIdEl) || matchIdEl.ValueKind != JsonValueKind.String)
                            continue;

                        var matchId = matchIdEl.GetString()!;
                        if (string.IsNullOrWhiteSpace(matchId))
                            continue;

                        var exists = await _matchRepository.ExistsAsync(matchId, cancellationToken);
                        if (exists)
                        {
                            _logger.LogDebug("Match {MatchId} already exists. Skipping.", matchId);
                            continue;
                        }

                        var matchUrl = $"{_options.BaseUrl}/matches/{matchId}";
                        _logger.LogInformation("Fetching match {MatchId} details from {Url}", matchId, matchUrl);

                        using var matchResponse = await clientNoKey.GetAsync(matchUrl, cancellationToken);
                        if (!matchResponse.IsSuccessStatusCode)
                        {
                            var body = await matchResponse.Content.ReadAsStringAsync(cancellationToken);
                            _logger.LogWarning("PUBG matches API returned non-success for match {MatchId}. Status: {StatusCode}. Body: {Body}",
                                matchId, (int)matchResponse.StatusCode, body);
                            continue;
                        }

                        using var matchStream = await matchResponse.Content.ReadAsStreamAsync(cancellationToken);
                        using var matchDoc = await JsonDocument.ParseAsync(matchStream, cancellationToken: cancellationToken);

                        if (!(matchDoc.RootElement.TryGetProperty("data", out var matchData) &&
                              matchData.TryGetProperty("attributes", out var matchAttributes)))
                        {
                            _logger.LogWarning("Match {MatchId} response missing data.attributes", matchId);
                            continue;
                        }

                        var matchType = matchAttributes.TryGetProperty("matchType", out var matchTypeEl) && matchTypeEl.ValueKind == JsonValueKind.String
                            ? matchTypeEl.GetString() ?? string.Empty
                            : string.Empty;

                        if (!string.IsNullOrEmpty(matchType) &&
                            (string.Equals(matchType, "arcade", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(matchType, "custom", StringComparison.OrdinalIgnoreCase)))
                        {
                            _logger.LogInformation("Skipping match {MatchId} of type '{MatchType}'.", matchId, matchType);
                            continue;
                        }

                        var mapName = matchAttributes.TryGetProperty("mapName", out var mapEl) && mapEl.ValueKind == JsonValueKind.String
                            ? mapEl.GetString() ?? string.Empty
                            : string.Empty;

                        var createdAt = matchAttributes.TryGetProperty("createdAt", out var createdEl) && createdEl.ValueKind == JsonValueKind.String
                            ? createdEl.GetString() ?? string.Empty
                            : string.Empty;

                        var matchEntity = new Match
                        {
                            MatchId = matchId,
                            MapName = mapName,
                            CreatedAt = createdAt
                        };

                        try
                        {
                            await _matchRepository.AddAsync(matchEntity, cancellationToken);
                            _logger.LogInformation("Inserted Match {MatchId} into DB.", matchId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to insert Match {MatchId} into DB.", matchId);
                            continue; 
                        }

                        if (matchDoc.RootElement.TryGetProperty("included", out var included) && included.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var inc in included.EnumerateArray())
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                if (!inc.TryGetProperty("type", out var typeEl) || typeEl.GetString() != "participant")
                                    continue;

                                if (!inc.TryGetProperty("attributes", out var partAttributes))
                                    continue;

                                if (!partAttributes.TryGetProperty("stats", out var statsEl) || statsEl.ValueKind != JsonValueKind.Object)
                                    continue;

                                if (!statsEl.TryGetProperty("playerId", out var pIdEl) || pIdEl.ValueKind != JsonValueKind.String)
                                    continue;

                                var participantPlayerId = pIdEl.GetString()!;
                                if (string.IsNullOrWhiteSpace(participantPlayerId))
                                    continue;

                                if (!memberPlayerIds.Contains(participantPlayerId))
                                    continue;

                                int dbnos = statsEl.TryGetProperty("DBNOs", out var dbnosEl) && dbnosEl.TryGetInt32(out var vdb) ? vdb : 0;
                                int assists = statsEl.TryGetProperty("assists", out var assistsEl) && assistsEl.TryGetInt32(out var vas) ? vas : 0;
                                decimal damageDealt = statsEl.TryGetProperty("damageDealt", out var dmgEl) && dmgEl.TryGetDecimal(out var vdmg) ? vdmg : 0m;
                                int headshotKills = statsEl.TryGetProperty("headshotKills", out var hsEl) && hsEl.TryGetInt32(out var vhs) ? vhs : 0;
                                int kills = statsEl.TryGetProperty("kills", out var killsEl) && killsEl.TryGetInt32(out var vk) ? vk : 0;
                                int revives = statsEl.TryGetProperty("revives", out var revEl) && revEl.TryGetInt32(out var vr) ? vr : 0;
                                int teamKills = statsEl.TryGetProperty("teamKills", out var tkEl) && tkEl.TryGetInt32(out var vtk) ? vtk : 0;
                                decimal timeSurvived = statsEl.TryGetProperty("timeSurvived", out var tsEl) && tsEl.TryGetDecimal(out var vts) ? vts : 0m;
                                int winPlace = statsEl.TryGetProperty("winPlace", out var wpEl) && wpEl.TryGetInt32(out var vwp) ? vwp : 0;

                                var playerMatchEntity = new PlayerMatch
                                {
                                    PlayerId = participantPlayerId,
                                    MatchId = matchId,
                                    DBNOs = dbnos,
                                    Assists = assists,
                                    DamageDealt = damageDealt,
                                    HeadshotsKills = headshotKills,
                                    Kills = kills,
                                    Revive = revives,
                                    TeamKill = teamKills,
                                    TimeSurvived = timeSurvived,
                                    WinPlace = winPlace,
                                    MatchCreatedAt = createdAt
                                };

                                try
                                {
                                    await _playerMatchRepository.AddAsync(playerMatchEntity, cancellationToken);
                                    _logger.LogInformation("Inserted PlayerMatch for player {PlayerId} in match {MatchId}.", participantPlayerId, matchId);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to insert PlayerMatch for player {PlayerId} in match {MatchId}.", participantPlayerId, matchId);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Match {MatchId} had no included participants.", matchId);
                        }
                        
                        await Task.Delay(50, cancellationToken);
                    } 
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("UpdateStatisticsAsync cancelled.");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing player {PlayerId}.", member.PlayerId);
                }
            }

            _logger.LogInformation("UpdateStatisticsAsync completed.");
        }

        public async Task<string?> GetPlayerLifetimeStatsAsync(string playerId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return null;

            try
            {
                var existing = await _playerLifetimeRepository.GetByPlayerIdAsync(playerId, cancellationToken).ConfigureAwait(false);

                var nowBaDate = DateTime.UtcNow.AddHours(-3).Date;

                if (existing != null)
                {
                    var existingBaDate = existing.DateOfUpdate.ToUniversalTime().AddHours(-3).Date;
                    if (existingBaDate == nowBaDate)
                    {
                        return existing.LifetimeJson;
                    }
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

                var encodedPlayerId = WebUtility.UrlEncode(playerId);
                var requestUrl = $"{_options.BaseUrl}/players/{encodedPlayerId}/seasons/lifetime?filter[gamepad]=false";
                _logger.LogInformation("Requesting PUBG lifetime stats for player {PlayerId} at {Url}", playerId, requestUrl);

                using var response = await client.GetAsync(requestUrl, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("PUBG lifetime API returned non-success for player {PlayerId}. Status: {StatusCode}. Body: {Body}",
                        playerId, (int)response.StatusCode, body);

                    if (existing != null)
                        return existing.LifetimeJson;

                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (existing != null)
                {
                    existing.LifetimeJson = responseBody;
                    existing.DateOfUpdate = DateTime.UtcNow;
                    try
                    {
                        await _playerLifetimeRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation("Updated PlayerLifetime cache for player {PlayerId}.", playerId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update PlayerLifetime for player {PlayerId}.", playerId);
                    }
                }
                else
                {
                    var newEntity = new PlayerLifetime
                    {
                        PlayerId = playerId,
                        LifetimeJson = responseBody,
                        DateOfUpdate = DateTime.UtcNow
                    };

                    try
                    {
                        await _playerLifetimeRepository.AddAsync(newEntity, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation("Inserted PlayerLifetime cache for player {PlayerId}.", playerId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to insert PlayerLifetime for player {PlayerId}.", playerId);
                    }
                }

                return responseBody;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed when calling PUBG lifetime API for player {PlayerId}.", playerId);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON error in GetPlayerLifetimeStatsAsync for player {PlayerId}.", playerId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetPlayerLifetimeStatsAsync for player {PlayerId}.", playerId);
                return null;
            }
        }

        public async Task<IEnumerable<RecentGameStatsDTO>> GetOrUpdateRecentGamesAsync(string playerId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return Enumerable.Empty<RecentGameStatsDTO>();

            try
            {
                // 1) Obtener registros existentes en la base de datos (últimos 20)
                var dbRecords = (await _recentGamesStatsRepository.GetLast20ByPlayerIdAsync(playerId, cancellationToken).ConfigureAwait(false))
                                .ToList();

                if (dbRecords.Count > 0)
                {
                    var latest = dbRecords.OrderByDescending(r => r.DateOfUpdate).First();
                    var elapsed = DateTime.UtcNow - latest.DateOfUpdate.ToUniversalTime();
                    if (elapsed < TimeSpan.FromHours(1))
                    {
                        _logger.LogInformation("GetOrUpdateRecentGamesAsync: devolviendo últimos 20 registros desde DB para player {PlayerId} (última actualización hace {Elapsed}).", playerId, elapsed);
                        return dbRecords.Select(r => _mapper.Map<RecentGameStatsDTO>(r)).ToList();
                    }
                }

                // 2) No hay registros recientes en DB -> solicitar player para obtener matchIds
                var clientWithKey = _httpClientFactory.CreateClient();
                clientWithKey.DefaultRequestHeaders.Accept.Clear();
                clientWithKey.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));
                clientWithKey.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

                var encodedPlayerId = WebUtility.UrlEncode(playerId);
                var playerUrl = $"{_options.BaseUrl}/players/{encodedPlayerId}";
                _logger.LogInformation("Requesting PUBG player {PlayerId} at {Url}", playerId, playerUrl);

                using var playerResponse = await SendPlayersRequestWithRateLimitAsync(clientWithKey, playerUrl, cancellationToken);
                if (!playerResponse.IsSuccessStatusCode)
                {
                    var body = await playerResponse.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("PUBG players API returned non-success for player {PlayerId}. Status: {StatusCode}. Body: {Body}",
                        playerId, (int)playerResponse.StatusCode, body);

                    // Devolver lo que haya en DB (aunque sea antiguo) si existe
                    return dbRecords.Select(r => _mapper.Map<RecentGameStatsDTO>(r)).ToList();
                }

                using var playerStream = await playerResponse.Content.ReadAsStreamAsync(cancellationToken);
                using var playerDoc = await JsonDocument.ParseAsync(playerStream, cancellationToken: cancellationToken);

                if (!(playerDoc.RootElement.TryGetProperty("data", out var data) &&
                      data.TryGetProperty("relationships", out var relationships) &&
                      relationships.TryGetProperty("matches", out var matchesRel) &&
                      matchesRel.TryGetProperty("data", out var matchesArray) &&
                      matchesArray.ValueKind == JsonValueKind.Array &&
                      matchesArray.GetArrayLength() > 0))
                {
                    _logger.LogInformation("GetOrUpdateRecentGamesAsync: no matches found for player {PlayerId}", playerId);
                    return dbRecords.Select(r => _mapper.Map<RecentGameStatsDTO>(r)).ToList();
                }

                // Preparar set de MatchIds ya presentes en DB para evitar duplicados
                var existingMatchIds = new HashSet<string>(dbRecords.Select(e => e.MatchId), StringComparer.OrdinalIgnoreCase);

                var clientNoKey = _httpClientFactory.CreateClient();
                clientNoKey.DefaultRequestHeaders.Accept.Clear();
                clientNoKey.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api+json"));

                var newInserted = new List<RecentGamesStats>();

                int taken = 0;
                foreach (var matchRef in matchesArray.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (taken >= 20) break;

                    if (!matchRef.TryGetProperty("id", out var matchIdEl) || matchIdEl.ValueKind != JsonValueKind.String)
                        continue;

                    var matchId = matchIdEl.GetString();
                    if (string.IsNullOrWhiteSpace(matchId)) continue;

                    taken++;

                    if (existingMatchIds.Contains(matchId))
                    {
                        _logger.LogDebug("GetOrUpdateRecentGamesAsync: match {MatchId} ya existe en DB para player {PlayerId}, se omite.", matchId, playerId);
                        continue;
                    }

                    var matchUrl = $"{_options.BaseUrl}/matches/{matchId}";
                    _logger.LogInformation("Fetching match {MatchId} details from {Url}", matchId, matchUrl);

                    using var matchResponse = await clientNoKey.GetAsync(matchUrl, cancellationToken);
                    if (!matchResponse.IsSuccessStatusCode)
                    {
                        var body = await matchResponse.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("PUBG matches API returned non-success for match {MatchId}. Status: {StatusCode}. Body: {Body}",
                            matchId, (int)matchResponse.StatusCode, body);
                        continue;
                    }

                    using var matchStream = await matchResponse.Content.ReadAsStreamAsync(cancellationToken);
                    using var matchDoc = await JsonDocument.ParseAsync(matchStream, cancellationToken: cancellationToken);

                    if (!(matchDoc.RootElement.TryGetProperty("data", out var matchData) &&
                          matchData.TryGetProperty("attributes", out var matchAttributes)))
                    {
                        _logger.LogWarning("Match {MatchId} response missing data.attributes", matchId);
                        continue;
                    }

                    var createdAt = matchAttributes.TryGetProperty("createdAt", out var createdEl) && createdEl.ValueKind == JsonValueKind.String
                        ? createdEl.GetString() ?? string.Empty
                        : string.Empty;

                    var mapName = matchAttributes.TryGetProperty("mapName", out var mapEl) && mapEl.ValueKind == JsonValueKind.String
                        ? mapEl.GetString() ?? string.Empty
                        : string.Empty;

                    var gameMode = matchAttributes.TryGetProperty("gameMode", out var gmEl) && gmEl.ValueKind == JsonValueKind.String
                        ? gmEl.GetString() ?? string.Empty
                        : string.Empty;

                    var isCustom = matchAttributes.TryGetProperty("isCustomMatch", out var customEl) && customEl.ValueKind == JsonValueKind.True;

                    // Buscar participante correspondiente al playerId en included
                    RecentGamesStats? entityToInsert = null;

                    if (matchDoc.RootElement.TryGetProperty("included", out var included) && included.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var inc in included.EnumerateArray())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (!inc.TryGetProperty("type", out var typeEl) || typeEl.GetString() != "participant")
                                continue;

                            if (!inc.TryGetProperty("attributes", out var partAttributes))
                                continue;

                            if (!partAttributes.TryGetProperty("stats", out var statsEl) || statsEl.ValueKind != JsonValueKind.Object)
                                continue;

                            if (!statsEl.TryGetProperty("playerId", out var pIdEl) || pIdEl.ValueKind != JsonValueKind.String)
                                continue;

                            var participantPlayerId = pIdEl.GetString()!;
                            if (!string.Equals(participantPlayerId, playerId, StringComparison.OrdinalIgnoreCase))
                                continue;

                            // Extraer campos (si no existen, usar valores por defecto)
                            int dbnos = statsEl.TryGetProperty("DBNOs", out var dbnosEl) && dbnosEl.TryGetInt32(out var vdb) ? vdb : 0;
                            int assists = statsEl.TryGetProperty("assists", out var assistsEl) && assistsEl.TryGetInt32(out var vas) ? vas : 0;
                            int boots = statsEl.TryGetProperty("boots", out var bootsEl) && bootsEl.TryGetInt32(out var vb) ? vb : 0;
                            decimal damageDealt = statsEl.TryGetProperty("damageDealt", out var dmgEl) && dmgEl.TryGetDecimal(out var vdmg) ? vdmg : 0m;
                            int headshotsKills = statsEl.TryGetProperty("headshotKills", out var hsEl) && hsEl.TryGetInt32(out var vhs) ? vhs : 0;
                            int heals = statsEl.TryGetProperty("heals", out var healsEl) && healsEl.TryGetInt32(out var vh) ? vh : 0;
                            int killPlace = statsEl.TryGetProperty("killPlace", out var kpEl) && kpEl.TryGetInt32(out var vkp) ? vkp : 0;
                            int killStreaks = statsEl.TryGetProperty("killStreaks", out var ksEl) && ksEl.TryGetInt32(out var vks) ? vks : 0;
                            int kills = statsEl.TryGetProperty("kills", out var killsEl) && killsEl.TryGetInt32(out var vk) ? vk : 0;
                            decimal longestKill = statsEl.TryGetProperty("longestKill", out var lkEl) && lkEl.TryGetDecimal(out var vlk) ? vlk : 0m;
                            int revives = statsEl.TryGetProperty("revives", out var revEl) && revEl.TryGetInt32(out var vr) ? vr : 0;
                            decimal rideDistance = statsEl.TryGetProperty("rideDistance", out var rdEl) && rdEl.TryGetDecimal(out var vrd) ? vrd : 0m;
                            decimal swimDistance = statsEl.TryGetProperty("swimDistance", out var sdEl) && sdEl.TryGetDecimal(out var vsd) ? vsd : 0m;
                            decimal walkDistance = statsEl.TryGetProperty("walkDistance", out var wdEl) && wdEl.TryGetDecimal(out var vwd) ? vwd : 0m;
                            int roadKills = statsEl.TryGetProperty("roadKills", out var rdkEl) && rdkEl.TryGetInt32(out var vrdk) ? vrdk : 0;
                            int teamKills = statsEl.TryGetProperty("teamKills", out var tkEl) && tkEl.TryGetInt32(out var vtk) ? vtk : 0;
                            decimal timeSurvived = statsEl.TryGetProperty("timeSurvived", out var tsEl) && tsEl.TryGetDecimal(out var vts) ? vts : 0m;
                            int vehicleDestroys = statsEl.TryGetProperty("vehicleDestroys", out var vdEl) && vdEl.TryGetInt32(out var vvd) ? vvd : 0;
                            int weaponsAcquired = statsEl.TryGetProperty("weaponsAcquired", out var waEl) && waEl.TryGetInt32(out var vwa) ? vwa : 0;
                            int winPlace = statsEl.TryGetProperty("winPlace", out var wpEl) && wpEl.TryGetInt32(out var vwp) ? vwp : 0;

                            entityToInsert = new RecentGamesStats
                            {
                                PlayerId = participantPlayerId,
                                DateOfUpdate = DateTime.UtcNow,
                                MatchId = matchId,
                                CreatedAt = createdAt,
                                MapName = mapName,
                                GameMode = gameMode,
                                IsCustomMatch = isCustom,
                                DBNOs = dbnos,
                                Assists = assists,
                                Boots = boots,
                                DamageDealt = damageDealt,
                                HeadshotsKills = headshotsKills,
                                Heals = heals,
                                KillPlace = killPlace,
                                KillStreaks = killStreaks,
                                Kills = kills,
                                LongestKill = longestKill,
                                Revives = revives,
                                RideDistance = rideDistance,
                                SwimDistance = swimDistance,
                                WalkDistance = walkDistance,
                                RoadKills = roadKills,
                                TeamKills = teamKills,
                                TimeSurvived = timeSurvived,
                                VehicleDestroys = vehicleDestroys,
                                WeaponsAcquired = weaponsAcquired,
                                WinPlace = winPlace
                            };

                            break; // ya encontramos el participante correspondiente
                        }
                    }

                    if (entityToInsert == null)
                    {
                        // Si no encontramos participante con ese playerId en included, creamos un registro básico con Match metadata
                        entityToInsert = new RecentGamesStats
                        {
                            PlayerId = playerId,
                            DateOfUpdate = DateTime.UtcNow,
                            MatchId = matchId,
                            CreatedAt = createdAt,
                            MapName = mapName,
                            GameMode = gameMode,
                            IsCustomMatch = isCustom
                        };
                    }

                    try
                    {
                        await _recentGamesStatsRepository.AddAsync(entityToInsert, cancellationToken).ConfigureAwait(false);
                        newInserted.Add(entityToInsert);
                        _logger.LogInformation("Inserted RecentGamesStats for player {PlayerId} match {MatchId}.", playerId, matchId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to insert RecentGamesStats for player {PlayerId} match {MatchId}.", playerId, matchId);
                    }

                    // pequeñas pausas para evitar sobrecargar API
                    await Task.Delay(50, cancellationToken);
                }

                // 3) Recuperar últimos 20 registros desde DB y devolver DTOs
                var latest20 = (await _recentGamesStatsRepository.GetLast20ByPlayerIdAsync(playerId, cancellationToken).ConfigureAwait(false))
                                .Take(20)
                                .ToList();

                return latest20.Select(r => _mapper.Map<RecentGameStatsDTO>(r)).ToList();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("GetOrUpdateRecentGamesAsync cancelled for player {PlayerId}.", playerId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetOrUpdateRecentGamesAsync for player {PlayerId}.", playerId);
                return Enumerable.Empty<RecentGameStatsDTO>();
            }
        }

        private async Task<HttpResponseMessage> SendPlayersRequestWithRateLimitAsync(HttpClient client, string url, CancellationToken cancellationToken)
        {
            await _playersRateSemaphore.WaitAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;
                var since = now - _lastPlayersRequest;
                if (since < _playersRateInterval)
                {
                    var wait = _playersRateInterval - since;
                    _logger.LogDebug("Throttling players request for {Delay}ms to respect rate limit.", (int)wait.TotalMilliseconds);
                    await Task.Delay(wait, cancellationToken);
                }

                var response = await client.GetAsync(url, cancellationToken);
                _lastPlayersRequest = DateTime.UtcNow;
                return response;
            }
            finally
            {
                _playersRateSemaphore.Release();
            }
        }
    }
}