namespace GEPCP_Ferreteria_El_Pana.Models
{
    using System.ComponentModel.DataAnnotations;

    public class ComisionViewModel
    {
        public int ComisionId { get; set; }

        [Display(Name = "Empleado")]
        public string NombreEmpleado { get; set; } = string.Empty;

        [Display(Name = "Entregas")]
        public int CantidadEntregas { get; set; }

        [Display(Name = "Monto Calculado")]
        public decimal MontoCalculado { get; set; }

        [Display(Name = "Monto Final")]
        public decimal MontoFinal { get; set; }
    }
}