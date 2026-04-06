using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Filters;
using System.Text.RegularExpressions;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class PuestosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PuestosController> _logger;

        public PuestosController(ApplicationDbContext context, ILogger<PuestosController> logger)
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

                var query = _context.Puestos.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var termino = busqueda.Trim().ToLower();
                    query = query.Where(p => p.Nombre.ToLower().Contains(termino));
                }

                var puestos = await query
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();

                ViewBag.TotalPuestos = puestos.Count;
                ViewBag.TotalActivos = puestos.Count(p => p.Activo);
                ViewBag.SalarioPromedio = puestos.Any()
                                            ? puestos.Average(p => p.SalarioBase)
                                            : 0;

                // Cantidad de empleados por puesto para la columna del Index
                ViewBag.EmpleadosPorPuesto = await _context.Empleados
                    .GroupBy(e => e.Puesto)
                    .Select(g => new { Puesto = g.Key, Total = g.Count() })
                    .ToDictionaryAsync(x => x.Puesto, x => x.Total);

                return View(puestos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar listado de puestos. Búsqueda: {B}", busqueda);
                TempData["Error"] = "Ocurrió un error al cargar los puestos. Intentá de nuevo.";
                return View(new List<Puesto>());
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public IActionResult Create()
        {
            return View(new Puesto { Activo = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Puesto model)
        {
            try
            {
                AplicarValidaciones(model, null);

                if (!ModelState.IsValid)
                    return View(model);

                model.Nombre = SanitizarTexto(model.Nombre);
                model.SalarioBase = Math.Round(model.SalarioBase, 2);

                _context.Add(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Puesto creado: {Nombre} Salario {S}", model.Nombre, model.SalarioBase);
                TempData["Success"] = $"Puesto '{model.Nombre}' creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al crear puesto: {Nombre}", model.Nombre);
                ModelState.AddModelError(string.Empty, "Error al guardar en la base de datos. Verificá que el nombre no esté duplicado.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear puesto");
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                return View(model);
            }
        }

        // ── EDIT ──────────────────────────────────────────────────────────────

        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null || id <= 0) return NotFound();

                var puesto = await _context.Puestos.FindAsync(id);
                if (puesto == null) return NotFound();

                return View(puesto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de edición de puesto ID: {Id}", id);
                TempData["Error"] = "Error al cargar el formulario de edición.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Puesto model)
        {
            try
            {
                if (id != model.PuestoId) return NotFound();

                AplicarValidaciones(model, id);

                if (!ModelState.IsValid)
                    return View(model);

                model.Nombre = SanitizarTexto(model.Nombre);
                model.SalarioBase = Math.Round(model.SalarioBase, 2);

                _context.Update(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Puesto actualizado: ID {Id} Nombre {N}", id, model.Nombre);
                TempData["Success"] = $"Puesto '{model.Nombre}' actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error de concurrencia al editar puesto ID: {Id}", id);
                if (!await _context.Puestos.AnyAsync(p => p.PuestoId == id))
                    return NotFound();

                ModelState.AddModelError(string.Empty,
                    "El registro fue modificado por otro usuario. Recargá e intentá de nuevo.");
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al editar puesto ID: {Id}", id);
                ModelState.AddModelError(string.Empty, "Error al guardar en la base de datos.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al editar puesto ID: {Id}", id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                return View(model);
            }
        }

        // ── ACTIVAR ───────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(int id)
        {
            try
            {
                var puesto = await _context.Puestos.FindAsync(id);
                if (puesto == null)
                {
                    TempData["Error"] = "Puesto no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                puesto.Activo = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Puesto activado: ID {Id}", id);
                TempData["Success"] = $"Puesto '{puesto.Nombre}' activado correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar puesto ID: {Id}", id);
                TempData["Error"] = "Error al activar el puesto. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── DESACTIVAR ────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            try
            {
                var puesto = await _context.Puestos.FindAsync(id);
                if (puesto == null)
                {
                    TempData["Error"] = "Puesto no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si hay empleados activos usando este puesto
                var empleadosActivos = await _context.Empleados
                    .CountAsync(e => e.Puesto == puesto.Nombre && e.Activo);

                if (empleadosActivos > 0)
                {
                    TempData["Error"] = $"No se puede desactivar '{puesto.Nombre}' porque tiene {empleadosActivos} empleado(s) activo(s) asignado(s).";
                    return RedirectToAction(nameof(Index));
                }

                puesto.Activo = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Puesto desactivado: ID {Id}", id);
                TempData["Success"] = $"Puesto '{puesto.Nombre}' desactivado correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar puesto ID: {Id}", id);
                TempData["Error"] = "Error al desactivar el puesto. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var puesto = await _context.Puestos.FindAsync(id);
                if (puesto == null)
                {
                    TempData["Error"] = "Puesto no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar que no haya empleados usando este puesto
                var totalEmpleados = await _context.Empleados
                    .CountAsync(e => e.Puesto == puesto.Nombre);

                if (totalEmpleados > 0)
                {
                    TempData["Error"] = $"No se puede eliminar '{puesto.Nombre}' porque tiene {totalEmpleados} empleado(s) asignado(s). Desactivalo en su lugar.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Puestos.Remove(puesto);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Puesto eliminado: ID {Id} Nombre {N}", id, puesto.Nombre);
                TempData["Success"] = $"Puesto '{puesto.Nombre}' eliminado correctamente.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al eliminar puesto ID: {Id}", id);
                TempData["Error"] = "No se puede eliminar el puesto porque tiene registros asociados.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al eliminar puesto ID: {Id}", id);
                TempData["Error"] = "Ocurrió un error inesperado al eliminar el puesto.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private static string SanitizarTexto(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return Regex.Replace(input.Trim(), @"[<>""'%;()&]", string.Empty);
        }

        private void AplicarValidaciones(Puesto model, int? idActual)
        {
            // Nombre
            if (string.IsNullOrWhiteSpace(model.Nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre del puesto es obligatorio.");
            }
            else if (model.Nombre.Trim().Length < 2)
            {
                ModelState.AddModelError("Nombre", "El nombre debe tener al menos 2 caracteres.");
            }
            else if (model.Nombre.Trim().Length > 100)
            {
                ModelState.AddModelError("Nombre", "El nombre no puede superar 100 caracteres.");
            }
            else if (!Regex.IsMatch(model.Nombre.Trim(), @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\.\-\/]{2,100}$"))
            {
                ModelState.AddModelError("Nombre", "El nombre solo puede contener letras, espacios, puntos y guiones.");
            }
            else if (_context.Puestos.Any(p =>
                p.Nombre.ToLower() == model.Nombre.Trim().ToLower() &&
                (idActual == null || p.PuestoId != idActual)))
            {
                ModelState.AddModelError("Nombre", "Ya existe un puesto con ese nombre.");
            }

            // Salario
            if (model.SalarioBase < 0)
                ModelState.AddModelError("SalarioBase", "El salario no puede ser negativo.");
            else if (model.SalarioBase > 9_999_999.99m)
                ModelState.AddModelError("SalarioBase", "El salario excede el límite máximo (₡9,999,999.99).");
        }
    }
}