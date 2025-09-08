using Microsoft.EntityFrameworkCore;
using TNA.DAL.DbContext;
using TNA.DAL.Entities;
using TNA.DAL.Repositories.Interfaces;

namespace TNA.DAL.Repositories.Implementations
{
    public class MatchRepository : IMatchRepository
    {
        private readonly TNADbContext _dbContext;

        public MatchRepository(TNADbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<bool> ExistsAsync(string matchId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(matchId)) return false;

            return await _dbContext.Matches
                .AsNoTracking()
                .AnyAsync(m => m.MatchId == matchId, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task AddAsync(Match match, CancellationToken cancellationToken = default)
        {
            if (match is null) throw new ArgumentNullException(nameof(match));

            await _dbContext.Matches.AddAsync(match, cancellationToken).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<Match?> GetByMatchIdAsync(string matchId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(matchId)) return null;

            return await _dbContext.Matches
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MatchId == matchId, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
