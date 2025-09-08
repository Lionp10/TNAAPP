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
        // Cambiado a int: ahora se almacena el promedio de WinPlace redondeado
        public int AverageWinPlace { get; set; }
        /// <summary>
        /// Puntuación final entre 0.00 y 10.00 (mejor = 10)
        /// </summary>
        public decimal TotalPoints { get; set; }

        // Nueva propiedad calculada para presentación: HH:mm:ss
        public string TotalTimeSurvivedFormatted => TimeFormatter.FormatSecondsToHms(TotalTimeSurvived);
    }
}
