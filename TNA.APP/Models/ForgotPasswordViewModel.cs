using System.ComponentModel.DataAnnotations;

namespace TNA.APP.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Ingresa un email válido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }
}
