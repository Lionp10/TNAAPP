using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;
using TNA.APP.Models;
using TNA.BLL.Services.Interfaces;

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

        public HomeController(ILogger<HomeController> logger, IClanService clanService, IClanMemberService clanMemberService, IClanMemberSMService clanMemberSMService, IPubgService pubgService, IPlayerMatchService playerMatchService)
        {
            _logger = logger;
            _clanService = clanService;
            _clanMemberService = clanMemberService;
            _clanMemberSMService = clanMemberSMService;
            _pubgService = pubgService;
            _playerMatchService = playerMatchService;
        }

        // GET: /Home/Index?range=day|week|month|all
        public async Task<IActionResult> Index(string range = "day")
        {
            var clan = await _pubgService.GetOrUpdateClanAsync();

            // Calcular rango en zona GMT-3
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
                    // semana calendario anterior (Lun-Dom) en GMT-3
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

            var ranking = await _playerMatchService.GetRankingAsync(startUtc, endUtc);

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

        public async Task<IActionResult> Members()
        {
            var clanMembers = await _clanMemberService.GetActiveMembersAsync();

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

        public async Task<IActionResult> Tournaments()
        {            
            return View();
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

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}