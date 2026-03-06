using GEPCP_Ferreteria_El_Pana.Models;

namespace GEPCP_Ferreteria_El_Pana.Services
{
    public interface IAuthService
    {
        bool ValidateUser(string usuario, string password, out string rol);
    }

    public class AuthService : IAuthService
    {
        public bool ValidateUser(string usuario, string password, out string rol)
        {
            rol = string.Empty;

            if (usuario == "admin.rrhh" && password == "Admin2025!")
            {
                rol = "RRHH";
                return true;
            }
            if (usuario == "jefatura" && password == "Jefe2025!")
            {
                rol = "Jefatura";
                return true;
            }

            return false;
        }
    }
}