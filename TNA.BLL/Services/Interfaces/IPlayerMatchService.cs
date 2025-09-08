using TNA.BLL.DTOs;

namespace TNA.BLL.Services.Interfaces
{
    public interface IPlayerMatchService
    {
        Task<List<PlayerRankingDTO>> GetRankingAsync(DateTimeOffset? startUtc, DateTimeOffset? endUtc, CancellationToken cancellationToken = default);
    }
}
