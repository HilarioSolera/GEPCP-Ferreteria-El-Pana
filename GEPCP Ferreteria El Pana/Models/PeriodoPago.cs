using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public enum EstadoPeriodo
    {
        Abierto = 1,
        Cerrado = 2
    }

    public enum NumeroQuincena
    {
        Primera = 1,  // día 1 al 15
        Segunda = 2   // día 16 al último
    }

    public class PeriodoPago
    {
        public int PeriodoPagoId { get; set; }

        [Required]
        [Display(Name = "Fecha Inicio")]
        public DateTime FechaInicio { get; set; }

        [Required]
        [Display(Name = "Fecha Fin")]
        public DateTime FechaFin { get; set; }

        [Required]
        [Display(Name = "Quincena")]
        public NumeroQuincena Quincena { get; set; }

        [Required]
        public int Mes { get; set; }

        [Required]
        public int Anio { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public EstadoPeriodo Estado { get; set; } = EstadoPeriodo.Abierto;

        // ── Navegación ─────────────────────────────────
        public ICollection<PlanillaEmpleado> PlanillasEmpleado { get; set; } = new List<PlanillaEmpleado>();
        public ICollection<HorasExtras> HorasExtras { get; set; } = new List<HorasExtras>();
        public ICollection<PagoFeriado> PagosFeriado { get; set; } = new List<PagoFeriado>();

        // ── Propiedades calculadas ──────────────────────
        public string Descripcion => $"Quincena {(int)Quincena} — {FechaInicio:dd/MM/yyyy} al {FechaFin:dd/MM/yyyy}";
    }
}