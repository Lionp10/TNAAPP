using TNA.DAL.Entities;

namespace TNA.DAL.Repositories.Interfaces
{
    public interface IPlayerMatchRepository
    {
        Task AddAsync(PlayerMatch playerMatch, CancellationToken cancellationToken = default);
        Task<List<PlayerMatch>> GetByDateRangeAsync(DateTimeOffset? startUtc, DateTimeOffset? endUtc, CancellationToken cancellationToken = default);
    }
}
