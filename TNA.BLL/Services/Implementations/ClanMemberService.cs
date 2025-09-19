using AutoMapper;
using TNA.BLL.DTOs;
using TNA.BLL.Services.Interfaces;
using TNA.DAL.Entities;
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
                return new List<ClanMemberDTO>();

            return _mapper.Map<List<ClanMemberDTO>>(clanMembersEntity);
        }

        public async Task<ClanMemberDTO?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _clanMemberRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (entity == null) return null;
            return _mapper.Map<ClanMemberDTO>(entity);
        }

        public async Task<int> CreateAsync(ClanMemberCreateDTO dto, CancellationToken cancellationToken = default)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            var entity = new ClanMember
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Nickname = dto.Nickname,
                PlayerId = dto.PlayerId,
                ClanId = dto.ClanId,
                ProfileImage = dto.ProfileImage,
                Enabled = dto.Enabled
            };

            return await _clanMemberRepository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateAsync(ClanMemberUpdateDTO dto, CancellationToken cancellationToken = default)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            var entity = new ClanMember
            {
                Id = dto.Id,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Nickname = dto.Nickname ?? string.Empty,
                PlayerId = dto.PlayerId ?? string.Empty,
                ClanId = dto.ClanId ?? string.Empty,
                ProfileImage = dto.ProfileImage,
                Enabled = dto.Enabled
            };

            await _clanMemberRepository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        }

        public async Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            await _clanMemberRepository.SoftDeleteAsync(id, cancellationToken).ConfigureAwait(false);
        }

        public async Task HardDeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            await _clanMemberRepository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        }

        public async Task<PagedResultDTO<ClanMemberDTO>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var (items, total) = await _clanMemberRepository.GetPagedAsync(page, pageSize, cancellationToken).ConfigureAwait(false);
            var dtoItems = _mapper.Map<List<ClanMemberDTO>>(items ?? new List<ClanMember>());
            return new PagedResultDTO<ClanMemberDTO>
            {
                Items = dtoItems,
                TotalItems = total,
                Page = page < 1 ? 1 : page,
                PageSize = pageSize < 1 ? 10 : pageSize
            };
        }
    }
}
