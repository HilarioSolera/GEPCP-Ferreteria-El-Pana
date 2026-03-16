using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH")]
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(ApplicationDbContext context, ILogger<UsuariosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── INDEX ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(string? busqueda)
        {
            try
            {
                ViewBag.Busqueda = busqueda;

                var query = _context.Usuarios.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var termino = busqueda.Trim().ToLower();
                    query = query.Where(u =>
                        u.NombreUsuario.ToLower().Contains(termino) ||
                        u.NombreCompleto.ToLower().Contains(termino) ||
                        u.Rol.ToLower().Contains(termino)
                    );
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
                TempData["Error"] = "Ocurrió un error al cargar los usuarios. Intentá de nuevo.";
                return View(new List<Usuario>());
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public IActionResult Create()
        {
            return View(new UsuarioCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioCreateViewModel model)
        {
            try
            {
                AplicarValidacionesCrear(model);

                if (!ModelState.IsValid)
                    return View(model);

                var usuario = new Usuario
                {
                    NombreUsuario = model.NombreUsuario.Trim().ToLower(),
                    NombreCompleto = model.NombreCompleto.Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Rol = model.Rol
                };

                _context.Add(usuario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario creado: {NombreUsuario} Rol {Rol}",
                    usuario.NombreUsuario, usuario.Rol);
                TempData["Success"] = $"Usuario '{usuario.NombreUsuario}' creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al crear usuario: {NombreUsuario}", model.NombreUsuario);
                ModelState.AddModelError(string.Empty, "Error al guardar. Verificá que el nombre de usuario no esté duplicado.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear usuario");
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                return View(model);
            }
        }

        // ── CAMBIAR PASSWORD ──────────────────────────────────────────────────

        public async Task<IActionResult> CambiarPassword(int? id)
        {
            try
            {
                if (id == null || id <= 0) return NotFound();

                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null) return NotFound();

                return View(new CambiarPasswordViewModel
                {
                    UsuarioId = usuario.UsuarioId,
                    NombreUsuario = usuario.NombreUsuario
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de cambio de contraseña, ID: {Id}", id);
                TempData["Error"] = "Error al cargar el formulario. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel model)
        {
            try
            {
                AplicarValidacionesPassword(model);

                if (!ModelState.IsValid)
                    return View(model);

                var usuario = await _context.Usuarios.FindAsync(model.UsuarioId);
                if (usuario == null) return NotFound();

                // Verificar contraseña actual
                if (!BCrypt.Net.BCrypt.Verify(model.PasswordActual, usuario.PasswordHash))
                {
                    ModelState.AddModelError("PasswordActual", "La contraseña actual es incorrecta.");
                    return View(model);
                }

                // Verificar que la nueva no sea igual a la actual
                if (BCrypt.Net.BCrypt.Verify(model.PasswordNueva, usuario.PasswordHash))
                {
                    ModelState.AddModelError("PasswordNueva",
                        "La nueva contraseña no puede ser igual a la actual.");
                    return View(model);
                }

                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordNueva);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Contraseña actualizada: {NombreUsuario}", usuario.NombreUsuario);
                TempData["Success"] = $"Contraseña de '{usuario.NombreUsuario}' actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña, UsuarioId: {Id}", model.UsuarioId);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                return View(model);
            }
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
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
                    TempData["Error"] = "No podés eliminar tu propio usuario mientras estás conectado.";
                    return RedirectToAction(nameof(Index));
                }

                if (usuario.Rol == "RRHH")
                {
                    var totalRRHH = await _context.Usuarios.CountAsync(u => u.Rol == "RRHH");
                    if (totalRRHH <= 1)
                    {
                        TempData["Error"] = "No se puede eliminar el único usuario con rol RRHH. Creá otro primero.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario eliminado: {NombreUsuario} Rol {Rol}",
                    usuario.NombreUsuario, usuario.Rol);
                TempData["Success"] = $"Usuario '{usuario.NombreUsuario}' eliminado correctamente.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al eliminar usuario ID: {Id}", id);
                TempData["Error"] = "No se puede eliminar el usuario porque tiene registros asociados.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al eliminar usuario ID: {Id}", id);
                TempData["Error"] = "Ocurrió un error inesperado al eliminar el usuario.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private void AplicarValidacionesCrear(UsuarioCreateViewModel model)
        {
            // Nombre de usuario
            if (string.IsNullOrWhiteSpace(model.NombreUsuario))
            {
                ModelState.AddModelError("NombreUsuario", "El nombre de usuario es obligatorio.");
            }
            else if (!Regex.IsMatch(model.NombreUsuario.Trim(), @"^[a-zA-Z0-9\.\-_]{3,50}$"))
            {
                ModelState.AddModelError("NombreUsuario",
                    "El usuario solo puede contener letras, números, puntos, guiones y guiones bajos (3-50 caracteres).");
            }
            else if (_context.Usuarios.Any(u =>
                u.NombreUsuario.ToLower() == model.NombreUsuario.Trim().ToLower()))
            {
                ModelState.AddModelError("NombreUsuario", "Ya existe un usuario con ese nombre.");
            }

            // Nombre completo
            if (string.IsNullOrWhiteSpace(model.NombreCompleto))
                ModelState.AddModelError("NombreCompleto", "El nombre completo es obligatorio.");
            else if (model.NombreCompleto.Trim().Length < 5)
                ModelState.AddModelError("NombreCompleto", "El nombre completo debe tener al menos 5 caracteres.");
            else if (!Regex.IsMatch(model.NombreCompleto.Trim(),
                @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s'\-]{5,100}$"))
                ModelState.AddModelError("NombreCompleto",
                    "El nombre completo solo puede contener letras y espacios.");

            // Contraseña
            ValidarFortalezaPassword(model.Password, "Password");

            // Confirmar contraseña
            if (!string.IsNullOrWhiteSpace(model.Password) &&
                model.Password != model.ConfirmarPassword)
                ModelState.AddModelError("ConfirmarPassword", "Las contraseñas no coinciden.");

            // Rol
            var rolesValidos = new[] { "RRHH", "Jefatura" };
            if (string.IsNullOrWhiteSpace(model.Rol) || !rolesValidos.Contains(model.Rol))
                ModelState.AddModelError("Rol", "Seleccioná un rol válido.");
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
            {
                ModelState.AddModelError(campo, "La contraseña es obligatoria.");
                return;
            }
            if (password.Length < 8)
                ModelState.AddModelError(campo, "La contraseña debe tener al menos 8 caracteres.");
            else if (password.Length > 100)
                ModelState.AddModelError(campo, "La contraseña no puede superar 100 caracteres.");
            else if (!Regex.IsMatch(password, @"[A-Z]"))
                ModelState.AddModelError(campo, "La contraseña debe tener al menos una letra mayúscula.");
            else if (!Regex.IsMatch(password, @"[a-z]"))
                ModelState.AddModelError(campo, "La contraseña debe tener al menos una letra minúscula.");
            else if (!Regex.IsMatch(password, @"[0-9]"))
                ModelState.AddModelError(campo, "La contraseña debe tener al menos un número.");
            else if (!Regex.IsMatch(password, @"[!@#$%^&*\(\)\-_=\+\[\]\{\};:'"",\.<>\?\/\\|`~]"))
                ModelState.AddModelError(campo, "La contraseña debe tener al menos un carácter especial (!@#$%...).");
        }
    }
}