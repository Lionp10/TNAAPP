using AutoMapper;
using TNA.BLL.DTOs;
using TNA.BLL.Services.Interfaces;
using TNA.DAL.Repositories.Interfaces;

namespace TNA.BLL.Services.Implementations
{
    public class ClanServcice : IClanService
    {
        private readonly IClanRepository _clanRepository;
        private readonly IMapper _mapper;

        public ClanServcice(IClanRepository clanRepository, IMapper mapper)
        {
            _clanRepository = clanRepository ?? throw new ArgumentNullException(nameof(clanRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<ClanDTO?> GetByClanIdAsync(string clanId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(clanId))
                return null;

            var clanEntity = await _clanRepository
                .GetByClanIdAsync(clanId, cancellationToken)
                .ConfigureAwait(false);

            if (clanEntity is null)
                return null;

            return _mapper.Map<ClanDTO>(clanEntity);
        }
    }
}
