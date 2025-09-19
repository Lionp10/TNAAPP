using TNA.DAL.Entities;

namespace TNA.DAL.Repositories.Interfaces
{
    public interface IClanMemberRepository
    {
        Task<List<ClanMember>> GetActiveMembersAsync(CancellationToken cancellationToken = default);
        Task<ClanMember?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<int> AddAsync(ClanMember entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(ClanMember entity, CancellationToken cancellationToken = default);
        Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<(List<ClanMember> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    }
}
