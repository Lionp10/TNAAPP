namespace TNA.BLL.DTOs
{
    public class MatchDTO
    {
        public int Id { get; set; } 
        public required string MatchId { get; set; } 
        public required string MapName { get; set; } 
        public required string CreatedAt { get; set; } 
    }
}
