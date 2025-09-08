using AutoMapper;
using TNA.BLL.DTOs;
using TNA.BLL.Services.Interfaces;
using TNA.DAL.Repositories.Implementations;
using TNA.DAL.Repositories.Interfaces;

namespace TNA.BLL.Services.Implementations
{
    public class ClanMemberSMService : IClanMemberSMService
    {
        private readonly IClanMemberSMRepository _repository;
        private readonly IMapper _mapper;

        public ClanMemberSMService(IClanMemberSMRepository repository, IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<List<ClanMemberSocialMediaDTO>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default)
        {
            var clanMembersSM = await _repository.GetByMemberIdAsync(memberId, cancellationToken).ConfigureAwait(false);

            if (clanMembersSM is null)
                return null;

            return _mapper.Map<List<ClanMemberSocialMediaDTO>>(clanMembersSM);
        }
    }
}
