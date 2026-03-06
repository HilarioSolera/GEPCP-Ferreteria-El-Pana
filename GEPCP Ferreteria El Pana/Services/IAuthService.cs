using GEPCP_Ferreteria_El_Pana.Models;
using Microsoft.AspNetCore.Identity;

namespace GEPCP_Ferreteria_El_Pana.Services
{
    public interface IAuthService
    {
        bool ValidateUser(string usuario, string password, out string rol);
    }

    public class AuthService : IAuthService
    {
        private readonly List<User> _users;
        private readonly PasswordHasher<User> _hasher;

        public AuthService()
        {
            _hasher = new PasswordHasher<User>();

            // Usuarios semilla (solo demo). En producción usa base de datos / secretos.
            _users = new List<User>
            {
                new User
                {
                    Usuario = "admin.rrhh",
                    Rol = "RRHH",
                    NombreCompleto = "Administrador RRHH",
                    PasswordHash = string.Empty // se rellenará abajo
                },
                new User
                {
                    Usuario = "jefatura",
                    Rol = "Jefatura",
                    NombreCompleto = "Usuario Jefatura",
                    PasswordHash = string.Empty
                }
            };

            // Asignar hashes seguros a las contraseñas iniciales
            var rrhh = _users.First(u => u.Usuario == "admin.rrhh");
            rrhh.PasswordHash = _hasher.HashPassword(rrhh, "Admin2025!");

            var jefe = _users.First(u => u.Usuario == "jefatura");
            jefe.PasswordHash = _hasher.HashPassword(jefe, "Jefe2025!");
        }

        public bool ValidateUser(string usuario, string password, out string rol)
        {
            rol = string.Empty;
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
                return false;

            var user = _users.FirstOrDefault(u => u.Usuario.Equals(usuario, System.StringComparison.OrdinalIgnoreCase));
            if (user == null)
                return false;

            var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (verify == PasswordVerificationResult.Success)
            {
                rol = user.Rol;
                return true;
            }

            return false;
        }
    }
}