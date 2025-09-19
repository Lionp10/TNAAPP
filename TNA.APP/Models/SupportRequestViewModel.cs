using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace TNA.APP.Models
{
    public class SupportRequestViewModel
    {
        [Required(ErrorMessage = "Nombre requerido")]
        [StringLength(60)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Apellido requerido")]
        [StringLength(60)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email requerido")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe tu mensaje")]
        [StringLength(5000, ErrorMessage = "Mensaje demasiado largo")]
        public string Message { get; set; } = string.Empty;

        public List<IFormFile>? Attachments { get; set; }
    }
}
