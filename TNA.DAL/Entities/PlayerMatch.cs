namespace TNA.DAL.Entities
{
    public class PlayerMatch
    {
        public int Id { get; set; } // PK autonumerico
        public int DBNOs { get; set; } // int not null
        public int Assists { get; set; } // int not null
        public int Kills { get; set; } // int not null
        public int HeadshotsKills { get; set; } // int not null
        public decimal DamageDealt { get; set; } // decimal(18,2) not null
        public int Revive { get; set; } // int not null
        public int TeamKill { get; set; } // int not null
        public decimal TimeSurvived { get; set; } // decimal(18,2) not null
        public int WinPlace { get; set; } // int not null
        public required string MatchCreatedAt { get; set; } // nvarchar(50) not null

        // Nuevas propiedades para relacionar estadística con jugador y partido
        public required string PlayerId { get; set; } // nvarchar(128) not null
        public required string MatchId { get; set; } // nvarchar(max) not null (puede ajustarse a longitud fija)
    }
}
