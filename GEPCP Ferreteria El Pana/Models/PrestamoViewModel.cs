namespace GEPCP_Ferreteria_El_Pana.Models
{
    using System.ComponentModel.DataAnnotations;

    public class PrestamoViewModel
    {
        public int PrestamoId { get; set; }

        [Required]
        [Display(Name = "Empleado")]
        public string NombreEmpleado { get; set; } = string.Empty;

        [Required]
        [Range(1, 9999999)]
        [Display(Name = "Monto Principal")]
        public decimal MontoPrincipal { get; set; }

        [Display(Name = "Saldo Actual")]
        public decimal SaldoActual { get; set; }

        [Required]
        [Range(1, 9999999)]
        [Display(Name = "Cuota Mensual")]
        public decimal CuotaMensual { get; set; }

        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Activo";
    }
}