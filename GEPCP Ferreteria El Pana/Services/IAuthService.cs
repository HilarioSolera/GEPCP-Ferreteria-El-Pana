using GEPCP_Ferreteria_El_Pana.Data;

namespace GEPCP_Ferreteria_El_Pana.Services
{
    // ── INTERFAZ ──────────────────────────────────────────────────────────────
    public interface IAuthService
    {
        bool ValidateUser(string usuario, string password, out string rol);
    }

    // ── IMPLEMENTACIÓN ────────────────────────────────────────────────────────
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public bool ValidateUser(string usuario, string password, out string rol)
        {
            rol = string.Empty;

            try
            {
                // Validaciones básicas de entrada
                if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Intento de login con credenciales vacías.");
                    return false;
                }

                if (usuario.Length > 50 || password.Length > 100)
                {
                    _logger.LogWarning("Intento de login con credenciales de longitud excesiva.");
                    return false;
                }

                // Buscar usuario en BD (case-insensitive)
                var user = _context.Usuarios
                    .FirstOrDefault(u =>
                        u.NombreUsuario.ToLower() == usuario.Trim().ToLower());

                if (user == null)
                {
                    _logger.LogWarning("Login fallido — usuario no encontrado: {Usuario}", usuario);
                    return false;
                }

                // Verificar hash BCrypt
                var passwordValida = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

                if (!passwordValida)
                {
                    _logger.LogWarning("Login fallido — contraseña incorrecta para: {Usuario}", usuario);
                    return false;
                }

                // Login exitoso
                rol = user.Rol;
                _logger.LogInformation("Login exitoso: {Usuario} Rol {Rol}", user.NombreUsuario, rol);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al validar credenciales para: {Usuario}", usuario);
                return false;
            }
        }
    }
}