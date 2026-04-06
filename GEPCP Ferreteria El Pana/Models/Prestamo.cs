using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class Prestamo
    {
        public ICollection<AbonoPrestamo> AbonosPrestamo { get; set; } = new List<AbonoPrestamo>();
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PrestamoId { get; set; }

        [Required]
        public int EmpleadoId { get; set; }

        [ForeignKey("EmpleadoId")]
        public Empleado Empleado { get; set; } = null!;

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Required]
        public DateTime FechaPrestamo { get; set; } = DateTime.Now;

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Interes { get; set; }

        [Required]
        public int Cuotas { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CuotaMensual { get; set; }

        public bool Activo { get; set; } = true;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Monto Original")]
        public decimal MontoOriginal { get; set; }
    }
}