using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TNA.BLL.DTOs;
using TNA.BLL.Services.Interfaces;

namespace TNA.APP.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: /User
        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var users = await _userService.GetAllAsync(cancellationToken).ConfigureAwait(false);
            return View(users);
        }

        // GET: /User/Details/5
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
        {
            var user = await _userService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (user is null) return NotFound();
            return View(user);
        }

        // GET: /User/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateDTO model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var id = await _userService.CreateAsync(model, cancellationToken).ConfigureAwait(false);
                TempData["SuccessMessage"] = "Usuario creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Error creating user");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating user");
                ModelState.AddModelError(string.Empty, "Error inesperado al crear el usuario.");
                return View(model);
            }
        }

        // GET: /User/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var user = await _userService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (user is null) return NotFound();

            var dto = new UserUpdateDTO
            {
                Id = user.Id,
                Nickname = user.Nickname,
                Email = user.Email,
                RoleId = user.RoleId,
                MemberId = user.MemberId,
                Enabled = user.Enabled
                // Password left null (only set when changing)
            };

            return View(dto);
        }

        // POST: /User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserUpdateDTO model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _userService.UpdateAsync(model, cancellationToken).ConfigureAwait(false);
                TempData["SuccessMessage"] = "Usuario actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Validation error updating user");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating user");
                ModelState.AddModelError(string.Empty, "Error inesperado al actualizar el usuario.");
                return View(model);
            }
        }

        // GET: /User/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            var user = await _userService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (user is null) return NotFound();
            return View(user);
        }

        // POST: /User/Delete/5  (soft delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _userService.SoftDeleteAsync(id, cancellationToken).ConfigureAwait(false);
                TempData["SuccessMessage"] = "Usuario deshabilitado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting user");
                TempData["ErrorMessage"] = "Error inesperado al eliminar el usuario.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Optional: hard delete endpoint (dangerous; keep protected)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDelete(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _userService.HardDeleteAsync(id, cancellationToken).ConfigureAwait(false);
                TempData["SuccessMessage"] = "Usuario eliminado permanentemente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error hard-deleting user");
                TempData["ErrorMessage"] = "Error inesperado al eliminar permanentemente el usuario.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
