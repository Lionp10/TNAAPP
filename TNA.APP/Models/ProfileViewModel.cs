using System.ComponentModel.DataAnnotations;
using TNA.BLL.DTOs;

namespace TNA.APP.Models
{
    public class ProfileViewModel
    {
        // Usuario
        public int Id { get; set; }

        [Required(ErrorMessage = "El nickname es obligatorio")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 50 caracteres")]
        [Display(Name = "Nickname")]
        public string Nickname { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingresa un email válido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [Display(Name = "Nueva contraseña (opcional)")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Confirmar nueva contraseña")]
        public string? ConfirmPassword { get; set; }

        // Si el usuario está relacionado con un ClanMember
        public int? MemberId { get; set; }

        // ViewModel del miembro (si aplica). No usar DTOs aquí evita validación implícita no deseada.
        public ClanMemberViewModel? Member { get; set; }

        // Redes sociales del miembro (máx 5)
        public List<ClanMemberSMViewModel>? MemberSocialMedias { get; set; } = new();
    }
}
