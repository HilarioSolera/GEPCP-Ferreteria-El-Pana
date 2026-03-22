using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class PagoFeriado
    {
        public int PagoFeriadoId { get; set; }

        [Required]
        public int EmpleadoId { get; set; }

        [Required]
        public int FeriadoId { get; set; }

        [Required]
        public int PeriodoPagoId { get; set; }

        [Display(Name = "Monto Total (₡)")]
        public decimal MontoTotal { get; set; }

        // ── Navegación ─────────────────────────────────
        public Empleado Empleado { get; set; } = null!;
        public Feriado Feriado { get; set; } = null!;
        public PeriodoPago PeriodoPago { get; set; } = null!;
    }
}