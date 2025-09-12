namespace TNA.APP.Models
{
    public class ClanMemberSMViewModel
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public string? SocialMediaId { get; set; }
        public string? SocialMediaUrl { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
