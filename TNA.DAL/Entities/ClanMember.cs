namespace TNA.DAL.Entities
{
    public class ClanMember
    {
        public int Id { get; set; } // PK autonumerico
        public string? FirstName { get; set; } // nvarchar(50) null
        public string? LastName { get; set; } // nvarchar(50) null
        public required string Nickname { get; set; } // nvarchar(50) null
        public string? Email { get; set; } // nvarchar(50) null
        public required string PlayerId { get; set; } // nvarchar(128) not null
        public required string ClanId { get; set; } // nvarchar(128) not null
        public string? ProfileImage { get; set; } // nvarchar(MAX) null
        public bool Enabled { get; set; } // bit not null
    }
}
