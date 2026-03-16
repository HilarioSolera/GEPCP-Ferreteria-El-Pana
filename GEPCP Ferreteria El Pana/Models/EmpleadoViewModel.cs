using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class EmpleadoViewModel
    {
        public int EmpleadoId { get; set; }

        [Required(ErrorMessage = "La cédula es obligatoria")]
        [StringLength(20)]
        [Display(Name = "Cédula")]
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El primer apellido es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Primer Apellido")]
        public string PrimerApellido { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Segundo Apellido")]
        public string? SegundoApellido { get; set; }

        [Required(ErrorMessage = "El puesto es obligatorio")]
        [Display(Name = "Puesto")]
        public string Puesto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El salario base es obligatorio")]
        [Range(1, 99999999, ErrorMessage = "El salario debe ser mayor a cero")]
        [Display(Name = "Salario Base (₡)")]
        public decimal SalarioBase { get; set; }

        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Activo";

        // ── NUEVOS CAMPOS ──────────────────────────────
        [StringLength(20)]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        [Display(Name = "Teléfono / Celular")]
        public string? Telefono { get; set; }

        [StringLength(150)]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [Display(Name = "Correo Electrónico")]
        public string? CorreoElectronico { get; set; }
        // ───────────────────────────────────────────────
    }
}