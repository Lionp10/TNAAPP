using TNA.DAL.Entities;

namespace TNA.DAL.Repositories.Interfaces
{
    public interface IRecentGameStatsRepository
    {
        Task<RecentGamesStats?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<RecentGamesStats>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<RecentGamesStats>> GetAllByPlayerIdAsync(string playerId, CancellationToken cancellationToken = default);
        Task<IEnumerable<RecentGamesStats>> GetLast20ByPlayerIdAsync(string playerId, CancellationToken cancellationToken = default);
        Task<int> AddAsync(RecentGamesStats entity, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(RecentGamesStats entity, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
