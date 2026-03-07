namespace GEPCP_Ferreteria_El_Pana.Models
{
    public class Usuario
    {
        public int UsuarioId { get; set; }

        public string NombreUsuario { get; set; } = string.Empty;

        public string CorreoElectronico { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;  // Encriptado con BCrypt

        public int RolId { get; set; }  // 1 = RRHH, 2 = Jefatura
    }
}