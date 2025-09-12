using TNA.DAL.Entities;

namespace TNA.DAL.Repositories.Interfaces
{
    public interface IClanMemberSMRepository
    {
        Task<List<ClanMemberSocialMedia>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);
        Task<ClanMemberSocialMedia?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task AddAsync(ClanMemberSocialMedia entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(ClanMemberSocialMedia entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
