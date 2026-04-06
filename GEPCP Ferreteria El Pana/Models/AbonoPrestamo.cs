using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class AbonoPrestamo
    {
        [Key]
        public int AbonoPrestamoId { get; set; }

        [Required]
        public int PrestamoId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Required]
        public DateTime FechaAbono { get; set; } = DateTime.Now;

        [StringLength(200)]
        public string? Observaciones { get; set; }

        public Prestamo Prestamo { get; set; } = null!;
    }
}