using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public enum TipoVacacion
    {
        ConPago = 1,
        SinPago = 2
    }

    public enum EstadoVacacion
    {
        Pendiente = 1,
        Aprobada = 2,
        Rechazada = 3
    }

    public class Vacacion
    {
        public int VacacionId { get; set; }

        [Required(ErrorMessage = "El empleado es obligatorio.")]
        public int EmpleadoId { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        [Display(Name = "Fecha de Inicio")]
        [DataType(DataType.Date)]
        public DateTime FechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
        [Display(Name = "Fecha de Fin")]
        [DataType(DataType.Date)]
        public DateTime FechaFin { get; set; }

        [Required(ErrorMessage = "El tipo es obligatorio.")]
        [Display(Name = "Tipo de Vacación")]
        public TipoVacacion Tipo { get; set; } = TipoVacacion.ConPago;

        [Required(ErrorMessage = "El estado es obligatorio.")]
        [Display(Name = "Estado")]
        public EstadoVacacion Estado { get; set; } = EstadoVacacion.Pendiente;

        [Display(Name = "Días Hábiles Tomados")]
        [Range(0.5, 365, ErrorMessage = "Los días deben ser entre 0.5 y 365.")]
        public decimal DiasHabiles { get; set; }

        [Display(Name = "Días Disponibles al Momento")]
        public decimal DiasDisponiblesAlRegistrar { get; set; }

        [Display(Name = "Monto Deducido (₡)")]
        public decimal MontoDeducido { get; set; }

        [Display(Name = "Salario Diario al Registrar (₡)")]
        public decimal SalarioDiario { get; set; }

        [StringLength(300)]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        [Display(Name = "Fecha de Registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [StringLength(100)]
        [Display(Name = "Registrado por")]
        public string RegistradoPor { get; set; } = string.Empty;

        // ── Navegación ─────────────────────────────────
        public Empleado Empleado { get; set; } = null!;
    }
}