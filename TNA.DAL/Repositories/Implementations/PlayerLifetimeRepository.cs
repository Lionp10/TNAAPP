using Microsoft.EntityFrameworkCore;
using TNA.DAL.DbContext;
using TNA.DAL.Entities;
using TNA.DAL.Repositories.Interfaces;

namespace TNA.DAL.Repositories
{
    public class PlayerLifetimeRepository : IPlayerLifetimeRepository
    {
        private readonly TNADbContext _db;

        public PlayerLifetimeRepository(TNADbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<PlayerLifetime?> GetByPlayerIdAsync(string playerId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return null;
            return await _db.PlayerLifetimes
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.PlayerId == playerId, cancellationToken)
                            .ConfigureAwait(false);
        }

        public async Task<int> AddAsync(PlayerLifetime entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            await _db.PlayerLifetimes.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(PlayerLifetime entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _db.PlayerLifetimes.Update(entity);
            var changes = await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return changes > 0;
        }
    }
}
