namespace TNA.BLL.DTOs
{
    public class UserDTO
    {
        public int Id { get; set; }
        public required string Nickname { get; set; }
        public required string Email { get; set; }
        public int RoleId { get; set; }
        public int? MemberId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Enabled { get; set; }
    }

    public class UserCreateDTO
    {
        public required string Nickname { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }

        public int RoleId { get; set; } = 0;
        public int? MemberId { get; set; }
        public bool Enabled { get; set; } = true;

    }

    public class UserUpdateDTO
    {
        public int Id { get; set; }
        public required string Nickname { get; set; }
        public required string Email { get; set; }
        public string? Password { get; set; }

        public int RoleId { get; set; }
        public int? MemberId { get; set; }
        public bool Enabled { get; set; }

    }
}
