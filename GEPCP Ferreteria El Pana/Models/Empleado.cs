// Models/Empleado.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class Empleado
    {
        [Key]
        public int EmpleadoId { get; set; }

        [Required, StringLength(20)]
        public string Cedula { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string PrimerApellido { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SegundoApellido { get; set; }

        [Required, StringLength(100)]
        public string Puesto { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalarioBase { get; set; }

        public bool Activo { get; set; } = true;  // En BD es bool, en ViewModel lo mostramos como string
    }
}