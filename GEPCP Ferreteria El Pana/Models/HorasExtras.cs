using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class HorasExtras
    {
        public int HorasExtrasId { get; set; }

        [Required(ErrorMessage = "Seleccioná un empleado válido.")]
        public int EmpleadoId { get; set; }

        [Required(ErrorMessage = "Seleccioná un período válido.")]
        public int PeriodoPagoId { get; set; }

        [Required(ErrorMessage = "El total de horas es obligatorio.")]
        [Range(0.5, 999.99, ErrorMessage = "Las horas deben estar entre 0.5 y 999.99.")]
        [Display(Name = "Total Horas")]
        public decimal TotalHoras { get; set; }

        [Required(ErrorMessage = "El valor hora es obligatorio.")]
        [Display(Name = "Valor Hora (₡)")]
        public decimal ValorHora { get; set; }

        [Required(ErrorMessage = "El porcentaje es obligatorio.")]
        [Range(1.0, 3.0, ErrorMessage = "El porcentaje debe estar entre 1.0 y 3.0.")]
        [Display(Name = "Porcentaje")]
        public decimal Porcentaje { get; set; } = 1.5m;

        [Display(Name = "Monto Total (₡)")]
        public decimal MontoTotal { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria.")]
        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; } = DateTime.Today;

        // ── Navegación ─────────────────────────────────
        public Empleado Empleado { get; set; } = null!;
        public PeriodoPago PeriodoPago { get; set; } = null!;
    }
}