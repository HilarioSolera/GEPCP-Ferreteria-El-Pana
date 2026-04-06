using System.ComponentModel.DataAnnotations;

namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class RegistroAuditoria
    {
        public int RegistroAuditoriaId { get; set; }

        [Required]
        public string Usuario { get; set; } = string.Empty;

        [Required]
        public string Accion { get; set; } = string.Empty;

        [Required]
        public string Modulo { get; set; } = string.Empty;

        public string? Detalle { get; set; }

        public string? IpAddress { get; set; }

        [Required]
        public DateTime FechaHora { get; set; } = DateTime.Now;
    }
}