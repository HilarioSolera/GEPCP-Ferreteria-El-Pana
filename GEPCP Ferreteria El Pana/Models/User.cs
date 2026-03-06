namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class User
    {
        public string Usuario { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
    }
}