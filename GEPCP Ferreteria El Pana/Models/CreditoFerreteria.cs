using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class CreditoFerreteria
    {

        public ICollection<AbonoCreditoFerreteria> AbonosCreditoFerreteria { get; set; } = new List<AbonoCreditoFerreteria>();
        public int CreditoFerreteriaId { get; set; }

        [Required]
        public int EmpleadoId { get; set; }

        [Required]
        [Display(Name = "Monto Total (₡)")]
        public decimal MontoTotal { get; set; }

        [Required]
        [Display(Name = "Saldo Actual (₡)")]
        public decimal Saldo { get; set; }

        [Required]
        [Display(Name = "Cuota Quincenal (₡)")]
        public decimal CuotaQuincenal { get; set; }

        [Required]
        [Display(Name = "Fecha del Crédito")]
        public DateTime FechaCredito { get; set; } = DateTime.Today;

        [Required]
        [StringLength(200)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        // ── Navegación ─────────────────────────────────
        public Empleado Empleado { get; set; } = null!;
    }
}