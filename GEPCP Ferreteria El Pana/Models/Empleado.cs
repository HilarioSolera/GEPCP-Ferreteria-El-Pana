using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class Empleado
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EmpleadoId { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "Cédula")]
        public string Cedula { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Primer Apellido")]
        public string PrimerApellido { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Segundo Apellido")]
        public string? SegundoApellido { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Puesto")]
        public string Puesto { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Salario Base")]
        public decimal SalarioBase { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        // Colecciones de navegación (relaciones uno-a-muchos)
        public virtual ICollection<Comision> Comisiones { get; set; } = new List<Comision>();
        public virtual ICollection<Planilla> Planillas { get; set; } = new List<Planilla>();
        public virtual ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
    }
}