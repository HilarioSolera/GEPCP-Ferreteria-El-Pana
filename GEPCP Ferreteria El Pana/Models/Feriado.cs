using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public enum TipoFeriado
    {
        Obligatorio = 1,
        NoObligatorio = 2
    }

    public class Feriado
    {
        public int FeriadoId { get; set; }

        [Required]
        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Tipo")]
        public TipoFeriado Tipo { get; set; } = TipoFeriado.Obligatorio;

        // Navegación
        public ICollection<PagoFeriado> PagosFeriado { get; set; } = new List<PagoFeriado>();
    }
}