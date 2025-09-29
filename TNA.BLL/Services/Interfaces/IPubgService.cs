using TNA.BLL.DTOs;

namespace TNA.BLL.Services.Interfaces
{
    public interface IPubgService
    {
        Task<ClanDTO?> GetOrUpdateClanAsync(CancellationToken cancellationToken = default);

        Task UpdateStatisticsAsync(CancellationToken cancellationToken = default);

        Task<string?> GetPlayerLifetimeStatsAsync(string playerId, CancellationToken cancellationToken = default);

        Task<IEnumerable<RecentGameStatsDTO>> GetOrUpdateRecentGamesAsync(string playerId, CancellationToken cancellationToken = default);
    }
}
