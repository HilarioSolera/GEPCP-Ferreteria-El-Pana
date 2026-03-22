using System.ComponentModel.DataAnnotations;

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

        // Calculados (no mapeados a BD)
        public int HorasMensuales    => TipoJornada == TipoJornada.Completa ? 240 : 120;
        public int HorasQuincenales  => TipoJornada == TipoJornada.Completa ? 120 : 60;
        public decimal ValorHora     => HorasMensuales > 0
                                        ? Math.Round(SalarioBase / HorasMensuales, 2)
                                        : 0;
        public decimal ValorHoraExtra => Math.Round(ValorHora * 1.5m, 2);
    }
}