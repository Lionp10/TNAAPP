using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.IO;
using TNA.APP.Models;
using TNA.BLL.DTOs;
using TNA.BLL.Services.Interfaces;
using TNA.DAL.DbContext;

namespace TNA.APP.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IClanService _clanService;
        private readonly IClanMemberService _clanMemberService;
        private readonly IClanMemberSMService _clanMemberSMService;
        private readonly IPubgService _pubgService;
        private readonly IPlayerMatchService _playerMatchService;
        private readonly IUserService _userService;
        private readonly IS3Service _s3Service;

        public HomeController(ILogger<HomeController> logger, IClanService clanService,
            IClanMemberService clanMemberService, IClanMemberSMService clanMemberSMService,
            IPubgService pubgService, IPlayerMatchService playerMatchService,
            IUserService userService, IS3Service s3Service)
        {
            _logger = logger;
            _clanService = clanService;
            _clanMemberService = clanMemberService;
            _clanMemberSMService = clanMemberSMService;
            _pubgService = pubgService;
            _playerMatchService = playerMatchService;
            _userService = userService;
            _s3Service = s3Service;
        }

        public async Task<IActionResult> Index(string range = "day")
        {
            var clan = await _pubgService.GetOrUpdateClanAsync();

            DateTimeOffset? startUtc = null;
            DateTimeOffset? endUtc = null;
            var tzOffset = TimeSpan.FromHours(-3);
            var nowTz = DateTimeOffset.UtcNow.ToOffset(tzOffset);

            string rangeDisplay;
            DateTimeOffset? startLocal = null;
            DateTimeOffset? endLocal = null;

            switch ((range ?? "day").ToLowerInvariant())
            {
                case "day":
                    var lastDay = nowTz.Date.AddDays(-1);
                    startLocal = new DateTimeOffset(lastDay, tzOffset);
                    endLocal = startLocal.Value.AddDays(1);
                    rangeDisplay = startLocal.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                    break;

                case "week":
                    var dow = nowTz.DayOfWeek;
                    int daysSinceMonday = dow == DayOfWeek.Sunday ? 6 : ((int)dow - 1);
                    var startOfCurrentWeek = nowTz.Date.AddDays(-daysSinceMonday);
                    var prevWeekStart = startOfCurrentWeek.AddDays(-7);
                    startLocal = new DateTimeOffset(prevWeekStart, tzOffset);
                    endLocal = startLocal.Value.AddDays(7);
                    rangeDisplay = $"{startLocal.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} - {endLocal.Value.AddDays(-1).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}";
                    break;

                case "month":
                    var startOfCurrentMonth = new DateTime(nowTz.Year, nowTz.Month, 1);
                    var prevMonthStart = startOfCurrentMonth.AddMonths(-1);
                    startLocal = new DateTimeOffset(prevMonthStart, tzOffset);
                    endLocal = startLocal.Value.AddMonths(1);
                    rangeDisplay = $"{startLocal.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} - {endLocal.Value.AddDays(-1).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}";
                    break;

                case "all":
                default:
                    startLocal = null;
                    endLocal = null;
                    rangeDisplay = "Histórico (todos los datos)";
                    break;
            }

            if (startLocal.HasValue) startUtc = startLocal.Value.ToUniversalTime();
            if (endLocal.HasValue) endUtc = endLocal.Value.ToUniversalTime();

            var ranking = await _playerMatch_service_wrapper(startUtc, endUtc).ConfigureAwait(false);

            var vm = new HomeIndexViewModel
            {
                Clan = clan,
                Rankings = ranking,
                SelectedRange = (range ?? "day").ToLowerInvariant(),
                RangeDisplay = rangeDisplay,
                RangeStartLocal = startLocal,
                RangeEndLocal = endLocal
            };

            return View(vm);
        }

        private async Task<List<TNA.BLL.DTOs.PlayerRankingDTO>> _playerMatch_service_wrapper(DateTimeOffset? s, DateTimeOffset? e)
        {
            return await _playerMatchService.GetRankingAsync(s, e);
        }

        public async Task<IActionResult> Members()
        {
            var clanMembers = await _clanMember_service_wrapper();

            var vm = new List<MemberViewModel>();

            foreach (var member in clanMembers)
            {
                var socialMedias = await _clanMemberSMService.GetByMemberIdAsync(member.Id);
                vm.Add(new MemberViewModel
                {
                    Member = member,
                    SocialMedias = socialMedias
                });
            }

            return View(vm);
        }

        private async Task<List<TNA.BLL.DTOs.ClanMemberDTO>> _clanMember_service_wrapper()
        {
            return await _clanMemberService.GetActiveMembersAsync();
        }

        public async Task<IActionResult> Tournaments()
        {
            return View();
        }

        public async Task<IActionResult> Profile(CancellationToken cancellationToken = default)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Challenge();

            var user = await _userService.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
            if (user is null) return NotFound();

            var vm = new ProfileViewModel
            {
                Id = user.Id,
                Nickname = user.Nickname,
                Email = user.Email,
                MemberId = (user.RoleId == 3 && user.MemberId.HasValue) ? user.MemberId : null
            };

            if (user.RoleId == 3 && user.MemberId.HasValue)
            {
                var db = HttpContext.RequestServices.GetService(typeof(TNADbContext)) as TNADbContext;
                if (db != null)
                {
                    var member = await db.ClanMembers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == user.MemberId.Value, cancellationToken)
                        .ConfigureAwait(false);

                    if (member != null)
                    {
                        vm.Member = new ClanMemberViewModel
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

                        try
                        {
                            var socials = await _clanMemberSMService.GetByMemberIdAsync(member.Id, cancellationToken).ConfigureAwait(false);
                            vm.MemberSocialMedias = socials?.Select(s => new ClanMemberSMViewModel
                            {
                                Id = s.Id,
                                MemberId = s.MemberId,
                                SocialMediaId = s.SocialMediaId,
                                SocialMediaUrl = s.SocialMediaUrl,
                                Enabled = s.Enabled
                            }).ToList();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "No se pudieron cargar redes sociales para member {MemberId}", member.Id);
                            vm.MemberSocialMedias = new List<ClanMemberSMViewModel>();
                        }
                    }
                }
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model, IFormFile? ProfileImageFile, CancellationToken cancellationToken = default)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Challenge();

            model.Id = userId;

            int claimRoleId = 0;
            int.TryParse(User.FindFirst("roleid")?.Value, out claimRoleId);

            if (ProfileImageFile != null && ProfileImageFile.Length > 0)
            {
                var ext = Path.GetExtension(ProfileImageFile.FileName) ?? string.Empty;
                if (!ext.Equals(".webp", StringComparison.OrdinalIgnoreCase)
                    && !ProfileImageFile.ContentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("ProfileImageFile", "Solo se permiten imágenes en formato .webp.");
                }
            }

            if (!ModelState.IsValid)
            {
                if (claimRoleId == 3 && model.MemberId.HasValue && model.Member == null)
                {
                    var db = HttpContext.RequestServices.GetService(typeof(TNADbContext)) as TNADbContext;
                    if (db != null)
                    {
                        try
                        {
                            var member = await db.ClanMembers
                                                 .AsNoTracking()
                                                 .FirstOrDefaultAsync(m => m.Id == model.MemberId.Value, cancellationToken)
                                                 .ConfigureAwait(false);
                            if (member != null)
                            {
                                model.Member = new ClanMemberViewModel
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
                            }
                        }
                        catch { /* no bloqueamos la validación por este fallo */ }
                    }
                }

                if (claimRoleId == 3 && model.MemberId.HasValue && (model.MemberSocialMedias == null || !model.MemberSocialMedias.Any()))
                {
                    try
                    {
                        var socials = await _clanMemberSMService.GetByMemberIdAsync(model.MemberId.Value, cancellationToken).ConfigureAwait(false);
                        model.MemberSocialMedias = socials?.Select(s => new ClanMemberSMViewModel
                        {
                            Id = s.Id,
                            MemberId = s.MemberId,
                            SocialMediaId = s.SocialMediaId,
                            SocialMediaUrl = s.SocialMediaUrl,
                            Enabled = s.Enabled
                        }).ToList();
                    }
                    catch { model.MemberSocialMedias = new List<ClanMemberSMViewModel>(); }
                }

                return View(model);
            }

            try
            {
                var existingUser = await _userService.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
                if (existingUser is null) return NotFound();

                var newNickname = model.Nickname?.Trim() ?? string.Empty;
                if (!string.Equals(existingUser.Nickname, newNickname, StringComparison.OrdinalIgnoreCase))
                {
                    if (await _userService.NicknameExistsAsync(newNickname, cancellationToken).ConfigureAwait(false))
                    {
                        ModelState.AddModelError(nameof(ProfileViewModel.Nickname), "El nickname ya está en uso.");
                        if (claimRoleId == 3 && model.MemberId.HasValue && (model.MemberSocialMedias == null || !model.MemberSocialMedias.Any()))
                        {
                            try
                            {
                                var socials = await _clanMemberSMService.GetByMemberIdAsync(model.MemberId.Value, cancellationToken).ConfigureAwait(false);
                                model.MemberSocialMedias = socials?.Select(s => new ClanMemberSMViewModel
                                {
                                    Id = s.Id,
                                    MemberId = s.MemberId,
                                    SocialMediaId = s.SocialMediaId,
                                    SocialMediaUrl = s.SocialMediaUrl,
                                    Enabled = s.Enabled
                                }).ToList();
                            }
                            catch { model.MemberSocialMedias = new List<ClanMemberSMViewModel>(); }
                        }
                        return View(model);
                    }
                }

                var userNeedsUpdate = false;
                var userUpdate = new UserUpdateDTO
                {
                    Id = existingUser.Id,
                    Nickname = existingUser.Nickname,
                    Email = existingUser.Email, 
                    Password = null,
                    RoleId = existingUser.RoleId,
                    MemberId = existingUser.MemberId,
                    Enabled = existingUser.Enabled
                };

                if (!string.Equals(existingUser.Nickname, newNickname, StringComparison.OrdinalIgnoreCase))
                {
                    userUpdate.Nickname = newNickname;
                    userNeedsUpdate = true;
                }

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    userUpdate.Password = model.Password;
                    userNeedsUpdate = true;
                }

                if (userNeedsUpdate)
                {
                    await _userService.UpdateAsync(userUpdate, cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("Usuario {UserId} actualizado (usuario) por el propio usuario.", userId);
                }

                if (existingUser.MemberId.HasValue
                    && existingUser.RoleId == 3
                    && model.Member != null
                    && model.Member.Id == existingUser.MemberId.Value)
                {
                    var db = HttpContext.RequestServices.GetService(typeof(TNADbContext)) as TNADbContext;
                    if (db != null)
                    {
                        var existingMember = await db.ClanMembers.FirstOrDefaultAsync(m => m.Id == model.Member.Id, cancellationToken).ConfigureAwait(false);
                        if (existingMember != null)
                        {
                            var memberChanged = false;

                            var previousImageKey = existingMember.ProfileImage;

                            bool uploadedNewFile = false;
                            if (ProfileImageFile != null && ProfileImageFile.Length > 0)
                            {
                                _logger.LogDebug("Profile POST: se recibió archivo. Name={Name} Length={Length} ContentType={CT}", ProfileImageFile.FileName, ProfileImageFile.Length, ProfileImageFile.ContentType);

                                try
                                {
                                    if (_s3Service == null)
                                    {
                                        _logger.LogError("IS3Service no está inyectado en HomeController.");
                                        ModelState.AddModelError(string.Empty, "Servicio de almacenamiento no disponible.");
                                        var socials = await _clanMemberSMService.GetByMemberIdAsync(model.Member.Id, cancellationToken).ConfigureAwait(false);
                                        model.MemberSocialMedias = socials?.Select(s => new ClanMemberSMViewModel { Id = s.Id, MemberId = s.MemberId, SocialMediaId = s.SocialMediaId, SocialMediaUrl = s.SocialMediaUrl, Enabled = s.Enabled }).ToList();
                                        return View(model);
                                    }

                                    var newKey = await _s3Service.UploadFileAsync(ProfileImageFile, cancellationToken).ConfigureAwait(false);
                                    if (string.IsNullOrWhiteSpace(newKey))
                                    {
                                        _logger.LogWarning("S3Service.UploadFileAsync devolvió key vacía para user {UserId}.", userId);
                                        ModelState.AddModelError(string.Empty, "No se pudo subir la imagen. Intenta de nuevo.");
                                        var socials = await _clanMemberSMService.GetByMemberIdAsync(model.Member.Id, cancellationToken).ConfigureAwait(false);
                                        model.MemberSocialMedias = socials?.Select(s => new ClanMemberSMViewModel { Id = s.Id, MemberId = s.MemberId, SocialMediaId = s.SocialMediaId, SocialMediaUrl = s.SocialMediaUrl, Enabled = s.Enabled }).ToList();
                                        return View(model);
                                    }

                                    _logger.LogInformation("S3: archivo subido correctamente. Key={Key} UserId={UserId}", newKey, userId);
                                    existingMember.ProfileImage = newKey;
                                    memberChanged = true;
                                    uploadedNewFile = true;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error subiendo imagen a S3 para UserId {UserId}", userId);
                                    ModelState.AddModelError(string.Empty, "Error subiendo la imagen. Intenta de nuevo más tarde.");
                                    var socials = await _clanMemberSMService.GetByMemberIdAsync(model.Member.Id, cancellationToken).ConfigureAwait(false);
                                    model.MemberSocialMedias = socials?.Select(s => new ClanMemberSMViewModel { Id = s.Id, MemberId = s.MemberId, SocialMediaId = s.SocialMediaId, SocialMediaUrl = s.SocialMediaUrl, Enabled = s.Enabled }).ToList();
                                    return View(model);
                                }
                            }
                            else
                            {
                                if (model.Member.ProfileImage != null
                                    && !string.Equals(existingMember.ProfileImage ?? string.Empty, model.Member.ProfileImage ?? string.Empty, StringComparison.Ordinal))
                                {
                                    existingMember.ProfileImage = model.Member.ProfileImage;
                                    memberChanged = true;
                                }
                            }

                            if (!string.Equals(existingMember.FirstName ?? string.Empty, model.Member.FirstName ?? string.Empty, StringComparison.Ordinal))
                            {
                                existingMember.FirstName = model.Member.FirstName;
                                memberChanged = true;
                            }

                            if (!string.Equals(existingMember.LastName ?? string.Empty, model.Member.LastName ?? string.Empty, StringComparison.Ordinal))
                            {
                                existingMember.LastName = model.Member.LastName;
                                memberChanged = true;
                            }

                            if (memberChanged)
                            {
                                db.ClanMembers.Update(existingMember);
                                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                                _logger.LogInformation("ClanMember {MemberId} actualizado por usuario {UserId}.", existingMember.Id, userId);
                            }

                            if (uploadedNewFile && !string.IsNullOrWhiteSpace(previousImageKey))
                            {
                                bool looksLikeS3Key = !(previousImageKey.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                                        || previousImageKey.StartsWith("/")
                                                        || previousImageKey.StartsWith("~"));

                                if (looksLikeS3Key && !string.Equals(previousImageKey, existingMember.ProfileImage, StringComparison.Ordinal))
                                {
                                    try
                                    {
                                        await _s3Service.DeleteObjectAsync(previousImageKey, cancellationToken).ConfigureAwait(false);
                                        _logger.LogInformation("Imagen previa {Key} eliminada de S3 para Member {MemberId}", previousImageKey, existingMember.Id);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "No se pudo eliminar imagen previa {Key} de S3 para Member {MemberId}", previousImageKey, existingMember.Id);
                                    }
                                }
                            }
                        }
                    }

                    if (model.MemberSocialMedias != null)
                    {
                        var currentSocials = await _clanMemberSMService.GetByMemberIdAsync(model.Member.Id, cancellationToken).ConfigureAwait(false) ?? new List<ClanMemberSocialMediaDTO>();

                        List<ClanMemberSocialMediaDTO> incomingDtos = (model.MemberSocialMedias ?? new List<ClanMemberSMViewModel>())
                            .Select(vm => new ClanMemberSocialMediaDTO
                            {
                                Id = vm.Id,
                                MemberId = vm.MemberId,
                                SocialMediaId = vm.SocialMediaId ?? string.Empty,
                                SocialMediaUrl = vm.SocialMediaUrl ?? string.Empty,
                                Enabled = vm.Enabled
                            }).ToList();

                        bool socialsEqual = AreSocialListsEqual(currentSocials, incomingDtos);
                        if (!socialsEqual)
                        {
                            try
                            {
                                await _clanMemberSMService.SyncForMemberAsync(model.Member.Id, incomingDtos, cancellationToken).ConfigureAwait(false);
                                _logger.LogInformation("Redes sociales sincronizadas para Member {MemberId} por usuario {UserId}.", model.Member.Id, userId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error sincronizando redes sociales para MemberId {MemberId}", model.Member.Id);
                                ModelState.AddModelError(string.Empty, "Error al guardar redes sociales.");
                                return View(model);
                            }
                        }
                    }
                }

                var updatedUser = await _userService.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
                if (updatedUser != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, updatedUser.Id.ToString()),
                        new Claim(ClaimTypes.Name, updatedUser.Nickname),
                        new Claim(ClaimTypes.Email, updatedUser.Email ?? string.Empty),
                        new Claim("roleid", updatedUser.RoleId.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, principal);
                }

                TempData["SuccessMessage"] = "Perfil actualizado correctamente.";
                return RedirectToAction(nameof(Profile));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Validación al actualizar perfil");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado actualizando perfil de usuario {UserId}", userId);
                ModelState.AddModelError(string.Empty, "Error inesperado al actualizar el perfil.");
                return View(model);
            }

            static bool AreSocialListsEqual(List<ClanMemberSocialMediaDTO> left, List<ClanMemberSocialMediaDTO> right)
            {
                if (left == null && right == null) return true;
                if (left == null) left = new List<ClanMemberSocialMediaDTO>();
                if (right == null) right = new List<ClanMemberSocialMediaDTO>();

                if (left.Count != right.Count) return false;

                var normalizedLeft = left.Select(s => new
                {
                    Id = s.Id,
                    SocialMediaId = (s.SocialMediaId ?? string.Empty).Trim(),
                    SocialMediaUrl = (s.SocialMediaUrl ?? string.Empty).Trim(),
                    Enabled = s.Enabled
                }).OrderBy(x => x.Id).ThenBy(x => x.SocialMediaId).ToList();

                var normalizedRight = right.Select(s => new
                {
                    Id = s.Id,
                    SocialMediaId = (s.SocialMediaId ?? string.Empty).Trim(),
                    SocialMediaUrl = (s.SocialMediaUrl ?? string.Empty).Trim(),
                    Enabled = s.Enabled
                }).OrderBy(x => x.Id).ThenBy(x => x.SocialMediaId).ToList();

                for (int i = 0; i < normalizedLeft.Count; i++)
                {
                    var a = normalizedLeft[i];
                    var b = normalizedRight[i];

                    if (a.Id != b.Id) return false;
                    if (!string.Equals(a.SocialMediaId, b.SocialMediaId, StringComparison.OrdinalIgnoreCase)) return false;
                    if (!string.Equals(a.SocialMediaUrl, b.SocialMediaUrl, StringComparison.OrdinalIgnoreCase)) return false;
                    if (a.Enabled != b.Enabled) return false;
                }

                return true;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatistics()
        {
            _logger.LogInformation("Manual statistics update requested.");

            try
            {
                await _pubgService.UpdateStatisticsAsync();
                _logger.LogInformation("UpdateStatisticsAsync finished successfully.");
                return Ok();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("UpdateStatisticsAsync cancelled.");
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateStatisticsAsync failed.");
                return StatusCode(500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> PlayerLifetimeStats(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return BadRequest("playerId es requerido.");

            try
            {
                var json = await _pubgService.GetPlayerLifetimeStatsAsync(playerId, HttpContext.RequestAborted);
                if (json == null)
                    return StatusCode(502, "No se pudo obtener datos desde PUBG.");

                return Content(json, "application/json");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("PlayerLifetimeStats cancelado.");
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener PlayerLifetimeStats para {PlayerId}", playerId);
                return StatusCode(500);
            }
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}