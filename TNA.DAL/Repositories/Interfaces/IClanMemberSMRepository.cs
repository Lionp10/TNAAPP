using TNA.DAL.Entities;

namespace TNA.DAL.Repositories.Interfaces
{
    public interface IClanMemberSMRepository
    {
        Task<List<ClanMemberSocialMedia>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default);
    }
}
