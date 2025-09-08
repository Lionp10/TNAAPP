using System.Threading;
using System.Threading.Tasks;
using TNA.DAL.Entities;

namespace TNA.DAL.Repositories.Interfaces
{
    public interface IClanRepository
    {
        Task<Clan?> GetByClanIdAsync(string clanId, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Clan clan, CancellationToken cancellationToken = default);
    }
}
