namespace TNA.DAL.Entities
{
    public class Match
    {
        public int Id { get; set; } // PK autonumerico
        public required string MatchId { get; set; } // nvarchar(128) not null
        public required string MapName { get; set; } // nvarchar(50) not null
        public required string CreatedAt { get; set; } // nvarchar(50) not null
    }
}
