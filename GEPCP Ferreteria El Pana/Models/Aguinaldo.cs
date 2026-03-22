using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class Aguinaldo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AguinaldoId { get; set; }

        [Required]
        public int EmpleadoId { get; set; }

        [ForeignKey("EmpleadoId")]
        public Empleado Empleado { get; set; } = null!;

        [Required]
        public int Anio { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        [Required]
        public DateTime FechaPago { get; set; } = DateTime.Today;

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal MontoTotal { get; set; }

        [StringLength(200)]
        public string? Observaciones { get; set; }

        // Auditoría
        public DateTime CreadoEn { get; set; } = DateTime.Now;
    }
}