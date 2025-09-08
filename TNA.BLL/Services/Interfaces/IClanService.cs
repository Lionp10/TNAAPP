using TNA.BLL.DTOs;

namespace TNA.BLL.Services.Interfaces
{
    public interface IClanService
    {
        Task<ClanDTO?> GetByClanIdAsync(string clanId, CancellationToken cancellationToken = default);
    }
}
