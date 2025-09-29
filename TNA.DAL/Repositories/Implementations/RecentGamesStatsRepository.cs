using Microsoft.EntityFrameworkCore;
using TNA.DAL.DbContext;
using TNA.DAL.Entities;
using TNA.DAL.Repositories.Interfaces;

namespace TNA.DAL.Repositories
{
    public class RecentGamesStatsRepository : IRecentGameStatsRepository
    {
        private readonly TNADbContext _db;

        public RecentGamesStatsRepository(TNADbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<RecentGamesStats?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0) return null;
            return await _db.RecentGamesStats
                            .AsNoTracking()
                            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                            .ConfigureAwait(false);
        }

        public async Task<IEnumerable<RecentGamesStats>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.RecentGamesStats
                            .AsNoTracking()
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false);
        }

        public async Task<IEnumerable<RecentGamesStats>> GetAllByPlayerIdAsync(string playerId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return Enumerable.Empty<RecentGamesStats>();
            return await _db.RecentGamesStats
                            .AsNoTracking()
                            .Where(r => r.PlayerId == playerId)
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false);
        }

        public async Task<IEnumerable<RecentGamesStats>> GetLast20ByPlayerIdAsync(string playerId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return Enumerable.Empty<RecentGamesStats>();
            return await _db.RecentGamesStats
                            .AsNoTracking()
                            .Where(r => r.PlayerId == playerId)
                            .OrderByDescending(r => r.DateOfUpdate)
                            .Take(20)
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false);
        }

        public async Task<int> AddAsync(RecentGamesStats entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            await _db.RecentGamesStats.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(RecentGamesStats entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _db.RecentGamesStats.Update(entity);
            var changes = await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return changes > 0;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0) return false;
            var existing = await _db.RecentGamesStats.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
            if (existing == null) return false;
            _db.RecentGamesStats.Remove(existing);
            var changes = await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return changes > 0;
        }
    }
}
