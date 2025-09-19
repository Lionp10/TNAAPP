using System.ComponentModel.DataAnnotations;

namespace TNA.APP.Models
{
    public class ClanMemberViewModel
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Nickname { get; set; }
        public string? PlayerId { get; set; }
        public string? ClanId { get; set; }
        public string? ProfileImage { get; set; }
        public bool Enabled { get; set; }
    }
}
