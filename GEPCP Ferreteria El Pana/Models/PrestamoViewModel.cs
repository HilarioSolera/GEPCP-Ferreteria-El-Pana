using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class PrestamoViewModel
    {
        public int PrestamoId { get; set; }

        [Required(ErrorMessage = "Seleccione un empleado")]
        [Display(Name = "Empleado")]
        public int EmpleadoId { get; set; }

        public string NombreEmpleado { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto es obligatorio")]
        [Range(1, 9999999, ErrorMessage = "El monto debe ser mayor a cero")]
        [Display(Name = "Monto del Préstamo")]
        public decimal MontoPrincipal { get; set; }

        [Display(Name = "Saldo Actual")]
        public decimal SaldoActual { get; set; }

        [Required(ErrorMessage = "La cuota mensual es obligatoria")]
        [Range(1, 9999999, ErrorMessage = "La cuota debe ser mayor a cero")]
        [Display(Name = "Cuota Mensual")]
        public decimal CuotaMensual { get; set; }

        [Required(ErrorMessage = "El número de cuotas es obligatorio")]
        [Range(1, 120, ErrorMessage = "Las cuotas deben estar entre 1 y 120")]
        [Display(Name = "Total de Cuotas")]
        public int CuotasTotal { get; set; }

        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Activo";
        [Required(ErrorMessage = "La fecha del préstamo es obligatoria")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha del Préstamo")]
        public DateTime FechaPrestamo { get; set; } = DateTime.Today;
    }
}