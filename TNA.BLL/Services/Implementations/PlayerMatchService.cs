using System;
using Microsoft.Extensions.Logging;
using TNA.BLL.DTOs;
using TNA.BLL.Services.Interfaces;
using TNA.DAL.Repositories.Interfaces;

namespace TNA.BLL.Services.Implementations
{
    public class PlayerMatchService : IPlayerMatchService
    {
        private readonly IPlayerMatchRepository _playerMatchRepository;
        private readonly IClanMemberRepository _clanMember_repository;
        private readonly ILogger<PlayerMatchService> _logger;

        public PlayerMatchService(
            IPlayerMatchRepository playerMatchRepository,
            IClanMemberRepository clanMemberRepository,
            ILogger<PlayerMatchService> logger)
        {
            _playerMatchRepository = playerMatchRepository ?? throw new ArgumentNullException(nameof(playerMatchRepository));
            _clanMember_repository = clanMemberRepository ?? throw new ArgumentNullException(nameof(clanMemberRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<PlayerRankingDTO>> GetRankingAsync(DateTimeOffset? startUtc, DateTimeOffset? endUtc, CancellationToken cancellationToken = default)
        {
            var matches = await _playerMatchRepository.GetByDateRangeAsync(startUtc, endUtc, cancellationToken).ConfigureAwait(false);
            if (matches is null || matches.Count == 0)
                return new List<PlayerRankingDTO>();

            // Agrupar por jugador y calcular totales + medias por partido
            var perPlayer = matches
                .GroupBy(m => m.PlayerId, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var totalDBNOs = g.Sum(x => x.DBNOs);
                    var totalAssists = g.Sum(x => x.Assists);
                    var totalKills = g.Sum(x => x.Kills);
                    var totalHeadshots = g.Sum(x => x.HeadshotsKills);
                    var totalDamage = g.Sum(x => x.DamageDealt);
                    var totalRevives = g.Sum(x => x.Revive);
                    var totalTeamKills = g.Sum(x => x.TeamKill);
                    var totalTimeSurvived = g.Sum(x => x.TimeSurvived);
                    var matchesCount = g.Count();
                    var avgWinPlace = g.Average(x => (double)x.WinPlace);

                    return new
                    {
                        PlayerId = g.Key,
                        MatchesCount = matchesCount,
                        TotalDBNOs = totalDBNOs,
                        TotalAssists = totalAssists,
                        TotalKills = totalKills,
                        TotalHeadshots = totalHeadshots,
                        TotalDamage = totalDamage,
                        TotalRevives = totalRevives,
                        TotalTeamKills = totalTeamKills,
                        TotalTimeSurvived = totalTimeSurvived,
                        AverageWinPlace = avgWinPlace,
                        AvgDBNOs = matchesCount > 0 ? (double)totalDBNOs / matchesCount : 0,
                        AvgAssists = matchesCount > 0 ? (double)totalAssists / matchesCount : 0,
                        AvgKills = matchesCount > 0 ? (double)totalKills / matchesCount : 0,
                        AvgHeadshots = matchesCount > 0 ? (double)totalHeadshots / matchesCount : 0,
                        AvgDamage = matchesCount > 0 ? (double)totalDamage / matchesCount : 0,
                        AvgRevives = matchesCount > 0 ? (double)totalRevives / matchesCount : 0,
                        AvgTeamKills = matchesCount > 0 ? (double)totalTeamKills / matchesCount : 0,
                        AvgTimeSurvived = matchesCount > 0 ? (double)totalTimeSurvived / matchesCount : 0
                    };
                })
                .ToList();

            // Nicknames
            var members = await _clanMember_repository.GetActiveMembersAsync(cancellationToken).ConfigureAwait(false);
            var nickByPlayer = members?.ToDictionary(m => m.PlayerId, m => m.Nickname, StringComparer.OrdinalIgnoreCase)
                               ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Calcular máximos sobre medias para normalizar
            double maxAvgDbnos = perPlayer.Max(x => x.AvgDBNOs);
            double maxAvgAssists = perPlayer.Max(x => x.AvgAssists);
            double maxAvgKills = perPlayer.Max(x => x.AvgKills);
            double maxAvgHeadshots = perPlayer.Max(x => x.AvgHeadshots);
            double maxAvgDamage = perPlayer.Max(x => x.AvgDamage);
            double maxAvgRevives = perPlayer.Max(x => x.AvgRevives);
            double maxAvgTeamKills = perPlayer.Max(x => x.AvgTeamKills);
            double maxAvgTimeSurvived = perPlayer.Max(x => x.AvgTimeSurvived);
            double minAvgWinPlace = perPlayer.Min(x => x.AverageWinPlace);
            double maxAvgWinPlace = perPlayer.Max(x => x.AverageWinPlace);

            // Pesos (suma positivos ~= 1)
            const double wDbnos = 0.08;
            const double wAssists = 0.08;
            const double wKills = 0.30;
            const double wHeadshots = 0.06;
            const double wDamage = 0.20;
            const double wRevives = 0.05;
            const double wTimeSurvived = 0.10;
            const double wWinPlace = 0.13;
            const double teamKillPenaltyMax = 0.25;

            var results = new List<PlayerRankingDTO>(perPlayer.Count);

            foreach (var p in perPlayer)
            {
                // Normalizaciones (seguras)
                double normDbnos = maxAvgDbnos > 0 ? p.AvgDBNOs / maxAvgDbnos : 0;
                double normAssists = maxAvgAssists > 0 ? p.AvgAssists / maxAvgAssists : 0;
                double normKills = maxAvgKills > 0 ? p.AvgKills / maxAvgKills : 0;
                double normHeadshots = maxAvgHeadshots > 0 ? p.AvgHeadshots / maxAvgHeadshots : 0;
                double normDamage = maxAvgDamage > 0 ? p.AvgDamage / maxAvgDamage : 0;
                double normRevives = maxAvgRevives > 0 ? p.AvgRevives / maxAvgRevives : 0;
                double normTimeSurvived = maxAvgTimeSurvived > 0 ? p.AvgTimeSurvived / maxAvgTimeSurvived : 0;

                // WinPlace: menor es mejor => invertir
                double normWinPlace;
                if (Math.Abs(maxAvgWinPlace - minAvgWinPlace) < 1e-9)
                {
                    normWinPlace = 0;
                }
                else
                {
                    normWinPlace = (maxAvgWinPlace - p.AverageWinPlace) / (maxAvgWinPlace - minAvgWinPlace);
                    normWinPlace = Math.Clamp(normWinPlace, 0.0, 1.0);
                }

                double normAvgTeamKills = maxAvgTeamKills > 0 ? p.AvgTeamKills / maxAvgTeamKills : 0;

                // Score positivo (medias)
                double positiveScore =
                    wDbnos * normDbnos +
                    wAssists * normAssists +
                    wKills * normKills +
                    wHeadshots * normHeadshots +
                    wDamage * normDamage +
                    wRevives * normRevives +
                    wTimeSurvived * normTimeSurvived +
                    wWinPlace * normWinPlace;

                double penalty = teamKillPenaltyMax * normAvgTeamKills;

                double baseScore = Math.Clamp(positiveScore - penalty, 0.0, 1.0);

                // Factor de confiabilidad: 10 partidas => 1.0
                double reliability = Math.Min(1.0, Math.Log(p.MatchesCount + 1) / Math.Log(11));

                double finalNormalized = Math.Clamp(baseScore * reliability, 0.0, 1.0);
                decimal totalPoints = Math.Round((decimal)(finalNormalized * 10.0), 2, MidpointRounding.AwayFromZero);

                results.Add(new PlayerRankingDTO
                {
                    PlayerId = p.PlayerId,
                    PlayerNickname = nickByPlayer.TryGetValue(p.PlayerId, out var nick) ? nick : null,
                    MatchesCount = p.MatchesCount,
                    TotalDBNOs = p.TotalDBNOs,
                    TotalAssists = p.TotalAssists,
                    TotalKills = p.TotalKills,
                    TotalHeadshotsKills = p.TotalHeadshots,
                    TotalDamageDealt = p.TotalDamage,
                    TotalRevives = p.TotalRevives,
                    TotalTeamKill = p.TotalTeamKills,
                    TotalTimeSurvived = p.TotalTimeSurvived,
                    // Aquí redondeamos el AvgWinPlace a int como pediste
                    AverageWinPlace = (int)Math.Round(p.AverageWinPlace, MidpointRounding.AwayFromZero),
                    TotalPoints = totalPoints
                });
            }

            // Añadir miembros activos que no aparecen en matches (todos 0). No se guardan en BD.
            var presentIds = results.Select(r => r.PlayerId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (members != null && members.Count > 0)
            {
                foreach (var mem in members)
                {
                    if (!presentIds.Contains(mem.PlayerId))
                    {
                        results.Add(new PlayerRankingDTO
                        {
                            PlayerId = mem.PlayerId,
                            PlayerNickname = mem.Nickname,
                            MatchesCount = 0,
                            TotalDBNOs = 0,
                            TotalAssists = 0,
                            TotalKills = 0,
                            TotalHeadshotsKills = 0,
                            TotalDamageDealt = 0m,
                            TotalRevives = 0,
                            TotalTeamKill = 0,
                            TotalTimeSurvived = 0m,
                            AverageWinPlace = 0,
                            TotalPoints = 0m
                        });
                    }
                }
            }

            // Ordenar: por puntos desc, desempate por MatchesCount y luego por TotalKills
            var ordered = results
                .OrderByDescending(r => r.TotalPoints)
                .ThenByDescending(r => r.MatchesCount)
                .ThenByDescending(r => r.TotalKills)
                .ToList();

            _logger.LogInformation("Computed ranking for {Count} players. Range [{Start} - {End})", ordered.Count, startUtc?.ToString("o") ?? "null", endUtc?.ToString("o") ?? "null");

            return ordered;
        }
    }
}
