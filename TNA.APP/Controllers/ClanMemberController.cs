using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using TNA.APP.Models;
using TNA.BLL.DTOs;
using TNA.BLL.Services.Interfaces;

namespace TNA.APP.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class ClanMemberController : Controller
    {
        private readonly ILogger<ClanMemberController> _logger;
        private readonly IClanMemberService _clanMemberService;
        private readonly IClanMemberSMService _clanMemberSMService;
        private readonly IS3Service _s3Service;
        private readonly IConfiguration _configuration;
        private const int PageSizeConst = 10;

        public ClanMemberController(
            ILogger<ClanMemberController> logger,
            IClanMemberService clanMemberService,
            IClanMemberSMService clanMemberSMService,
            IS3Service s3Service,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clanMemberService = clanMemberService ?? throw new ArgumentNullException(nameof(clanMemberService));
            _clanMemberSMService = clanMemberSMService ?? throw new ArgumentNullException(nameof(clanMemberSMService));
            _s3Service = s3Service ?? throw new ArgumentNullException(nameof(s3Service));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            var paged = await _clanMemberService.GetPagedAsync(page, PageSizeConst, cancellationToken).ConfigureAwait(false);

            var vm = new ClanMemberIndexViewModel
            {
                Page = paged.Page,
                PageSize = paged.PageSize,
                TotalItems = paged.TotalItems,
                TotalPages = paged.TotalPages
            };

            foreach (var m in paged.Items)
            {
                var socials = await _clanMemberSMService.GetByMemberIdAsync(m.Id, cancellationToken).ConfigureAwait(false);
                vm.Items.Add(new MemberViewModel
                {
                    Member = m,
                    SocialMedias = socials
                });
            }

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
        {
            var memberDto = await _clanMemberService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (memberDto == null) return NotFound();

            var socials = await _clanMemberSMService.GetByMemberIdAsync(id, cancellationToken).ConfigureAwait(false);

            var vm = new MemberViewModel
            {
                Member = memberDto,
                SocialMedias = socials
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var vm = new ClanMemberViewModel
            {
                ClanId = _configuration["Clan:DefaultId"] ?? string.Empty
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClanMemberViewModel model, List<ClanMemberSMViewModel>? memberSocialMedias, IFormFile? ProfileImageFile, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.MemberSocialMedias = memberSocialMedias ?? new List<ClanMemberSMViewModel>();
                return View(model);
            }

            try
            {
                string? imageKey = model.ProfileImage;
                if (ProfileImageFile != null && ProfileImageFile.Length > 0)
                {
                    imageKey = await _s3Service.UploadFileAsync(ProfileImageFile, cancellationToken).ConfigureAwait(false);
                }

                var dto = new ClanMemberCreateDTO
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Nickname = model.Nickname ?? string.Empty,
                    PlayerId = model.PlayerId ?? string.Empty,
                    ClanId = model.ClanId ?? string.Empty,
                    ProfileImage = imageKey, 
                    Enabled = model.Enabled
                };

                var newId = await _clanMemberService.CreateAsync(dto, cancellationToken).ConfigureAwait(false);

                TempData["SuccessMessage"] = "Miembro creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando ClanMember");
                ModelState.AddModelError(string.Empty, "Error inesperado al crear el miembro.");
                ViewBag.MemberSocialMedias = memberSocialMedias ?? new List<ClanMemberSMViewModel>();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var member = await _clanMemberService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (member == null) return NotFound();

            var socials = await _clanMemberSMService.GetByMemberIdAsync(id, cancellationToken).ConfigureAwait(false);

            var vm = new ClanMemberViewModel
            {
                Id = member.Id,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Nickname = member.Nickname,
                PlayerId = member.PlayerId,
                ClanId = member.ClanId,
                ProfileImage = member.ProfileImage,
                Enabled = member.Enabled
            };

            ViewBag.MemberSocialMedias = socials?.Select(s => new ClanMemberSMViewModel
            {
                Id = s.Id,
                MemberId = s.MemberId,
                SocialMediaId = s.SocialMediaId,
                SocialMediaUrl = s.SocialMediaUrl,
                Enabled = s.Enabled
            }).ToList() ?? new List<ClanMemberSMViewModel>();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClanMemberViewModel model, List<ClanMemberSMViewModel>? memberSocialMedias, IFormFile? ProfileImageFile, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.MemberSocialMedias = memberSocialMedias ?? new List<ClanMemberSMViewModel>();
                return View(model);
            }

            try
            {
                var existingMemberDto = await _clanMemberService.GetByIdAsync(model.Id, cancellationToken).ConfigureAwait(false);
                string? previousImageKey = existingMemberDto?.ProfileImage;

                string? imageKey = model.ProfileImage;

                bool uploadedNewFile = false;
                if (ProfileImageFile != null && ProfileImageFile.Length > 0)
                {
                    imageKey = await _s3Service.UploadFileAsync(ProfileImageFile, cancellationToken).ConfigureAwait(false);
                    uploadedNewFile = true;
                }

                var updateDto = new ClanMemberUpdateDTO
                {
                    Id = model.Id,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Nickname = model.Nickname,
                    PlayerId = model.PlayerId,
                    ClanId = model.ClanId,
                    ProfileImage = imageKey,
                    Enabled = model.Enabled
                };

                await _clanMemberService.UpdateAsync(updateDto, cancellationToken).ConfigureAwait(false);

                if (uploadedNewFile && !string.IsNullOrWhiteSpace(previousImageKey))
                {
                    bool looksLikeS3Key = !(previousImageKey.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                            || previousImageKey.StartsWith("/") 
                                            || previousImageKey.StartsWith("~"));

                    if (looksLikeS3Key && !string.Equals(previousImageKey, imageKey, StringComparison.Ordinal))
                    {
                        try
                        {
                            await _s3Service.DeleteObjectAsync(previousImageKey, cancellationToken).ConfigureAwait(false);
                            _logger.LogInformation("Imagen previa {Key} eliminada de S3 para Member {MemberId}", previousImageKey, model.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "No se pudo eliminar imagen previa {Key} de S3 para Member {MemberId}", previousImageKey, model.Id);
                        }
                    }
                }

                TempData["SuccessMessage"] = "Miembro actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado actualizando ClanMember {MemberId}", model.Id);
                ModelState.AddModelError(string.Empty, "Error inesperado al actualizar el miembro.");
                ViewBag.MemberSocialMedias = memberSocialMedias ?? new List<ClanMemberSMViewModel>();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            var member = await _clanMemberService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (member == null) return NotFound();

            var vm = new ClanMemberViewModel
            {
                Id = member.Id,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Nickname = member.Nickname,
                PlayerId = member.PlayerId,
                ClanId = member.ClanId,
                ProfileImage = member.ProfileImage,
                Enabled = member.Enabled
            };

            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _clanMemberService.SoftDeleteAsync(id, cancellationToken).ConfigureAwait(false);
                TempData["SuccessMessage"] = "Miembro deshabilitado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deshabilitando ClanMember {MemberId}", id);
                TempData["ErrorMessage"] = "Error inesperado al eliminar el miembro.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDelete(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var existing = await _clanMemberService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
                string? previousImageKey = existing?.ProfileImage;

                await _clanMemberService.HardDeleteAsync(id, cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(previousImageKey))
                {
                    bool looksLikeS3Key = !(previousImageKey.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                            || previousImageKey.StartsWith("/")
                                            || previousImageKey.StartsWith("~"));

                    if (looksLikeS3Key)
                    {
                        try
                        {
                            await _s3Service.DeleteObjectAsync(previousImageKey, cancellationToken).ConfigureAwait(false);
                            _logger.LogInformation("Imagen {Key} eliminada de S3 tras HardDelete de Member {MemberId}", previousImageKey, id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Fallo al eliminar imagen {Key} de S3 después de HardDelete Member {MemberId}", previousImageKey, id);
                        }
                    }
                }

                TempData["SuccessMessage"] = "Miembro eliminado permanentemente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando permanentemente ClanMember {MemberId}", id);
                TempData["ErrorMessage"] = "Error inesperado al eliminar permanentemente el miembro.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
