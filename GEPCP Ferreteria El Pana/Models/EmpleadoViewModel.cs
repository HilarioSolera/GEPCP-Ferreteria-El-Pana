using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class EmpleadoViewModel
    {
        [Key]
        public int EmpleadoId { get; set; }

        [Required(ErrorMessage = "La cédula es obligatoria")]
        [StringLength(20, ErrorMessage = "Máximo 20 caracteres")]
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
        [StringLength(100)]
        [Display(Name = "Puesto")]
        public string Puesto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El salario base es obligatorio")]
        [Range(0, double.MaxValue, ErrorMessage = "El salario debe ser positivo")]
        [Display(Name = "Salario Base")]
        public decimal SalarioBase { get; set; }  // ← aquí está la propiedad que faltaba

        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Activo";  // ← y aquí también
    }
}