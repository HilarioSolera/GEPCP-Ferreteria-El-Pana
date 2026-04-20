using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class Puesto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PuestoId { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Departamento")]
        public string Departamento { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "Nombre del Puesto")]
        public string Nombre { get; set; } = string.Empty;

        [Required, StringLength(20)]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = "TOCG";

        [Required, Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Salario Base (₡)")]
        public decimal SalarioBase { get; set; }

        public bool Activo { get; set; } = true;
    }
}