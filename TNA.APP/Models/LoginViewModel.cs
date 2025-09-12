using System.ComponentModel.DataAnnotations;

namespace TNA.APP.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;

        // URL a donde volver tras login
        public string? ReturnUrl { get; set; }
    }
}
