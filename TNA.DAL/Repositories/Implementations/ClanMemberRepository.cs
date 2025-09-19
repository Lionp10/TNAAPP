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

        public async Task<ClanMember?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.ClanMembers
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<int> AddAsync(ClanMember entity, CancellationToken cancellationToken = default)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            await _dbContext.ClanMembers.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return entity.Id;
        }

        public async Task UpdateAsync(ClanMember entity, CancellationToken cancellationToken = default)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            var existing = await _dbContext.ClanMembers.FirstOrDefaultAsync(m => m.Id == entity.Id, cancellationToken).ConfigureAwait(false);
            if (existing is null) throw new KeyNotFoundException($"ClanMember with Id {entity.Id} not found.");

            existing.FirstName = entity.FirstName;
            existing.LastName = entity.LastName;
            existing.Nickname = entity.Nickname;
            existing.PlayerId = entity.PlayerId;
            existing.ClanId = entity.ClanId;
            existing.ProfileImage = entity.ProfileImage;
            existing.Enabled = entity.Enabled;

            _dbContext.ClanMembers.Update(existing);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var existing = await _dbContext.ClanMembers.FirstOrDefaultAsync(m => m.Id == id, cancellationToken).ConfigureAwait(false);
            if (existing is null) return;
            existing.Enabled = false;
            _dbContext.ClanMembers.Update(existing);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var existing = await _dbContext.ClanMembers.FirstOrDefaultAsync(m => m.Id == id, cancellationToken).ConfigureAwait(false);
            if (existing is null) return;
            _dbContext.ClanMembers.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<(List<ClanMember> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _dbContext.ClanMembers.AsNoTracking().Where(m => m.Enabled);

            var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var items = await query
                .OrderBy(m => m.Nickname)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return (items, total);
        }
    }
}
