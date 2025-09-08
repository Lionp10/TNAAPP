using Microsoft.EntityFrameworkCore;
using TNA.DAL.DbContext;
using TNA.DAL.Entities;
using TNA.DAL.Repositories.Interfaces;

namespace TNA.DAL.Repositories.Implementations
{
    public class PlayerMatchRepository : IPlayerMatchRepository
    {
        private readonly TNADbContext _dbContext;

        public PlayerMatchRepository(TNADbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task AddAsync(PlayerMatch playerMatch, CancellationToken cancellationToken = default)
        {
            if (playerMatch is null) throw new ArgumentNullException(nameof(playerMatch));

            await _dbContext.PlayerMatches.AddAsync(playerMatch, cancellationToken).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<PlayerMatch>> GetByDateRangeAsync(DateTimeOffset? startUtc, DateTimeOffset? endUtc, CancellationToken cancellationToken = default)
        {
            string? startStr = startUtc?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            string? endStr = endUtc?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

            var query = _dbContext.PlayerMatches.AsNoTracking().AsQueryable();

            if (startStr is not null)
            {
                query = query.Where(pm => pm.MatchCreatedAt != null && string.Compare(pm.MatchCreatedAt, startStr) >= 0);
            }

            if (endStr is not null)
            {
                query = query.Where(pm => pm.MatchCreatedAt != null && string.Compare(pm.MatchCreatedAt, endStr) < 0);
            }

            var result = await query
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return result;
        }
    }
}
