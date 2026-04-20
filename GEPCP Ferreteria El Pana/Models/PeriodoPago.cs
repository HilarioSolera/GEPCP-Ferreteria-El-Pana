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
        Primera = 1,
        Segunda = 2
    }

    public enum TipoPeriodo
    {
        Quincenal = 1,
        Semanal = 2,
        Mensual = 3
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
        [Display(Name = "Tipo de Período")]
        public TipoPeriodo TipoPeriodo { get; set; } = TipoPeriodo.Quincenal;

        [Required]
        [Display(Name = "Estado")]
        public EstadoPeriodo Estado { get; set; } = EstadoPeriodo.Abierto;

        [Display(Name = "% CCSS Empleado")]
        public decimal PorcentajeCCSS { get; set; } = 10.83m;

        // CCSS desglosada
        [Display(Name = "% SEM (Seguro Enfermedad y Maternidad)")]
        public decimal PorcentajeSEM { get; set; } = 5.50m;

        [Display(Name = "% IVM (Invalidez, Vejez y Muerte)")]
        public decimal PorcentajeIVM { get; set; } = 4.33m;

        [Display(Name = "% Banco Popular")]
        public decimal PorcentajeBP { get; set; } = 1.00m;

        // ISR Tramos editables
        [Display(Name = "ISR Tramo 1 — Exento hasta (₡)")]
        public decimal ISR_Tramo1_Hasta { get; set; } = 918000m;

        [Display(Name = "ISR Tramo 2 — Desde (₡)")]
        public decimal ISR_Tramo2_Desde { get; set; } = 918000m;
        [Display(Name = "ISR Tramo 2 — Hasta (₡)")]
        public decimal ISR_Tramo2_Hasta { get; set; } = 1347000m;
        [Display(Name = "ISR Tramo 2 — %")]
        public decimal ISR_Tramo2_Porcentaje { get; set; } = 10m;

        [Display(Name = "ISR Tramo 3 — Desde (₡)")]
        public decimal ISR_Tramo3_Desde { get; set; } = 1347000m;
        [Display(Name = "ISR Tramo 3 — Hasta (₡)")]
        public decimal ISR_Tramo3_Hasta { get; set; } = 2364000m;
        [Display(Name = "ISR Tramo 3 — %")]
        public decimal ISR_Tramo3_Porcentaje { get; set; } = 15m;

        [Display(Name = "ISR Tramo 4 — Desde (₡)")]
        public decimal ISR_Tramo4_Desde { get; set; } = 2364000m;
        [Display(Name = "ISR Tramo 4 — Hasta (₡)")]
        public decimal ISR_Tramo4_Hasta { get; set; } = 4727000m;
        [Display(Name = "ISR Tramo 4 — %")]
        public decimal ISR_Tramo4_Porcentaje { get; set; } = 20m;

        [Display(Name = "ISR Tramo 5 — Desde (₡)")]
        public decimal ISR_Tramo5_Desde { get; set; } = 4727000m;
        [Display(Name = "ISR Tramo 5 — %")]
        public decimal ISR_Tramo5_Porcentaje { get; set; } = 25m;

        // Créditos fiscales ISR
        [Display(Name = "Crédito por hijo (₡/mes)")]
        public decimal ISR_CreditoHijo { get; set; } = 0m;

        [Display(Name = "Crédito por cónyuge (₡/mes)")]
        public decimal ISR_CreditoConyuge { get; set; } = 0m;

        // Navegación
        public ICollection<PlanillaEmpleado> PlanillasEmpleado { get; set; }
            = new List<PlanillaEmpleado>();
        public ICollection<HorasExtras> HorasExtras { get; set; }
            = new List<HorasExtras>();
        public ICollection<PagoFeriado> PagosFeriado { get; set; }
            = new List<PagoFeriado>();

        // Propiedades calculadas
        public string Descripcion => TipoPeriodo switch
        {
            TipoPeriodo.Semanal =>
                $"Semana — {FechaInicio:dd/MM/yyyy} al {FechaFin:dd/MM/yyyy}",
            TipoPeriodo.Mensual =>
                $"Mes {Mes}/{Anio} — {FechaInicio:dd/MM/yyyy} al {FechaFin:dd/MM/yyyy}",
            _ =>
                $"Quincena {(int)Quincena} — {FechaInicio:dd/MM/yyyy} al {FechaFin:dd/MM/yyyy}"
        };

        public string TipoPagoCompatible => TipoPeriodo switch
        {
            TipoPeriodo.Semanal => "Semanal",
            TipoPeriodo.Mensual => "Mensual",
            _ => "Quincenal"
        };
    }
}