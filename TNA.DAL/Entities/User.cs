using System;

namespace TNA.DAL.Entities
{
    public class User
    {
        public int Id { get; set; } 

        public required string Nickname { get; set; } 
        public required string Email { get; set; } 
        public required string PasswordHash { get; set; }
        public required int RoleId { get; set; }

        public int? MemberId { get; set; } 
        public DateTime? CreatedAt { get; set; } 
        public bool Enabled { get; set; } 

        public ClanMember? Member { get; set; }
    }
}
