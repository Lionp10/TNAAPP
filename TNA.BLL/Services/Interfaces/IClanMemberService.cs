using TNA.BLL.DTOs;

namespace TNA.BLL.Services.Interfaces
{
    public interface IClanMemberService
    {
        Task<List<ClanMemberDTO>> GetActiveMembersAsync(CancellationToken cancellationToken = default);
    }
}
