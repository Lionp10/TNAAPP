using TNA.DAL.Entities;

namespace TNA.DAL.Repositories.Interfaces
{
    public interface IMatchRepository
    {
        Task<bool> ExistsAsync(string matchId, CancellationToken cancellationToken = default);
        Task AddAsync(Match match, CancellationToken cancellationToken = default);
        Task<Match?> GetByMatchIdAsync(string matchId, CancellationToken cancellationToken = default);
    }
}
