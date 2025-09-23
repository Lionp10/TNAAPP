using TNA.DAL.Entities;

namespace TNA.DAL.Repositories.Interfaces
{
    public interface IPlayerLifetimeRepository
    {
        Task<PlayerLifetime?> GetByPlayerIdAsync(string playerId, CancellationToken cancellationToken = default);
        Task<int> AddAsync(PlayerLifetime entity, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(PlayerLifetime entity, CancellationToken cancellationToken = default);
    }
}
