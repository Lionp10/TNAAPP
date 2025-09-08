using Microsoft.EntityFrameworkCore;
using TNA.DAL.DbContext;
using TNA.DAL.Entities;
using TNA.DAL.Repositories.Interfaces;

namespace TNA.DAL.Repositories.Implementations
{
    public class ClanMemberSMRepository : IClanMemberSMRepository
    {
        private readonly TNADbContext _dbContext;

        public ClanMemberSMRepository(TNADbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<List<ClanMemberSocialMedia>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.ClanMemberSocialMedias
                .AsNoTracking()
                .Where(sm => sm.MemberId == memberId && sm.Enabled)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
