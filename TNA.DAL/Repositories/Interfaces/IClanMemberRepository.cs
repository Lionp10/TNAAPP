using TNA.DAL.Entities;

namespace TNA.DAL.Repositories.Interfaces
{
    public interface IClanMemberRepository
    {
        Task<List<ClanMember>> GetActiveMembersAsync(CancellationToken cancellationToken = default);
    }
}
