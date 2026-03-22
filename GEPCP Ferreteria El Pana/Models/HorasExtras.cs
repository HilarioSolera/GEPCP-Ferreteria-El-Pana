using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class HorasExtras
    {
        public int HorasExtrasId { get; set; }

        [Required]
        public int EmpleadoId { get; set; }

        [Required]
        public int PeriodoPagoId { get; set; }

        [Required]
        [Display(Name = "Total Horas")]
        public decimal TotalHoras { get; set; }

        [Required]
        [Display(Name = "Valor Hora (₡)")]
        public decimal ValorHora { get; set; }

        [Required]
        [Display(Name = "Porcentaje")]
        public decimal Porcentaje { get; set; } = 1.5m;

        [Display(Name = "Monto Total (₡)")]
        public decimal MontoTotal { get; set; }

        // ── Navegación ─────────────────────────────────
        public Empleado Empleado { get; set; } = null!;
        public PeriodoPago PeriodoPago { get; set; } = null!;
    }
}