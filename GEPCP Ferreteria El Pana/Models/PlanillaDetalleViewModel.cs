namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class PlanillaDetalleViewModel
    {
        public int EmpleadoId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public decimal SalarioBase { get; set; }
        public decimal Comisiones { get; set; }
        public decimal CuotasPrestamos { get; set; }
        public decimal SalarioNeto { get; set; }
    }
}