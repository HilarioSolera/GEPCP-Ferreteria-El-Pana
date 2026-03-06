using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class EmpleadoViewModel
    {
        public int EmpleadoId { get; set; }

        [Required(ErrorMessage = "La cédula es requerida")]
        [StringLength(20, ErrorMessage = "Máximo 20 caracteres")]
        [Display(Name = "Cédula")]
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El primer apellido es requerido")]
        [StringLength(50)]
        [Display(Name = "Primer Apellido")]
        public string PrimerApellido { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Segundo Apellido")]
        public string? SegundoApellido { get; set; }

        [Required(ErrorMessage = "El puesto es requerido")]
        [StringLength(100)]
        [Display(Name = "Puesto")]
        public string Puesto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El salario base es requerido")]
        [Range(0, double.MaxValue, ErrorMessage = "El salario debe ser positivo")]
        [Display(Name = "Salario Base")]
        public decimal SalarioBase { get; set; }

        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Activo";
    }
}