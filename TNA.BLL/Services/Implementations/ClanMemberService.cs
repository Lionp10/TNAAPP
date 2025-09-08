using AutoMapper;
using TNA.BLL.DTOs;
using TNA.BLL.Services.Interfaces;
using TNA.DAL.Repositories.Implementations;
using TNA.DAL.Repositories.Interfaces;

namespace TNA.BLL.Services.Implementations
{
    public class ClanMemberService : IClanMemberService
    {
        private readonly IClanMemberRepository _clanMemberRepository;
        private readonly IMapper _mapper;

        public ClanMemberService(IClanMemberRepository clanMemberRepository, IMapper mapper)
        {
            _clanMemberRepository = clanMemberRepository ?? throw new ArgumentNullException(nameof(clanMemberRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<List<ClanMemberDTO>> GetActiveMembersAsync(CancellationToken cancellationToken = default)
        {
            var clanMembersEntity = await _clanMemberRepository
                .GetActiveMembersAsync(cancellationToken)
                .ConfigureAwait(false);

            if (clanMembersEntity is null)
                return null;

            return _mapper.Map<List<ClanMemberDTO>>(clanMembersEntity);
        }
    }
}
