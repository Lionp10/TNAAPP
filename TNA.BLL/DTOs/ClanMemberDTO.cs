namespace TNA.BLL.DTOs
{
    public class ClanMemberDTO
    {
        public int Id { get; set; }
        public string? FirstName { get; set; } 
        public string? LastName { get; set; } 
        public required string Nickname { get; set; } 
        public required string PlayerId { get; set; } 
        public required string ClanId { get; set; } 
        public string? ProfileImage { get; set; } 
        public bool Enabled { get; set; }
    }

    public class ClanMemberCreateDTO
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public required string Nickname { get; set; }
        public required string PlayerId { get; set; }
        public required string ClanId { get; set; }
        public string? ProfileImage { get; set; }
        public bool Enabled { get; set; } = true;
    }

    public class ClanMemberUpdateDTO
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