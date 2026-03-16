using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class Empleado
    {
        public int EmpleadoId { get; set; }

        [Required]
        [StringLength(20)]
        public string Cedula { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string PrimerApellido { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SegundoApellido { get; set; }

        [Required]
        [StringLength(100)]
        public string Puesto { get; set; } = string.Empty;

        [Required]
        public decimal SalarioBase { get; set; }

        public bool Activo { get; set; } = true;

        // ── NUEVOS CAMPOS ──────────────────────────────
        [StringLength(20)]
        [Display(Name = "Teléfono / Celular")]
        public string? Telefono { get; set; }

        [StringLength(150)]
        [EmailAddress]
        [Display(Name = "Correo Electrónico")]
        public string? CorreoElectronico { get; set; }
        // ───────────────────────────────────────────────

        // Navegación
        public ICollection<Comision> Comisiones { get; set; } = new List<Comision>();
        public ICollection<Planilla> Planillas { get; set; } = new List<Planilla>();
        public ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
    }
}