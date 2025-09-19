namespace TNA.DAL.Entities
{
    public class Match
    {
        public int Id { get; set; } 
        public required string MatchId { get; set; } 
        public required string MapName { get; set; } 
        public required string CreatedAt { get; set; } 
    }
}
