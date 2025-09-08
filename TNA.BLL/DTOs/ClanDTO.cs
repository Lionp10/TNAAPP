namespace TNA.BLL.DTOs
{
    public class ClanDTO
    {
        public int Id { get; set; }
        public required string ClanId { get; set; } 
        public required string ClanName { get; set; } 
        public required string ClanTag { get; set; } 
        public int ClanLevel { get; set; } 
        public int ClanMemberCount { get; set; } 
        public DateTime DateOfUpdate { get; set; }
        public bool Enabled { get; set; } 
    }
}
