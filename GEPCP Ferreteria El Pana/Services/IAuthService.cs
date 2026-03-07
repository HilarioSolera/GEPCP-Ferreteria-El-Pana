using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Models;

namespace GEPCP_Ferreteria_El_Pana.Services
{
    public interface IAuthService
    {
        bool ValidateUser(string usuario, string password, out string rol);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool ValidateUser(string usuario, string password, out string rol)
        {
            rol = string.Empty;

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
                return false;

            var user = _context.Usuarios
                .FirstOrDefault(u => u.NombreUsuario.ToLower() == usuario.ToLower());

            if (user == null)
                return false;

            bool passwordValida = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (passwordValida)
            {
                rol = user.Rol;
                return true;
            }

            return false;
        }
    }
}