using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsuariosController> _logger;
        private readonly AuditoriaService _auditoria;

        public UsuariosController(
            ApplicationDbContext context,
            ILogger<UsuariosController> logger,
            AuditoriaService auditoria)
        {
            _context = context;
            _logger = logger;
            _auditoria = auditoria;
        }

        // INDEX

        public async Task<IActionResult> Index(string? busqueda)
        {
            try
            {
                var rolActual = HttpContext.Session.GetString("Rol");
                var usuarioActual = HttpContext.Session.GetString("Usuario");
                ViewBag.Busqueda = busqueda;

                var query = _context.Usuarios.AsNoTracking().AsQueryable();

                if (rolActual == "RRHH")
                    query = query.Where(u => u.NombreUsuario == usuarioActual);
                else if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var termino = busqueda.Trim().ToLower();
                    query = query.Where(u =>
                        u.NombreUsuario.ToLower().Contains(termino) ||
                        u.NombreCompleto.ToLower().Contains(termino) ||
                        u.Rol.ToLower().Contains(termino));
                }

                var usuarios = await query
                    .OrderBy(u => u.Rol)
                    .ThenBy(u => u.NombreUsuario)
                    .ToListAsync();

                ViewBag.TotalUsuarios = usuarios.Count;
                ViewBag.TotalRRHH = usuarios.Count(u => u.Rol == "RRHH");
                ViewBag.TotalJefatura = usuarios.Count(u => u.Rol == "Jefatura");

                return View(usuarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar listado de usuarios");
                TempData["Error"] = "Error al cargar los usuarios.";
                return View(new List<Usuario>());
            }
        }

        // CREATE — solo Jefatura

        [CustomAuthorize("Jefatura")]
        public IActionResult Create()
        {
            return View(new UsuarioCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize("Jefatura")]
        public async Task<IActionResult> Create(UsuarioCreateViewModel model)
        {
            try
            {
                AplicarValidacionesCrear(model);
                if (!ModelState.IsValid) return View(model);

                var usuario = new Usuario
                {
                    NombreUsuario = model.NombreUsuario.Trim().ToLower(),
                    NombreCompleto = model.NombreCompleto.Trim(),
                    CorreoElectronico = string.IsNullOrWhiteSpace(model.CorreoElectronico)
                                        ? null : model.CorreoElectronico.Trim().ToLower(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Rol = model.Rol
                };

                _context.Add(usuario);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Crear usuario", "Usuarios",
                    $"Usuario: {usuario.NombreUsuario} — Rol: {usuario.Rol}");

                _logger.LogInformation("Usuario creado: {U} Rol {R}",
                    usuario.NombreUsuario, usuario.Rol);

                TempData["Success"] = $"Usuario '{usuario.NombreUsuario}' creado correctamente.";
                TempData["Recomendacion"] = "Recordá compartir las credenciales de forma segura al nuevo usuario.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al crear usuario");
                ModelState.AddModelError(string.Empty,
                    "Error al guardar. Verificá que el nombre de usuario no esté duplicado.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear usuario");
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado.");
                return View(model);
            }
        }

        // EDIT — solo propio usuario

        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null || id <= 0) return NotFound();

                var usuarioActual = HttpContext.Session.GetString("Usuario");
                var objetivo = await _context.Usuarios.FindAsync(id);
                if (objetivo == null) return NotFound();

                if (objetivo.NombreUsuario != usuarioActual)
                {
                    TempData["Error"] = "Solo podés editar tu propio usuario.";
                    return RedirectToAction(nameof(Index));
                }

                return View(new UsuarioEditViewModel
                {
                    UsuarioId = objetivo.UsuarioId,
                    NombreUsuario = objetivo.NombreUsuario,
                    NombreCompleto = objetivo.NombreCompleto,
                    CorreoElectronico = objetivo.CorreoElectronico,
                    Rol = objetivo.Rol
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar edición usuario ID: {Id}", id);
                TempData["Error"] = "Error al cargar el formulario.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UsuarioEditViewModel model)
        {
            try
            {
                if (id != model.UsuarioId) return NotFound();

                var usuarioActual = HttpContext.Session.GetString("Usuario");
                var objetivo = await _context.Usuarios.FindAsync(id);
                if (objetivo == null) return NotFound();

                if (objetivo.NombreUsuario != usuarioActual)
                {
                    TempData["Error"] = "Solo podés editar tu propio usuario.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(model.NombreCompleto) ||
                    model.NombreCompleto.Trim().Length < 5)
                    ModelState.AddModelError("NombreCompleto",
                        "El nombre completo debe tener al menos 5 caracteres.");

                if (!string.IsNullOrWhiteSpace(model.CorreoElectronico) &&
                    !Regex.IsMatch(model.CorreoElectronico.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    ModelState.AddModelError("CorreoElectronico", "El formato del correo electrónico no es válido.");

                if (!ModelState.IsValid) return View(model);

                objetivo.NombreCompleto = model.NombreCompleto.Trim();
                objetivo.CorreoElectronico = string.IsNullOrWhiteSpace(model.CorreoElectronico)
                                             ? null : model.CorreoElectronico.Trim().ToLower();

                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    usuarioActual ?? "",
                    "Editar usuario", "Usuarios",
                    $"Usuario: {objetivo.NombreUsuario}");

                _logger.LogInformation("Usuario editado: ID {Id}", id);
                TempData["Success"] = "Datos actualizados correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar usuario ID: {Id}", id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado.");
                return View(model);
            }
        }

        // CAMBIAR PASSWORD — solo propio usuario

        public async Task<IActionResult> CambiarPassword(int? id)
        {
            try
            {
                if (id == null || id <= 0) return NotFound();

                var usuarioActual = HttpContext.Session.GetString("Usuario");
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null) return NotFound();

                if (usuario.NombreUsuario != usuarioActual)
                {
                    TempData["Error"] = "Solo podés cambiar tu propia contraseña.";
                    return RedirectToAction(nameof(Index));
                }

                return View(new CambiarPasswordViewModel
                {
                    UsuarioId = usuario.UsuarioId,
                    NombreUsuario = usuario.NombreUsuario
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar cambio de contraseña ID: {Id}", id);
                TempData["Error"] = "Error al cargar el formulario.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel model)
        {
            try
            {
                var usuarioActual = HttpContext.Session.GetString("Usuario");
                AplicarValidacionesPassword(model);
                if (!ModelState.IsValid) return View(model);

                var usuario = await _context.Usuarios.FindAsync(model.UsuarioId);
                if (usuario == null) return NotFound();

                if (usuario.NombreUsuario != usuarioActual)
                {
                    TempData["Error"] = "Solo podés cambiar tu propia contraseña.";
                    return RedirectToAction(nameof(Index));
                }

                if (!BCrypt.Net.BCrypt.Verify(model.PasswordActual, usuario.PasswordHash))
                {
                    ModelState.AddModelError("PasswordActual", "La contraseña actual es incorrecta.");
                    return View(model);
                }

                if (BCrypt.Net.BCrypt.Verify(model.PasswordNueva, usuario.PasswordHash))
                {
                    ModelState.AddModelError("PasswordNueva",
                        "La nueva contraseña no puede ser igual a la actual.");
                    return View(model);
                }

                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordNueva);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    usuarioActual ?? "",
                    "Cambiar contraseña", "Usuarios",
                    $"Usuario: {usuario.NombreUsuario}");

                _logger.LogInformation("Contraseña actualizada: {U}", usuario.NombreUsuario);
                TempData["Success"] = $"Contraseña de '{usuario.NombreUsuario}' actualizada correctamente.";
                TempData["Recomendacion"] = "Se recomienda cambiar la contraseña periódicamente para mayor seguridad.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña ID: {Id}", model.UsuarioId);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado.");
                return View(model);
            }
        }

        // DELETE — solo Jefatura

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize("Jefatura")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var usuarioActual = HttpContext.Session.GetString("Usuario");
                var usuario = await _context.Usuarios.FindAsync(id);

                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if (usuario.NombreUsuario == usuarioActual)
                {
                    TempData["Error"] = "No podés eliminar tu propio usuario.";
                    return RedirectToAction(nameof(Index));
                }

                if (usuario.Rol == "RRHH")
                {
                    var totalRRHH = await _context.Usuarios.CountAsync(u => u.Rol == "RRHH");
                    if (totalRRHH <= 1)
                    {
                        TempData["Error"] = "No se puede eliminar el único usuario RRHH.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    usuarioActual ?? "",
                    "Eliminar usuario", "Usuarios",
                    $"Usuario: {usuario.NombreUsuario} — Rol: {usuario.Rol}");

                _logger.LogInformation("Usuario eliminado: {U}", usuario.NombreUsuario);
                TempData["Success"] = $"Usuario '{usuario.NombreUsuario}' eliminado correctamente.";

                var totalJefatura = await _context.Usuarios.CountAsync(u => u.Rol == "Jefatura");
                if (totalJefatura <= 1)
                    TempData["Warning"] = "Solo queda un usuario con rol Jefatura. Considerá crear otro para evitar perder acceso administrativo.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario ID: {Id}", id);
                TempData["Error"] = "Error al eliminar el usuario.";
            }

            return RedirectToAction(nameof(Index));
        }

        // HELPERS

        private void AplicarValidacionesCrear(UsuarioCreateViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.NombreUsuario))
                ModelState.AddModelError("NombreUsuario", "El nombre de usuario es obligatorio.");
            else if (!Regex.IsMatch(model.NombreUsuario.Trim(), @"^[a-zA-Z0-9\.\-_]{3,50}$"))
                ModelState.AddModelError("NombreUsuario",
                    "Solo letras, números, puntos y guiones (3-50 caracteres).");
            else if (_context.Usuarios.Any(u =>
                u.NombreUsuario.ToLower() == model.NombreUsuario.Trim().ToLower()))
                ModelState.AddModelError("NombreUsuario", "Ya existe ese nombre de usuario.");

            if (string.IsNullOrWhiteSpace(model.NombreCompleto) ||
                model.NombreCompleto.Trim().Length < 5)
                ModelState.AddModelError("NombreCompleto",
                    "El nombre completo debe tener al menos 5 caracteres.");

            ValidarFortalezaPassword(model.Password, "Password");

            if (!string.IsNullOrWhiteSpace(model.Password) &&
                model.Password != model.ConfirmarPassword)
                ModelState.AddModelError("ConfirmarPassword", "Las contraseñas no coinciden.");

            var rolesValidos = new[] { "RRHH", "Jefatura" };
            if (string.IsNullOrWhiteSpace(model.Rol) || !rolesValidos.Contains(model.Rol))
                ModelState.AddModelError("Rol", "Seleccioná un rol válido.");

            if (!string.IsNullOrWhiteSpace(model.CorreoElectronico) &&
                !Regex.IsMatch(model.CorreoElectronico.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                ModelState.AddModelError("CorreoElectronico", "El formato del correo electrónico no es válido.");
        }

        private void AplicarValidacionesPassword(CambiarPasswordViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.PasswordActual))
                ModelState.AddModelError("PasswordActual", "La contraseña actual es obligatoria.");

            ValidarFortalezaPassword(model.PasswordNueva, "PasswordNueva");

            if (!string.IsNullOrWhiteSpace(model.PasswordNueva) &&
                model.PasswordNueva != model.ConfirmarPassword)
                ModelState.AddModelError("ConfirmarPassword", "Las contraseñas no coinciden.");
        }

        private void ValidarFortalezaPassword(string? password, string campo)
        {
            if (string.IsNullOrWhiteSpace(password))
            { ModelState.AddModelError(campo, "La contraseña es obligatoria."); return; }

            if (password.Length < 8)
                ModelState.AddModelError(campo, "Mínimo 8 caracteres.");
            else if (!Regex.IsMatch(password, @"[A-Z]"))
                ModelState.AddModelError(campo, "Debe tener al menos una mayúscula.");
            else if (!Regex.IsMatch(password, @"[a-z]"))
                ModelState.AddModelError(campo, "Debe tener al menos una minúscula.");
            else if (!Regex.IsMatch(password, @"[0-9]"))
                ModelState.AddModelError(campo, "Debe tener al menos un número.");
            else if (!Regex.IsMatch(password, @"[!@#$%^&*\(\)\-_=\+\[\]\{\};:"",\.<>\?\/\\|`~]"))
                ModelState.AddModelError(campo, "Debe tener al menos un carácter especial.");
        }
    }
}