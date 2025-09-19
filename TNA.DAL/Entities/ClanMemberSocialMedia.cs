namespace TNA.DAL.Entities
{
    public class ClanMemberSocialMedia
    {
        public int Id { get; set; } 
        public int MemberId { get; set; }
        public required string SocialMediaId { get; set; } 
        public required string SocialMediaUrl { get; set; } 
        public bool Enabled { get; set; } 
    }
}
