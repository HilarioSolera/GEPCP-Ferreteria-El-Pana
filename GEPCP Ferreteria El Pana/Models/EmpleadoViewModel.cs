using Microsoft.EntityFrameworkCore.Migrations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class EmpleadoViewModel
    {
        public int EmpleadoId { get; set; }

        [Required(ErrorMessage = "La cédula es obligatoria")]
        [StringLength(20)]
        [Display(Name = "Cédula")]
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El primer apellido es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Primer Apellido")]
        public string PrimerApellido { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Segundo Apellido")]
        public string? SegundoApellido { get; set; }

        [Required(ErrorMessage = "El puesto es obligatorio")]
        [Display(Name = "Puesto")]
        public string Puesto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El departamento es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Departamento")]
        public string Departamento { get; set; } = "Ventas";

        [Required(ErrorMessage = "El tipo de jornada es obligatorio")]
        [Display(Name = "Tipo de Jornada")]
        public TipoJornada TipoJornada { get; set; } = TipoJornada.Completa;

        [Required(ErrorMessage = "La fecha de ingreso es obligatoria")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Ingreso")]
        public DateTime FechaIngreso { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "El salario base es obligatorio")]
        [Range(1, 99999999, ErrorMessage = "El salario debe ser mayor a cero")]
        [Display(Name = "Salario Base (₡)")]
        public decimal SalarioBase { get; set; }

        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Activo";

        [StringLength(20)]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        [Display(Name = "Teléfono / Celular")]
        public string? Telefono { get; set; }

        [StringLength(150)]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [Display(Name = "Correo Electrónico")]
        public string? CorreoElectronico { get; set; }

        [StringLength(30)]
        [Display(Name = "Número de Cuenta BN")]
        public string? NumeroCuenta { get; set; }

        [Required]
        [Display(Name = "Forma de Pago")]
        public FormaPago FormaPago { get; set; } = FormaPago.Transferencia;

        [Display(Name = "Fecha de Nacimiento")]
        [DataType(DataType.Date)]
        public DateTime? FechaNacimiento { get; set; }

        // ── Contrato ────────────────────────────────────────────────────────
        [Required(ErrorMessage = "El tipo de contrato es obligatorio")]
        [Display(Name = "Tipo de Contrato")]
        public TipoContrato TipoContrato { get; set; } = TipoContrato.Indefinido;

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Vencimiento de Contrato")]
        public DateTime? FechaVencimientoContrato { get; set; }

        // ── Dirección ───────────────────────────────────────────────────────
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

        // ── Contacto de emergencia ──────────────────────────────────────────
        [StringLength(100)]
        [Display(Name = "Nombre Contacto de Emergencia")]
        public string? ContactoEmergenciaNombre { get; set; }

        [StringLength(20)]
        [Display(Name = "Teléfono Contacto de Emergencia")]
        public string? ContactoEmergenciaTelefono { get; set; }

        [Required]
        [Display(Name = "Tipo de Pago")]
        public TipoPago TipoPago { get; set; } = TipoPago.Quincenal;

        // ── ISR: Créditos fiscales ───────────────────────────────────────────
        [Display(Name = "Número de Hijos")]
        [Range(0, 20, ErrorMessage = "Debe ser entre 0 y 20")]
        public int NumHijos { get; set; } = 0;

        [Display(Name = "¿Tiene Cónyuge?")]
        public bool TieneConyuge { get; set; } = false;

        // ── Calculados (no mapeados a BD) ───────────────────────────────────
        public int HorasMensuales => TipoJornada == TipoJornada.Completa ? 240 : 120;
        public int HorasQuincenales => TipoJornada == TipoJornada.Completa ? 120 : 60;
        public decimal ValorHora => HorasMensuales > 0
                                       ? Math.Round(SalarioBase / HorasMensuales, 2)
                                       : 0;
        public decimal ValorHoraExtra => Math.Round(ValorHora * 1.5m, 2);

        // ── Helper: días para vencimiento de contrato ───────────────────────
        public int? DiasParaVencimiento => FechaVencimientoContrato.HasValue
            ? (int)(FechaVencimientoContrato.Value.Date - DateTime.Today).TotalDays
            : null;

        public bool ContratoProximoAVencer =>
            DiasParaVencimiento.HasValue && DiasParaVencimiento.Value >= 0 &&
            DiasParaVencimiento.Value <= 30;

        public bool ContratoVencido =>
            DiasParaVencimiento.HasValue && DiasParaVencimiento.Value < 0;

        // ── Vacaciones ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Días de vacaciones proporcionales disponibles según ley CR:
        /// 12 días hábiles por cada 50 semanas trabajadas.
        /// </summary>
        [NotMapped]
        public decimal DiasVacacionesDisponibles
        {
            get
            {
                if (FechaIngreso == default) return 0;
                var semanasTrabajadas = (decimal)(DateTime.Today - FechaIngreso).TotalDays / 7;
                var periodos50 = Math.Floor(semanasTrabajadas / 50);
                return periodos50 * 12;
            }
        }

        [NotMapped]
        public decimal SalarioDiario => SalarioBase / 30;

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
    }


}