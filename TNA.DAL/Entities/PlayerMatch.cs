namespace TNA.DAL.Entities
{
    public class PlayerMatch
    {
        public int Id { get; set; }
        public int DBNOs { get; set; } 
        public int Assists { get; set; } 
        public int Kills { get; set; } 
        public int HeadshotsKills { get; set; } 
        public decimal DamageDealt { get; set; } 
        public int Revive { get; set; } 
        public int TeamKill { get; set; }
        public decimal TimeSurvived { get; set; } 
        public int WinPlace { get; set; } 
        public required string MatchCreatedAt { get; set; }
        public required string PlayerId { get; set; }
        public required string MatchId { get; set; } 
    }
}
