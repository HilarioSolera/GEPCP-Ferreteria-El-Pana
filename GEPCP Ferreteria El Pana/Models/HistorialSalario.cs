using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class HistorialSalario
    {
        public int HistorialSalarioId { get; set; }

        [Required]
        public int EmpleadoId { get; set; }

        [Required]
        [Display(Name = "Salario Anterior (₡)")]
        public decimal SalarioAnterior { get; set; }

        [Required]
        [Display(Name = "Salario Nuevo (₡)")]
        public decimal SalarioNuevo { get; set; }

        [Required]
        [Display(Name = "Fecha de Cambio")]
        public DateTime FechaCambio { get; set; } = DateTime.Now;

        [StringLength(200)]
        [Display(Name = "Motivo")]
        public string? Motivo { get; set; }

        [StringLength(100)]
        [Display(Name = "Modificado por")]
        public string? ModificadoPor { get; set; }

        // ── Navegación ────────────────────────────────────────────────────────
        public Empleado Empleado { get; set; } = null!;
    }
}