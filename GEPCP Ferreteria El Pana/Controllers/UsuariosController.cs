using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH")]
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Usuarios
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .OrderBy(u => u.NombreUsuario)
                .ToListAsync();
            return View(usuarios);
        }

        // GET: /Usuarios/Create
        public IActionResult Create()
        {
            return View(new UsuarioCreateViewModel());
        }

        // POST: /Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioCreateViewModel model)
        {
            if (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == model.NombreUsuario))
            {
                ModelState.AddModelError("NombreUsuario", "Ya existe un usuario con ese nombre.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var usuario = new Usuario
            {
                NombreUsuario = model.NombreUsuario,
                NombreCompleto = model.NombreCompleto,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Rol = model.Rol
            };

            _context.Add(usuario);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Usuario '{model.NombreUsuario}' creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Usuarios/CambiarPassword/5
        public async Task<IActionResult> CambiarPassword(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            return View(new CambiarPasswordViewModel
            {
                UsuarioId = usuario.UsuarioId,
                NombreUsuario = usuario.NombreUsuario
            });
        }

        // POST: /Usuarios/CambiarPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel model)
        {
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

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordNueva);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Contraseña de '{usuario.NombreUsuario}' actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Usuarios/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Evitar eliminar el usuario actualmente logueado
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

            // Evitar eliminar si es el único usuario RRHH
            if (usuario.Rol == "RRHH")
            {
                var totalRRHH = await _context.Usuarios.CountAsync(u => u.Rol == "RRHH");
                if (totalRRHH <= 1)
                {
                    TempData["Error"] = "No se puede eliminar el único usuario con rol RRHH.";
                    return RedirectToAction(nameof(Index));
                }
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Usuario '{usuario.NombreUsuario}' eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}