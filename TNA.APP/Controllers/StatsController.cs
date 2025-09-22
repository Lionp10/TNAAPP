using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TNA.APP.Controllers
{
    public class StatsController : Controller
    {
        private readonly ILogger<StatsController> _logger;

        public StatsController(ILogger<StatsController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Player(string playerId, string nick)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return BadRequest("playerId es requerido.");

            ViewData["PlayerId"] = playerId;
            ViewData["PlayerNick"] = nick ?? string.Empty;
            return View();
        }
    }
}
