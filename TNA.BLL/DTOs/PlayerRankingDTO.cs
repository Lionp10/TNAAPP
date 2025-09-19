using TNA.BLL.Utils;

namespace TNA.BLL.DTOs
{
    public class PlayerRankingDTO
    {
        public required string PlayerId { get; set; }
        public string? PlayerNickname { get; set; }
        public int MatchesCount { get; set; }
        public int TotalDBNOs { get; set; }
        public int TotalAssists { get; set; }
        public int TotalKills { get; set; }
        public int TotalHeadshotsKills { get; set; }
        public decimal TotalDamageDealt { get; set; }
        public int TotalRevives { get; set; }
        public int TotalTeamKill { get; set; }
        public decimal TotalTimeSurvived { get; set; }
        public int AverageWinPlace { get; set; }
        public decimal TotalPoints { get; set; }
        public string TotalTimeSurvivedFormatted => TimeFormatter.FormatSecondsToHms(TotalTimeSurvived);
    }
}
