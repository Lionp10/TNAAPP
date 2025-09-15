using System.Security.Claims;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TNA.APP.Models;
using TNA.BLL.Services.Interfaces;
using TNA.BLL.DTOs;
using TNA.DAL.Entities;

namespace TNA.APP.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserService _userService;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<AuthController> _logger;
        private readonly IDataProtector _protector;
        private readonly IEmailService _emailService; // <-- nueva dependencia

        public AuthController(
            IUserService userService,
            IPasswordHasher<User> passwordHasher,
            ILogger<AuthController> logger,
            IDataProtectionProvider dataProtectionProvider,
            IEmailService emailService) // <-- inyectado
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            if (dataProtectionProvider == null) throw new ArgumentNullException(nameof(dataProtectionProvider));
            _protector = dataProtectionProvider.CreateProtector("TNA.APP.PasswordReset");
        }

        // GET: /Auth/Index
        [HttpGet]
        public IActionResult Index(string returnUrl = null)
        {
            var vm = new LoginViewModel { ReturnUrl = returnUrl };
            return View(vm);
        }

        // POST: /Auth/Index (login)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var userDto = await _userService.GetByEmailAsync(model.Email);
                if (userDto == null)
                {
                    ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
                    return View(model);
                }

                var repo = HttpContext.RequestServices.GetService(typeof(TNA.DAL.Repositories.Interfaces.IUserRepository)) as TNA.DAL.Repositories.Interfaces.IUserRepository;
                if (repo == null)
                {
                    _logger.LogError("IUserRepository no está registrado - imposible verificar contraseña.");
                    ModelState.AddModelError(string.Empty, "Error interno de autenticación.");
                    return View(model);
                }

                var userEntity = await repo.GetByEmailAsync(model.Email);
                if (userEntity == null)
                {
                    ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
                    return View(model);
                }

                var verify = _passwordHasher.VerifyHashedPassword(userEntity, userEntity.PasswordHash, model.Password);
                if (verify == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
                    return View(model);
                }

                if (!userEntity.Enabled)
                {
                    ModelState.AddModelError(string.Empty, "La cuenta está deshabilitada.");
                    return View(model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userEntity.Id.ToString()),
                    new Claim(ClaimTypes.Name, userEntity.Nickname),
                    new Claim(ClaimTypes.Email, userEntity.Email ?? ""),
                    new Claim("roleid", userEntity.RoleId.ToString())
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var props = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(model.RememberMe ? 72 : 8)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

                _logger.LogInformation("Usuario {Email} autenticado.", userEntity.Email);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login para {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Error inesperado al iniciar sesión.");
                return View(model);
            }
        }

        // POST: /Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // redirige a Auth/Index tal como solicitaste
            return RedirectToAction("Index", "Auth");
        }

        // GET: /Auth/Register  (esqueleto; puedes ampliarlo)
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var email = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;
                var username = model.Username?.Trim() ?? string.Empty;

                // Validar que el email no exista
                if (await _userService.EmailExistsAsync(email, cancellationToken).ConfigureAwait(false))
                {
                    ModelState.AddModelError(nameof(model.Email), "Este email ya está registrado.");
                    return View(model);
                }

                // Opcional: validar que el nickname no exista
                if (await _userService.NicknameExistsAsync(username, cancellationToken).ConfigureAwait(false))
                {
                    ModelState.AddModelError(nameof(model.Username), "El nombre de usuario ya está en uso.");
                    return View(model);
                }

                var dto = new UserCreateDTO
                {
                    Nickname = username,
                    Email = email,
                    Password = model.Password,
                    RoleId = 4,
                    MemberId = null,
                    Enabled = true
                };

                var newUserId = await _userService.CreateAsync(dto, cancellationToken).ConfigureAwait(false);

                // Auto-login del usuario creado
                var created = await _userService.GetByIdAsync(newUserId, cancellationToken).ConfigureAwait(false);
                if (created != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, created.Id.ToString()),
                        new Claim(ClaimTypes.Name, created.Nickname),
                        new Claim(ClaimTypes.Email, created.Email ?? string.Empty),
                        new Claim("roleid", created.RoleId.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    var props = new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
                    _logger.LogInformation("Nuevo usuario creado y autenticado: {Email}", created.Email);
                }

                // Redirect seguro
                if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return LocalRedirect(model.ReturnUrl);

                return RedirectToAction("Index", "Home");
            }
            catch (InvalidOperationException ex)
            {
                // Errores de validación de negocio (email/nickname duplicados)
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Error creando usuario desde Register");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado creando usuario desde Register");
                ModelState.AddModelError(string.Empty, "Error inesperado al crear la cuenta.");
                return View(model);
            }
        }

        // GET: /Auth/ForgotPassword (esqueleto)
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Auth/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var email = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;

                // No revelar si el email existe o no: mostrar siempre el mismo mensaje.
                var userDto = await _userService.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);

                const string userMessage = "Si existe una cuenta asociada a ese email, recibirás instrucciones para restablecer la contraseña.";

                if (userDto == null)
                {
                    // Simular comportamiento idéntico al caso exitoso para evitar enumeración de cuentas
                    ViewBag.Message = userMessage;
                    return View();
                }

                // Generar token protegido que incluye id y expiración (ej. 1 hora)
                var expiry = DateTimeOffset.UtcNow.AddHours(1);
                var payload = $"{userDto.Id}|{expiry.ToUnixTimeSeconds()}|{Guid.NewGuid()}";
                var protectedToken = _protector.Protect(payload);
                var tokenEncoded = WebUtility.UrlEncode(protectedToken);

                // Construir la URL de restablecimiento (necesitarás implementar ResetPassword)
                var resetUrl = Url.Action("ResetPassword", "Auth", new { token = tokenEncoded, email = email }, Request.Scheme);

                // Intentar enviar email si existe un servicio compatible registrado.
                if (_emailService != null)
                {
                    var html = $"<p>Hola,</p><p>Para restablecer tu contraseña haz clic en el siguiente enlace:</p><p><a href=\"{resetUrl}\">Restablecer contraseña</a></p><p>Si no solicitaste este cambio, ignora este correo.</p>";
                    try
                    {
                        await _emailService.SendEmailAsync(email, "Restablecer contraseña", html, true, cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation("Se envió email de restablecimiento a {Email} usando IEmailService.", email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error enviando email de restablecimiento a {Email}", email);
                        // no devolver error al usuario: mostramos el mismo mensaje por seguridad
                    }
                }
                else
                {
                    _logger.LogInformation("Reset link for {Email}: {Url}", email, resetUrl);
                }

                ViewBag.Message = userMessage;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando token de recuperación para {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Error inesperado al procesar la solicitud.");
                return View(model);
            }
        }
    }
}
