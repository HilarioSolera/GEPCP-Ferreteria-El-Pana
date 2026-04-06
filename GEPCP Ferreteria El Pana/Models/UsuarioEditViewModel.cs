using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class UsuarioEditViewModel
    {
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
        [Display(Name = "Correo Electrónico")]
        public string? CorreoElectronico { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio.")]
        [Display(Name = "Rol")]
        public string Rol { get; set; } = string.Empty;



    }
}