using Microsoft.EntityFrameworkCore;
using TNA.DAL.DbContext;
using TNA.DAL.Entities;
using TNA.DAL.Repositories.Interfaces;

namespace TNA.DAL.Repositories.Implementations
{
    public class ClanRepository : IClanRepository
    {
        private readonly TNADbContext _dbContext;

        public ClanRepository(TNADbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<Clan?> GetByClanIdAsync(string clanId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(clanId))
                return null;

            return await _dbContext.Clans
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClanId == clanId, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<bool> UpdateAsync(Clan clan, CancellationToken cancellationToken = default)
        {
            if (clan is null) throw new ArgumentNullException(nameof(clan));
            if (string.IsNullOrWhiteSpace(clan.ClanId)) return false;

            var existing = await _dbContext.Clans
                .FirstOrDefaultAsync(c => c.ClanId == clan.ClanId, cancellationToken)
                .ConfigureAwait(false);

            if (existing is null)
                return false;

            existing.ClanName = clan.ClanName;
            existing.ClanTag = clan.ClanTag;
            existing.ClanLevel = clan.ClanLevel;
            existing.ClanMemberCount = clan.ClanMemberCount;
            existing.DateOfUpdate = clan.DateOfUpdate;
            existing.Enabled = clan.Enabled;

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }
            catch (DbUpdateException) 
            { 
                return false;
            }
        }
    }
}
