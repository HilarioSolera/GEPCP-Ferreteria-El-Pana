namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class Usuario
    {
        public int UsuarioId { get; set; }

        public string NombreUsuario { get; set; } = string.Empty; // Renombrado para evitar conflicto

        public string NombreCompleto { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;
    }
}