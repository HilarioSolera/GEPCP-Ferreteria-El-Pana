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

        // Base de cálculo almacenada para auditoría y transparencia
        [Column(TypeName = "decimal(18,2)")]
        public decimal SumaDevengados { get; set; }

        // Cantidad de períodos cerrados que se tomaron en cuenta
        public int PeriodosConsiderados { get; set; }

        // ── Calculadora manual por mes ──────────────────────────────────────
        // Salarios brutos mensuales Dec(año-1)..Nov(año) separados por ';'
        [StringLength(500)]
        public string? SalariosMensuales { get; set; }

        // Salario en especie
        [Column(TypeName = "decimal(18,2)")]
        public decimal PorcentajeEspecie { get; set; }   // 0 = no aplica

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoEspecieMensual { get; set; } // monto mensual en especie

        [StringLength(200)]
        public string? Observaciones { get; set; }

        // Auditoría
        public DateTime CreadoEn { get; set; } = DateTime.Now;
    }
}