using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly AuditoriaService _auditoria;

        public AccountController(
            IAuthService authService,
            ILogger<AccountController> logger,
            ApplicationDbContext context,
            EmailService emailService,
            AuditoriaService auditoria)
        {
            _authService = authService;
            _logger = logger;
            _context = context;
            _emailService = emailService;
            _auditoria = auditoria;
        }

        // LOGIN GET

        [HttpGet]
        public IActionResult Login()
        {
            try
            {
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

        // LOGIN POST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                AplicarValidacionesLogin(model);
                if (!ModelState.IsValid)
                    return View(model);

                if (_authService.ValidateUser(model.Usuario, model.Password, out string rol))
                {
                    HttpContext.Session.SetString("Usuario", model.Usuario.Trim().ToLower());
                    HttpContext.Session.SetString("Rol", rol);

                    await _auditoria.RegistrarAsync(
                        model.Usuario.Trim().ToLower(),
                        "Login exitoso", "Autenticación",
                        ip: HttpContext.Connection.RemoteIpAddress?.ToString());

                    _logger.LogInformation("Login exitoso: {Usuario} Rol {Rol}",
                        model.Usuario, rol);

                    return RedirectToAction("Dashboard", "Home");
                }

                await _auditoria.RegistrarAsync(
                    model.Usuario.Trim().ToLower(),
                    "Login fallido", "Autenticación",
                    ip: HttpContext.Connection.RemoteIpAddress?.ToString());

                _logger.LogWarning("Login fallido para usuario: {Usuario}", model.Usuario);
                ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en login: {Usuario}", model.Usuario);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                return View(model);
            }
        }

        // LOGOUT

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var usuario = HttpContext.Session.GetString("Usuario");

                await _auditoria.RegistrarAsync(
                    usuario ?? "", "Logout", "Autenticación");

                HttpContext.Session.Clear();
                _logger.LogInformation("Logout: {Usuario}", usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar sesión");
            }
            return RedirectToAction(nameof(Login));
        }

        // HELPERS

        private void AplicarValidacionesLogin(LoginViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Usuario))
                ModelState.AddModelError("Usuario", "El nombre de usuario es obligatorio.");
            else if (model.Usuario.Trim().Length < 3)
                ModelState.AddModelError("Usuario", "El usuario debe tener al menos 3 caracteres.");
            else if (model.Usuario.Trim().Length > 50)
                ModelState.AddModelError("Usuario", "El usuario no puede superar 50 caracteres.");
            else if (!Regex.IsMatch(model.Usuario.Trim(), @"^[a-zA-Z0-9\.\-_@]+$"))
                ModelState.AddModelError("Usuario", "El usuario contiene caracteres no válidos.");

            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError("Password", "La contraseña es obligatoria.");
            else if (model.Password.Length < 8)
                ModelState.AddModelError("Password", "La contraseña debe tener al menos 8 caracteres.");
            else if (model.Password.Length > 100)
                ModelState.AddModelError("Password", "La contraseña no puede superar 100 caracteres.");
        }

        // OLVIDÉ MI CONTRASEÑA

        [HttpGet]
        public IActionResult OlvidePassword()
        {
            if (HttpContext.Session.GetString("Usuario") != null)
                return RedirectToAction("Dashboard", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OlvidePassword(string usuario)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(usuario))
                {
                    ModelState.AddModelError(string.Empty, "Ingresá tu nombre de usuario.");
                    return View();
                }

                var user = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.NombreUsuario == usuario.Trim().ToLower());

                if (user == null || string.IsNullOrWhiteSpace(user.CorreoElectronico))
                {
                    TempData["Info"] = "Si el usuario existe y tiene correo registrado, recibirás el código.";
                    return RedirectToAction(nameof(VerificarCodigo), new { usuario });
                }

                var codigo = new Random().Next(100000, 999999).ToString();
                user.TokenRecuperacion = BCrypt.Net.BCrypt.HashPassword(codigo);
                user.TokenExpiracion = DateTime.Now.AddMinutes(15);
                await _context.SaveChangesAsync();

                await _emailService.EnviarCodigoRecuperacionAsync(user.CorreoElectronico, codigo);

                await _auditoria.RegistrarAsync(
                    usuario.Trim().ToLower(),
                    "Solicitud recuperación contraseña", "Autenticación");

                _logger.LogInformation("Código recuperación enviado: {U}", usuario);
                TempData["Info"] = "Si el usuario existe y tiene correo registrado, recibirás el código.";
                return RedirectToAction(nameof(VerificarCodigo), new { usuario });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en recuperación: {U}", usuario);
                TempData["Error"] = "Ocurrió un error. Intentá de nuevo.";
                return View();
            }
        }

        // VERIFICAR CÓDIGO

        [HttpGet]
        public IActionResult VerificarCodigo(string usuario)
        {
            ViewBag.Usuario = usuario;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerificarCodigo(string usuario, string codigo)
        {
            try
            {
                ViewBag.Usuario = usuario;

                if (string.IsNullOrWhiteSpace(codigo))
                {
                    ModelState.AddModelError(string.Empty, "Ingresá el código.");
                    return View();
                }

                var user = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.NombreUsuario == usuario.Trim().ToLower());

                if (user == null ||
                    string.IsNullOrWhiteSpace(user.TokenRecuperacion) ||
                    user.TokenExpiracion == null ||
                    user.TokenExpiracion < DateTime.Now)
                {
                    ModelState.AddModelError(string.Empty,
                        "El código expiró o no es válido. Solicitá uno nuevo.");
                    return View();
                }

                if (!BCrypt.Net.BCrypt.Verify(codigo.Trim(), user.TokenRecuperacion))
                {
                    await _auditoria.RegistrarAsync(
                        usuario.Trim().ToLower(),
                        "Código recuperación incorrecto", "Autenticación");

                    ModelState.AddModelError(string.Empty, "Código incorrecto.");
                    return View();
                }

                TempData["RecuperacionUsuario"] = usuario.Trim().ToLower();
                TempData["RecuperacionValida"] = "si";
                return RedirectToAction(nameof(NuevaPassword));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar código: {U}", usuario);
                TempData["Error"] = "Ocurrió un error. Intentá de nuevo.";
                return View();
            }
        }

        // NUEVA CONTRASEÑA

        [HttpGet]
        public IActionResult NuevaPassword()
        {
            var tempValida = TempData["RecuperacionValida"] as string;
            var tempUsuario = TempData["RecuperacionUsuario"] as string;

            if (tempValida == "si" && !string.IsNullOrWhiteSpace(tempUsuario))
            {
                HttpContext.Session.SetString("RecuperacionValida", "si");
                HttpContext.Session.SetString("RecuperacionUsuario", tempUsuario);
                ViewBag.UsuarioRecuperacion = tempUsuario;
                return View();
            }

            var sessUsuario = HttpContext.Session.GetString("RecuperacionUsuario");
            if (!string.IsNullOrWhiteSpace(sessUsuario))
            {
                ViewBag.UsuarioRecuperacion = sessUsuario;
                return View();
            }

            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NuevaPassword(
            string password, string confirmar, string usuarioRecuperacion)
        {
            try
            {
                var valida = HttpContext.Session.GetString("RecuperacionValida");
                var usuario = HttpContext.Session.GetString("RecuperacionUsuario");

                if (string.IsNullOrWhiteSpace(usuario) &&
                    !string.IsNullOrWhiteSpace(usuarioRecuperacion))
                {
                    var userCheck = await _context.Usuarios
                        .FirstOrDefaultAsync(u =>
                            u.NombreUsuario == usuarioRecuperacion &&
                            u.TokenExpiracion != null &&
                            u.TokenExpiracion > DateTime.Now);

                    if (userCheck == null)
                    {
                        TempData["Error"] = "La sesión expiró. Solicitá un nuevo código.";
                        return RedirectToAction(nameof(OlvidePassword));
                    }
                    usuario = usuarioRecuperacion;
                }
                else if (valida != "si" || string.IsNullOrWhiteSpace(usuario))
                {
                    TempData["Error"] = "La sesión expiró. Solicitá un nuevo código.";
                    return RedirectToAction(nameof(OlvidePassword));
                }

                if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                {
                    ModelState.AddModelError(string.Empty,
                        "La contraseña debe tener al menos 8 caracteres.");
                    return View();
                }

                if (password != confirmar)
                {
                    ModelState.AddModelError(string.Empty, "Las contraseñas no coinciden.");
                    return View();
                }

                var user = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.NombreUsuario == usuario);

                if (user == null) return RedirectToAction(nameof(Login));

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                user.TokenRecuperacion = null;
                user.TokenExpiracion = null;
                await _context.SaveChangesAsync();

                HttpContext.Session.Remove("RecuperacionValida");
                HttpContext.Session.Remove("RecuperacionUsuario");

                await _auditoria.RegistrarAsync(
                    usuario, "Contraseña restablecida", "Autenticación");

                _logger.LogInformation("Contraseña restablecida: {U}", usuario);
                TempData["Success"] = "Contraseña actualizada. Podés iniciar sesión.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restablecer contraseña");
                TempData["Error"] = "Ocurrió un error. Intentá de nuevo.";
                return View();
            }
        }
    }
}