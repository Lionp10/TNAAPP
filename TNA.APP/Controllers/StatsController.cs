using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TNA.BLL.Services.Interfaces;

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

        // Acción renombrada: ahora se llama LifetimeStats y devuelve la vista correspondiente
        public IActionResult LifetimeStats(string playerId, string nick)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return BadRequest("playerId es requerido.");

            ViewData["PlayerId"] = playerId;
            ViewData["PlayerNick"] = nick ?? string.Empty;
            return View("LifetimeStats");
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
    }
}
