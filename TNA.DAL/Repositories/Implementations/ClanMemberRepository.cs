using Microsoft.EntityFrameworkCore;
using TNA.DAL.DbContext;
using TNA.DAL.Entities;
using TNA.DAL.Repositories.Interfaces;

namespace TNA.DAL.Repositories.Implementations
{
    public class ClanMemberRepository : IClanMemberRepository
    {
        private readonly TNADbContext _dbContext;

        public ClanMemberRepository(TNADbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<List<ClanMember>> GetActiveMembersAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.ClanMembers
                .AsNoTracking()
                .Where(m => m.Enabled)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
