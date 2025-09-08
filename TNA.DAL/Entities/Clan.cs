namespace TNA.DAL.Entities
{
    public class Clan
    {
        public int Id { get; set; } // PK autonumerico
        public required string ClanId { get; set; } // nvarchar(128) not null
        public required string ClanName { get; set; } // nvarchar(50) not null
        public required string ClanTag { get; set; } // nvarchar(10) not null
        public int ClanLevel { get; set; } // int not null
        public int ClanMemberCount { get; set; } // int not null
        public DateTime DateOfUpdate { get; set; } // datetime not null
        public bool Enabled { get; set; } // bit not null
    }
}
