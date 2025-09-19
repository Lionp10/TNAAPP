using AutoMapper;
using TNA.BLL.DTOs;
using TNA.BLL.Services.Interfaces;
using TNA.DAL.Entities;
using TNA.DAL.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace TNA.BLL.Services.Implementations
{
    public class ClanMemberSMService : IClanMemberSMService
    {
        private readonly IClanMemberSMRepository _repo;
        private readonly ILogger<ClanMemberSMService> _logger;
        private static readonly HashSet<string> _allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            "DC","ST","TW","YT","IG","FB","X"
        };
        private const int MaxItems = 5;

        public ClanMemberSMService(IClanMemberSMRepository repo, ILogger<ClanMemberSMService> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<ClanMemberSocialMediaDTO>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default)
        {
            var list = await _repo.GetByMemberIdAsync(memberId, cancellationToken).ConfigureAwait(false);
            return list.Select(e => new ClanMemberSocialMediaDTO
            {
                Id = e.Id,
                MemberId = e.MemberId,
                SocialMediaId = e.SocialMediaId,
                SocialMediaUrl = e.SocialMediaUrl,
                Enabled = e.Enabled
            }).ToList();
        }

        public async Task SyncForMemberAsync(int memberId, List<ClanMemberSocialMediaDTO> socialMedias, CancellationToken cancellationToken = default)
        {
            socialMedias ??= new List<ClanMemberSocialMediaDTO>();

            var normalized = socialMedias
                .Where(s => !string.IsNullOrWhiteSpace(s.SocialMediaId) && !string.IsNullOrWhiteSpace(s.SocialMediaUrl))
                .Select(s => new ClanMemberSocialMediaDTO
                {
                    Id = s.Id,
                    MemberId = memberId,
                    SocialMediaId = s.SocialMediaId.Trim().ToUpperInvariant(),
                    SocialMediaUrl = s.SocialMediaUrl.Trim(),
                    Enabled = true
                })
                .Where(s => _allowed.Contains(s.SocialMediaId))
                .GroupBy(s => s.SocialMediaId)
                .Select(g => g.First())
                .Take(MaxItems)
                .ToList();

            if (normalized.Count > MaxItems)
                normalized = normalized.Take(MaxItems).ToList();

            var existing = await _repo.GetByMemberIdAsync(memberId, cancellationToken).ConfigureAwait(false);

            var toDelete = existing
                .Where(e => !normalized.Any(n => string.Equals(n.SocialMediaId, e.SocialMediaId, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var del in toDelete)
            {
                await _repo.DeleteAsync(del.Id, cancellationToken).ConfigureAwait(false);
            }

            foreach (var item in normalized)
            {
                var ex = existing.FirstOrDefault(e => string.Equals(e.SocialMediaId, item.SocialMediaId, StringComparison.OrdinalIgnoreCase));
                if (ex != null)
                {
                    ex.SocialMediaUrl = item.SocialMediaUrl;
                    ex.Enabled = item.Enabled;
                    await _repo.UpdateAsync(ex, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var n = new ClanMemberSocialMedia
                    {
                        MemberId = memberId,
                        SocialMediaId = item.SocialMediaId,
                        SocialMediaUrl = item.SocialMediaUrl,
                        Enabled = item.Enabled
                    };
                    await _repo.AddAsync(n, cancellationToken).ConfigureAwait(false);
                }
            }

            _logger.LogInformation("Sincronizadas {Count} redes para MemberId {MemberId}", normalized.Count, memberId);
        }
    }
}
