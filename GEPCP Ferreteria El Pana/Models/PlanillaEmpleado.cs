using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class PlanillaEmpleado
    {
        public int PlanillaEmpleadoId { get; set; }

        [Required]
        public int PeriodoPagoId { get; set; }

        [Required]
        public int EmpleadoId { get; set; }

        // ── DEVENGADOS ──────────────────────────────────
        [Display(Name = "Horas Ordinarias")]
        public decimal HorasOrdinarias { get; set; }

        [Display(Name = "Horas Extras")]
        public decimal HorasExtras { get; set; }

        [Display(Name = "Horas No Laboradas")]
        public decimal HorasNoLaboradas { get; set; }

        [Display(Name = "Valor Hora")]
        public decimal ValorHora { get; set; }

        [Display(Name = "Valor Hora Extra")]
        public decimal ValorHoraExtra { get; set; }

        [Display(Name = "Salario Ordinario (₡)")]
        public decimal SalarioOrdinario { get; set; }

        [Display(Name = "Aumento Aplicado (₡)")]
        public decimal AumentoAplicado { get; set; }

        [Display(Name = "Pago Horas Extras (₡)")]
        public decimal MontoHorasExtras { get; set; }

        [Display(Name = "Pago Feriados (₡)")]
        public decimal MontoFeriados { get; set; }

        [Display(Name = "Total Devengado (₡)")]
        public decimal TotalDevengado { get; set; }

        // ── DEDUCCIONES ─────────────────────────────────
        [Display(Name = "% CCSS")]
        public decimal PorcentajeCCSS { get; set; } = 10.83m;

        [Display(Name = "Deducción CCSS (₡)")]
        public decimal DeduccionCCSS { get; set; }

        [Display(Name = "Deducción Préstamos (₡)")]
        public decimal DeduccionPrestamos { get; set; }

        [Display(Name = "Deducción Crédito Ferretería (₡)")]
        public decimal DeduccionCreditoFerreteria { get; set; }

        [Display(Name = "Deducción Incapacidad (₡)")]
        public decimal DeduccionIncapacidad { get; set; }

        [Display(Name = "Horas No Laboradas (₡)")]
        public decimal DeduccionHorasNoLaboradas { get; set; }

        [Display(Name = "Otras Deducciones (₡)")]
        public decimal OtrasDeducciones { get; set; }

        [Display(Name = "Total Deducciones (₡)")]
        public decimal TotalDeducciones { get; set; }

        [Display(Name = "Neto a Pagar (₡)")]
        public decimal NetoAPagar { get; set; }
        [Display(Name = "Deducción Vacaciones Sin Pago (₡)")]
        public decimal DeduccionVacaciones { get; set; }

        // ── Navegación ─────────────────────────────────
        public PeriodoPago PeriodoPago { get; set; } = null!;
        public Empleado Empleado { get; set; } = null!;

        public const decimal PorcentajeCCSSDefault = 10.83m;
    }
}