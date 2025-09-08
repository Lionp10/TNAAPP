using TNA.BLL.DTOs;

namespace TNA.BLL.Services.Interfaces
{
    public interface IPubgService
    {
        Task<ClanDTO?> GetOrUpdateClanAsync(CancellationToken cancellationToken = default);

        Task UpdateStatisticsAsync(CancellationToken cancellationToken = default);
    }
}
