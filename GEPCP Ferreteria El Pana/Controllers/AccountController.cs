using Microsoft.AspNetCore.Mvc;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Services;
using System.Text.RegularExpressions;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // ── LOGIN GET ─────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Login()
        {
            try
            {
                // Si ya tiene sesión activa redirigir al Dashboard
                if (HttpContext.Session.GetString("Usuario") != null)
                    return RedirectToAction("Dashboard", "Home");

                return View(new LoginViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar pantalla de login");
                return View(new LoginViewModel());
            }
        }

        // ── LOGIN POST ────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            try
            {
                AplicarValidacionesLogin(model);

                if (!ModelState.IsValid)
                    return View(model);

                if (_authService.ValidateUser(model.Usuario, model.Password, out string rol))
                {
                    // Guardar sesión
                    HttpContext.Session.SetString("Usuario", model.Usuario.Trim().ToLower());
                    HttpContext.Session.SetString("Rol", rol);

                    _logger.LogInformation("Login exitoso: {Usuario} Rol {Rol}",
                        model.Usuario, rol);

                    return RedirectToAction("Dashboard", "Home");
                }

                // Login fallido
                _logger.LogWarning("Login fallido para usuario: {Usuario}", model.Usuario);
                ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en login para usuario: {Usuario}", model.Usuario);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                return View(model);
            }
        }

        // ── LOGOUT ────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            try
            {
                var usuario = HttpContext.Session.GetString("Usuario");
                HttpContext.Session.Clear();

                _logger.LogInformation("Logout: {Usuario}", usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar sesión");
            }

            return RedirectToAction(nameof(Login));
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private void AplicarValidacionesLogin(LoginViewModel model)
        {
            // Usuario
            if (string.IsNullOrWhiteSpace(model.Usuario))
            {
                ModelState.AddModelError("Usuario", "El nombre de usuario es obligatorio.");
            }
            else if (model.Usuario.Trim().Length < 3)
            {
                ModelState.AddModelError("Usuario", "El usuario debe tener al menos 3 caracteres.");
            }
            else if (model.Usuario.Trim().Length > 50)
            {
                ModelState.AddModelError("Usuario", "El usuario no puede superar 50 caracteres.");
            }
            else if (!Regex.IsMatch(model.Usuario.Trim(), @"^[a-zA-Z0-9\.\-_@]+$"))
            {
                ModelState.AddModelError("Usuario", "El usuario contiene caracteres no válidos.");
            }

            // Contraseña
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError("Password", "La contraseña es obligatoria.");
            else if (model.Password.Length < 8)
                ModelState.AddModelError("Password", "La contraseña debe tener al menos 8 caracteres.");
            else if (model.Password.Length > 100)
                ModelState.AddModelError("Password", "La contraseña no puede superar 100 caracteres.");
        }
    }
}