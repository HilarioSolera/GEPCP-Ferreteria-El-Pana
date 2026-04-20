using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public enum EntidadIncapacidad
    {
        CCSS = 1,
        INS = 2
    }

    public enum ResponsablePago
    {
        Patrono = 1,
        CCSS = 2,
        INS = 3
    }

    public enum TipoIncapacidad
    {
        EnfermedadComun = 1,
        AccidenteTransito = 2,
        AccidenteLaboral = 3,
        LicenciaMaternidad = 4,  // Art. 95 CT: 4 meses (1 antes, 3 después del parto)
        LicenciaPaternidad = 5,  // Ley 9877: 2 días hábiles
        Recuperacion = 6,
        Otro = 7
    }

    public class Incapacidad
    {
        public int IncapacidadId { get; set; }

        [Required]
        public int EmpleadoId { get; set; }

        [Required]
        [Display(Name = "Entidad")]
        public EntidadIncapacidad Entidad { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Tipo de Incapacidad")]
        public string TipoIncapacidad { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Fecha Inicio")]
        public DateTime FechaInicio { get; set; }

        [Required]
        [Display(Name = "Fecha Fin")]
        public DateTime FechaFin { get; set; }

        [Display(Name = "Total Días")]
        public int TotalDias { get; set; }

        [StringLength(50)]
        [Display(Name = "Tiquete CCSS")]
        public string? TiqueteCCSS { get; set; }

        [Required]
        [Display(Name = "% de Pago")]
        public decimal PorcentajePago { get; set; } = 50;

        [Required]
        [Display(Name = "Responsable del Pago")]
        public ResponsablePago ResponsablePago { get; set; }

        [Display(Name = "Monto por Día (₡)")]
        public decimal MontoPorDia { get; set; }

        [Display(Name = "Monto Total (₡)")]
        public decimal MontoTotal { get; set; }

        [StringLength(200)]
        [Display(Name = "Observaciones")]

        public string? Observaciones { get; set; }
        [Display(Name = "Días a cargo del Patrono")]
        public int DiasPagadosPatrono { get; set; }

        // Navegación
        public Empleado Empleado { get; set; } = null!;

    }
}