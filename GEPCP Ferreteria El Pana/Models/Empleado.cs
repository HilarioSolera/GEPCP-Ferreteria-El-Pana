using Microsoft.EntityFrameworkCore.Migrations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace GEPCP_Ferreteria_El_Pana.Models
{

    public enum TipoJornada
    {
        Completa = 1,   // 240 horas mensuales
        MediaJornada = 2 // 120 horas mensuales
    }
    public enum FormaPago
    {
        Transferencia = 1,
        Efectivo = 2
    }
    public enum TipoContrato
    {
        Indefinido = 1,
        PlazoFijo = 2,
        PorObra = 3
    }
    public enum TipoPago
    {
        Quincenal = 1,
        Semanal = 2,
        Mensual = 3
    }


    public class Empleado
    {
        public ICollection<HistorialSalario> HistorialSalarios { get; set; } = new List<HistorialSalario>();
        public int EmpleadoId { get; set; }

        [Required]
        [StringLength(20)]
        public string Cedula { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string PrimerApellido { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SegundoApellido { get; set; }

        [Required]
        [StringLength(100)]
        public string Puesto { get; set; } = string.Empty;

        // ── NUEVO: Departamento ────────────────────────
        [Required]
        [StringLength(100)]
        [Display(Name = "Departamento")]
        public string Departamento { get; set; } = "Ventas";

        // ── NUEVO: Tipo de jornada ─────────────────────
        [Required]
        [Display(Name = "Tipo de Jornada")]
        public TipoJornada TipoJornada { get; set; } = TipoJornada.Completa;

        // ── NUEVO: Fecha de ingreso ────────────────────
        [Required]
        [Display(Name = "Fecha de Ingreso")]
        [DataType(DataType.Date)]
        public DateTime FechaIngreso { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Salario Base Mensual (₡)")]
        public decimal SalarioBase { get; set; }

        // ── Tipo de contrato ────────────────────────────────────────
        [Display(Name = "Tipo de Contrato")]
        public TipoContrato TipoContrato { get; set; } = TipoContrato.Indefinido;

        [Display(Name = "Fecha de Vencimiento de Contrato")]
        public DateTime? FechaVencimientoContrato { get; set; }

        // ── Dirección ───────────────────────────────────────────────
        [StringLength(100)]
        [Display(Name = "Provincia")]
        public string? DireccionProvincia { get; set; }

        [StringLength(100)]
        [Display(Name = "Cantón")]
        public string? DireccionCanton { get; set; }

        [StringLength(100)]
        [Display(Name = "Distrito")]
        public string? DireccionDistrito { get; set; }

        [StringLength(300)]
        [Display(Name = "Dirección Exacta")]
        public string? DireccionExacta { get; set; }

        // ── Contacto de emergencia ──────────────────────────────────
        [StringLength(100)]
        [Display(Name = "Contacto de Emergencia")]
        public string? ContactoEmergenciaNombre { get; set; }

        [StringLength(20)]
        [Display(Name = "Teléfono de Emergencia")]
        public string? ContactoEmergenciaTelefono { get; set; }

        // ── CALCULADO: Valor hora = SalarioBase / HorasMensuales ──
        [NotMapped]
        public int HorasMensuales => TipoJornada == TipoJornada.Completa ? 240 : 120;

        [NotMapped]
        public int HorasQuincenales => TipoJornada == TipoJornada.Completa ? 120 : 60;

        [NotMapped]
        public decimal ValorHora => HorasMensuales > 0
            ? Math.Round(SalarioBase / HorasMensuales, 2)
            : 0;


        [NotMapped]
        public int HorasPorPeriodo => TipoPago switch
        {
            TipoPago.Semanal => HorasMensuales / 4,
            TipoPago.Mensual => HorasMensuales,
            _ => HorasQuincenales
        };

        [NotMapped]
        public decimal FactorCuotaPrestamo => TipoPago switch
        {
            TipoPago.Semanal => 4m,
            TipoPago.Mensual => 1m,
            _ => 2m
        };

        [NotMapped]
        public string DescripcionTipoPago => TipoPago switch
        {
            TipoPago.Semanal => "Semanal",
            TipoPago.Mensual => "Mensual",
            _ => "Quincenal"
        };

        [NotMapped]
        public decimal ValorHoraExtra => Math.Round(ValorHora * 1.5m, 2);

        // ── ISR: Créditos fiscales ───────────────────────────
        [Display(Name = "Número de Hijos")]
        public int NumHijos { get; set; } = 0;

        [Display(Name = "¿Tiene Cónyuge?")]
        public bool TieneConyuge { get; set; } = false;

        public bool Activo { get; set; } = true;

        [StringLength(20)]
        [Display(Name = "Teléfono / Celular")]
        public string? Telefono { get; set; }

        [StringLength(150)]
        [EmailAddress]
        [Display(Name = "Correo Electrónico")]
        public string? CorreoElectronico { get; set; }

        // En Models/Empleado.cs agregar:

        [StringLength(30)]
        [Display(Name = "Número de Cuenta SINPE/Banco")]
        public string? NumeroCuenta { get; set; }

        [Required]
        [Display(Name = "Forma de Pago")]

        public FormaPago FormaPago { get; set; } = FormaPago.Transferencia;
        [Display(Name = "Fecha de Nacimiento")]
        [DataType(DataType.Date)]
        public DateTime? FechaNacimiento { get; set; }
        [Required]
        [Display(Name = "Tipo de Pago")]
        public TipoPago TipoPago { get; set; } = TipoPago.Quincenal;


        // ── Navegación ─────────────────────────────────
        public ICollection<Comision> Comisiones { get; set; } = new List<Comision>();
        public ICollection<Planilla> Planillas { get; set; } = new List<Planilla>();
        public ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
        public ICollection<PlanillaEmpleado> PlanillasEmpleado { get; set; } = new List<PlanillaEmpleado>();
        public ICollection<CreditoFerreteria> CreditosFerreteria { get; set; } = new List<CreditoFerreteria>();
        public ICollection<Incapacidad> Incapacidades { get; set; } = new List<Incapacidad>();
        public ICollection<HorasExtras> HorasExtras { get; set; } = new List<HorasExtras>();
        public ICollection<PagoFeriado> PagosFeriado { get; set; } = new List<PagoFeriado>();
        public ICollection<Vacacion> Vacaciones { get; set; } = new List<Vacacion>();
    }
}