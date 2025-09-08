using TNA.BLL.DTOs;

namespace TNA.BLL.Services.Interfaces
{
    public interface IClanMemberSMService
    {
        Task<List<ClanMemberSocialMediaDTO>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);
    }
}
