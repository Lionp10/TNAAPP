namespace TNA.DAL.Entities
{
    public class PlayerLifetime
    {
        public int Id { get; set; }
        public required string PlayerId { get; set; }
        public DateTime DateOfUpdate { get; set; }
        public string? LifetimeJson { get; set; }
    }
}
