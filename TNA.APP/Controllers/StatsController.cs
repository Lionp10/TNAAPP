using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TNA.BLL.Services.Interfaces;
using TNA.BLL.DTOs;
using System.Collections.Generic;
using System.Text.Json;

namespace TNA.APP.Controllers
{
    public class StatsController : Controller
    {
        private readonly ILogger<StatsController> _logger;
        private readonly IPubgService _pubgService;

        public StatsController(ILogger<StatsController> logger, IPubgService pubgService)
        {
            _logger = logger;
            _pubgService = pubgService;
        }

        public IActionResult Index()
        {
            return View();
        }
 
        public async Task<IActionResult> PlayerStats(string playerId, string nick)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return BadRequest("playerId es requerido.");

            ViewData["PlayerId"] = playerId;
            ViewData["PlayerNick"] = nick ?? string.Empty;

            // Obtener las últimas 20 partidas y pasarlas como modelo a la vista (patrón MVC)
            IEnumerable<RecentGameStatsDTO> recentGames = Array.Empty<RecentGameStatsDTO>();
            try
            {
                recentGames = await _pubgService.GetOrUpdateRecentGamesAsync(playerId, HttpContext.RequestAborted);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("PlayerStats: obtención de partidas cancelada.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PlayerStats: error obteniendo partidas recientes para {PlayerId}", playerId);
            }

            // Obtener LifetimeStats (JSON) en el controlador para pasarlo a la vista (evita llamadas extra en cliente)
            string? lifetimeJson = null;
            try
            {
                lifetimeJson = await _pubgService.GetPlayerLifetimeStatsAsync(playerId, HttpContext.RequestAborted);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("PlayerStats: obtención de lifetime cancelada.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PlayerStats: error obteniendo PlayerLifetimeStats para {PlayerId}", playerId);
            }

            ViewData["LifetimeJson"] = lifetimeJson ?? string.Empty;

            return View(recentGames);
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

        [HttpGet]
        public async Task<IActionResult> PlayerRecentGames(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return BadRequest("playerId es requerido.");

            try
            {
                var list = await _pubgService.GetOrUpdateRecentGamesAsync(playerId, HttpContext.RequestAborted);
                // Devuelve JSON con el listado de RecentGameStatsDTO (0..20)
                return Ok(list ?? Array.Empty<RecentGameStatsDTO>());
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("PlayerRecentGames cancelado.");
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener PlayerRecentGames para {PlayerId}", playerId);
                return StatusCode(500);
            }
        }
    }
}