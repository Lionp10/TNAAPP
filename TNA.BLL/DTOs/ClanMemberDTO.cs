namespace TNA.BLL.DTOs
{
    public class ClanMemberDTO
    {
        public int Id { get; set; }
        public string? FirstName { get; set; } 
        public string? LastName { get; set; } 
        public required string Nickname { get; set; } 
        public string? Email { get; set; } 
        public required string PlayerId { get; set; } 
        public required string ClanId { get; set; } 
        public string? ProfileImage { get; set; } 
        public bool Enabled { get; set; }
    }
}
