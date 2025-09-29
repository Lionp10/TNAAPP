using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TNA.BLL.DTOs
{
    public class RecentGameStatsDTO
    {
        // General Data
        public int Id { get; set; }
        public string PlayerId { get; set; }
        public DateTime DateOfUpdate { get; set; }

        // Match Data
        public string MatchId { get; set; }
        public string CreatedAt { get; set; }
        public string MapName { get; set; }
        public string GameMode { get; set; }
        public bool IsCustomMatch { get; set; }

        // Players Data
        public int DBNOs { get; set; }
        public int Assists { get; set; }
        public int Boots { get; set; }
        public decimal DamageDealt { get; set; }
        public int HeadshotsKills { get; set; }
        public int Heals { get; set; }
        public int KillPlace { get; set; }
        public int KillStreaks { get; set; }
        public int Kills { get; set; }
        public decimal LongestKill { get; set; }
        public int Revives { get; set; }
        public decimal RideDistance { get; set; }
        public decimal SwimDistance { get; set; }
        public decimal WalkDistance { get; set; }
        public int RoadKills { get; set; }
        public int TeamKills { get; set; }
        public decimal TimeSurvived { get; set; }
        public int VehicleDestroys { get; set; }
        public int WeaponsAcquired { get; set; }
        public int WinPlace { get; set; }
    }
}
