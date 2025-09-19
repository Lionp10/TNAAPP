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

namespace TNA.BLL.Services.Implementations
{
    public class PubgService : IPubgService
    {
        private readonly IClanRepository _clanRepository;
        private readonly IClanMemberRepository _clanMemberRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IPlayerMatchRepository _playerMatchRepository;
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
            IMapper mapper,
            IHttpClientFactory httpClientFactory,
            IOptions<PubgOptions> options,
            ILogger<PubgService> logger)
        {
            _clanRepository = clanRepository ?? throw new ArgumentNullException(nameof(clanRepository));
            _clanMemberRepository = clanMemberRepository ?? throw new ArgumentNullException(nameof(clanMemberRepository));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _playerMatchRepository = playerMatchRepository ?? throw new ArgumentNullException(nameof(playerMatchRepository));
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
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return responseBody;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed when calling PUBG lifetime API for player {PlayerId}.", playerId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetPlayerLifetimeStatsAsync for player {PlayerId}.", playerId);
                return null;
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