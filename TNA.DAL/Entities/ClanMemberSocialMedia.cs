namespace TNA.DAL.Entities
{
    public class ClanMemberSocialMedia
    {
        public int Id { get; set; } // PK autonumerico
        public int MemberId { get; set; } // int not null
        public required string SocialMediaId { get; set; } // char(2) not null
        public required string SocialMediaUrl { get; set; } // nvarchar(MAX) not null
        public bool Enabled { get; set; } // bit not null
    }
}
