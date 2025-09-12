using TNA.BLL.DTOs;

namespace TNA.BLL.Services.Interfaces
{
    public interface IClanMemberSMService
    {
        Task<List<ClanMemberSocialMediaDTO>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);

        Task SyncForMemberAsync(int memberId, List<ClanMemberSocialMediaDTO> socialMedias, CancellationToken cancellationToken = default);
    }
}
