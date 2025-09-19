using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TNA.BLL.DTOs;
using TNA.BLL.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using TNA.DAL.DbContext;
using TNA.DAL.Entities;
using TNA.APP.Models;
using System.Linq;
using System.Collections.Generic;

namespace TNA.APP.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;
        private const int PageSizeConst = 10;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;

            var users = await _userService.GetAllAsync(cancellationToken).ConfigureAwait(false);
            var total = users?.Count() ?? 0;

            var pageSize = PageSizeConst;
            var items = (users ?? new List<UserDTO>())
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new PagedListViewModel<UserDTO>(items, page, pageSize, total);

            return View(vm);
        }

        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
        {
            var user = await _userService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (user is null) return NotFound();
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {
            await PopulateRolesAndMembers(selectedRoleId: 4, selectedMemberId: null, currentUserId: null, cancellationToken);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateDTO model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateRolesAndMembers(model.RoleId, model.MemberId, null, cancellationToken);
                return View(model);
            }

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
                await PopulateRolesAndMembers(model.RoleId, model.MemberId, null, cancellationToken);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating user");
                ModelState.AddModelError(string.Empty, "Error inesperado al crear el usuario.");
                await PopulateRolesAndMembers(model.RoleId, model.MemberId, null, cancellationToken);
                return View(model);
            }
        }

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
            };

            await PopulateRolesAndMembers(dto.RoleId, dto.MemberId, dto.Id, cancellationToken);
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserUpdateDTO model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateRolesAndMembers(model.RoleId, model.MemberId, model.Id, cancellationToken);
                return View(model);
            }

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
                await PopulateRolesAndMembers(model.RoleId, model.MemberId, model.Id, cancellationToken);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating user");
                ModelState.AddModelError(string.Empty, "Error inesperado al actualizar el usuario.");
                await PopulateRolesAndMembers(model.RoleId, model.MemberId, model.Id, cancellationToken);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            var user = await _userService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (user is null) return NotFound();
            return View(user);
        }

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

        private async Task PopulateRolesAndMembers(int? selectedRoleId, int? selectedMemberId, int? currentUserId, CancellationToken cancellationToken = default)
        {
            var db = HttpContext.RequestServices.GetService(typeof(TNADbContext)) as TNADbContext;
            if (db != null)
            {
                var roles = await db.Roles
                    .AsNoTracking()
                    .OrderBy(r => r.Id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                ViewBag.Roles = new SelectList(roles, "Id", "Description", selectedRoleId);

                var assignedMemberIdsQuery = db.Users.AsNoTracking().Where(u => u.MemberId.HasValue);
                if (currentUserId.HasValue)
                    assignedMemberIdsQuery = assignedMemberIdsQuery.Where(u => u.Id != currentUserId.Value);

                var assignedMemberIds = await assignedMemberIdsQuery
                    .Select(u => u.MemberId!.Value)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                var availableMembers = await db.ClanMembers
                    .AsNoTracking()
                    .Where(m => m.Enabled && !assignedMemberIds.Contains(m.Id))
                    .OrderBy(m => m.Nickname)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                ViewBag.Members = new SelectList(availableMembers, "Id", "Nickname", selectedMemberId);
            }
            else
            {
                ViewBag.Roles = new SelectList(Array.Empty<Role>(), "Id", "Description", selectedRoleId);
                ViewBag.Members = new SelectList(Array.Empty<ClanMember>(), "Id", "Nickname", selectedMemberId);
            }
        }
    }
}
