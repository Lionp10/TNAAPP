using TNA.BLL.DTOs;

namespace TNA.BLL.Services.Interfaces
{
    public interface IClanMemberService
    {
        Task<List<ClanMemberDTO>> GetActiveMembersAsync(CancellationToken cancellationToken = default);
        Task<ClanMemberDTO?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<int> CreateAsync(ClanMemberCreateDTO dto, CancellationToken cancellationToken = default);
        Task UpdateAsync(ClanMemberUpdateDTO dto, CancellationToken cancellationToken = default);
        Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
        Task HardDeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<PagedResultDTO<ClanMemberDTO>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    }
}
