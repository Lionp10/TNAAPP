using System.ComponentModel.DataAnnotations;

namespace TNA.APP.Models
{
    public class ClanMemberViewModel
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        // mostrado en UI como readonly, no lo exigimos en validación del viewmodel
        public string? Nickname { get; set; }

        // readonly en la UI
        public string? PlayerId { get; set; }

        public string? ClanId { get; set; }
        public string? ProfileImage { get; set; }
        public bool Enabled { get; set; }
    }
}
