namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class PeriodoPlanillaViewModel
    {
        public int PeriodoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "Abierto";
        public decimal TotalBruto { get; set; }
        public decimal TotalDeducciones { get; set; }
        public decimal TotalNeto { get; set; }
    }
}