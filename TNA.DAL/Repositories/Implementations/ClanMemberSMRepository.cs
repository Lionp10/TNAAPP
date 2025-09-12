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

        public async Task<ClanMemberSocialMedia?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.ClanMemberSocialMedias
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task AddAsync(ClanMemberSocialMedia entity, CancellationToken cancellationToken = default)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            await _dbContext.ClanMemberSocialMedias.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateAsync(ClanMemberSocialMedia entity, CancellationToken cancellationToken = default)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            var existing = await _dbContext.ClanMemberSocialMedias.FirstOrDefaultAsync(s => s.Id == entity.Id, cancellationToken).ConfigureAwait(false);
            if (existing is null) throw new KeyNotFoundException($"Social media with Id {entity.Id} not found.");

            existing.SocialMediaId = entity.SocialMediaId;
            existing.SocialMediaUrl = entity.SocialMediaUrl;
            existing.Enabled = entity.Enabled;

            _dbContext.ClanMemberSocialMedias.Update(existing);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var existing = await _dbContext.ClanMemberSocialMedias.FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);
            if (existing is null) return;
            _dbContext.ClanMemberSocialMedias.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
