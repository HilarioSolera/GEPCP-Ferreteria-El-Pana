using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH")]
    public class ComisionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ComisionesController> _logger;

        public ComisionesController(ApplicationDbContext context, ILogger<ComisionesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── INDEX ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(string? busqueda, string? desde, string? hasta)
        {
            try
            {
                ViewBag.Busqueda = busqueda;
                ViewBag.Desde = desde;
                ViewBag.Hasta = hasta;

                var query = _context.Comisiones
                    .Include(c => c.Empleado)
                    .AsNoTracking()
                    .AsQueryable();

                // Filtro por búsqueda
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var termino = busqueda.Trim().ToLower();
                    query = query.Where(c =>
                        c.Empleado.Nombre.ToLower().Contains(termino) ||
                        c.Empleado.PrimerApellido.ToLower().Contains(termino) ||
                        c.Empleado.Cedula.Contains(termino) ||
                        c.Descripcion.ToLower().Contains(termino)
                    );
                }

                // Filtro por rango de fechas
                if (DateTime.TryParse(desde, out var fechaDesde))
                    query = query.Where(c => c.Fecha >= fechaDesde);

                if (DateTime.TryParse(hasta, out var fechaHasta))
                    query = query.Where(c => c.Fecha <= fechaHasta.AddDays(1));

                var comisiones = await query
                    .OrderByDescending(c => c.Fecha)
                    .ThenBy(c => c.Empleado.PrimerApellido)
                    .ToListAsync();

                // KPIs
                ViewBag.TotalComisiones = comisiones.Count;
                ViewBag.MontoTotal = comisiones.Sum(c => c.Monto);
                ViewBag.PromedioMonto = comisiones.Any()
                                            ? comisiones.Average(c => c.Monto)
                                            : 0;
                ViewBag.EmpleadosConComision = comisiones
                                            .Select(c => c.EmpleadoId)
                                            .Distinct()
                                            .Count();

                return View(comisiones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar listado de comisiones. Búsqueda: {B}", busqueda);
                TempData["Error"] = "Ocurrió un error al cargar las comisiones. Intentá de nuevo.";
                return View(new List<Comision>());
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public async Task<IActionResult> Create()
        {
            try
            {
                await CargarEmpleadosViewBag();
                return View(new Comision { Fecha = DateTime.Today });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de comisión");
                TempData["Error"] = "Error al cargar el formulario. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Comision model)
        {
            try
            {
                ModelState.Remove("Empleado");
                AplicarValidaciones(model);

                if (!ModelState.IsValid)
                {
                    await CargarEmpleadosViewBag(model.EmpleadoId);
                    return View(model);
                }

                model.Descripcion = SanitizarTexto(model.Descripcion);
                model.Monto = Math.Round(model.Monto, 2);

                _context.Add(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Comisión creada: EmpleadoId {EId} Monto {M} Fecha {F}",
                    model.EmpleadoId, model.Monto, model.Fecha);
                TempData["Success"] = $"Comisión de ₡{model.Monto:N0} registrada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al crear comisión");
                ModelState.AddModelError(string.Empty, "Error al guardar en la base de datos. Intentá de nuevo.");
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear comisión");
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }
        }

        // ── EDIT ──────────────────────────────────────────────────────────────

        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null || id <= 0) return NotFound();

                var comision = await _context.Comisiones.FindAsync(id);
                if (comision == null) return NotFound();

                await CargarEmpleadosViewBag(comision.EmpleadoId);
                return View(comision);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de edición, ID: {Id}", id);
                TempData["Error"] = "Error al cargar el formulario de edición.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Comision model)
        {
            try
            {
                if (id != model.ComisionId) return NotFound();

                ModelState.Remove("Empleado");
                AplicarValidaciones(model);

                if (!ModelState.IsValid)
                {
                    await CargarEmpleadosViewBag(model.EmpleadoId);
                    return View(model);
                }

                model.Descripcion = SanitizarTexto(model.Descripcion);
                model.Monto = Math.Round(model.Monto, 2);

                _context.Update(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Comisión actualizada: ID {Id}", id);
                TempData["Success"] = $"Comisión de ₡{model.Monto:N0} actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error de concurrencia al editar comisión ID: {Id}", id);
                if (!await _context.Comisiones.AnyAsync(c => c.ComisionId == id))
                    return NotFound();

                ModelState.AddModelError(string.Empty,
                    "El registro fue modificado por otro usuario. Recargá e intentá de nuevo.");
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al editar comisión ID: {Id}", id);
                ModelState.AddModelError(string.Empty, "Error al guardar en la base de datos.");
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al editar comisión ID: {Id}", id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                await CargarEmpleadosViewBag(model.EmpleadoId);
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
                var comision = await _context.Comisiones
                    .Include(c => c.Empleado)
                    .FirstOrDefaultAsync(c => c.ComisionId == id);

                if (comision == null)
                {
                    TempData["Error"] = "Comisión no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Comisiones.Remove(comision);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Comisión eliminada: ID {Id} EmpleadoId {EId}", id, comision.EmpleadoId);
                TempData["Success"] = $"Comisión de ₡{comision.Monto:N0} de {comision.Empleado.PrimerApellido} {comision.Empleado.Nombre} eliminada correctamente.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al eliminar comisión ID: {Id}", id);
                TempData["Error"] = "No se puede eliminar la comisión porque tiene registros asociados.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al eliminar comisión ID: {Id}", id);
                TempData["Error"] = "Ocurrió un error inesperado al eliminar la comisión.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private async Task CargarEmpleadosViewBag(int? selectedId = null)
        {
            ViewBag.Empleados = await _context.Empleados
                .Where(e => e.Activo)
                .OrderBy(e => e.PrimerApellido)
                .ThenBy(e => e.Nombre)
                .Select(e => new SelectListItem
                {
                    Value = e.EmpleadoId.ToString(),
                    Text = $"{e.PrimerApellido} {e.SegundoApellido} {e.Nombre} — {e.Puesto}",
                    Selected = e.EmpleadoId == selectedId
                })
                .ToListAsync();
        }

        private static string SanitizarTexto(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return Regex.Replace(input.Trim(), @"[<>""'%;()&]", string.Empty);
        }

        private void AplicarValidaciones(Comision model)
        {
            // Empleado
            if (model.EmpleadoId <= 0)
                ModelState.AddModelError("EmpleadoId", "Seleccioná un empleado válido.");

            // Monto
            if (model.Monto <= 0)
                ModelState.AddModelError("Monto", "El monto debe ser mayor a cero.");
            else if (model.Monto > 9_999_999.99m)
                ModelState.AddModelError("Monto", "El monto excede el límite máximo permitido (₡9,999,999.99).");

            // Fecha
            if (model.Fecha == default)
                ModelState.AddModelError("Fecha", "La fecha es obligatoria.");
            else if (model.Fecha > DateTime.Today.AddDays(1))
                ModelState.AddModelError("Fecha", "La fecha no puede ser futura.");
            else if (model.Fecha < DateTime.Today.AddYears(-2))
                ModelState.AddModelError("Fecha", "La fecha no puede ser anterior a 2 años.");

            // Descripción
            if (string.IsNullOrWhiteSpace(model.Descripcion))
                ModelState.AddModelError("Descripcion", "La descripción es obligatoria.");
            else if (model.Descripcion.Trim().Length < 5)
                ModelState.AddModelError("Descripcion", "La descripción debe tener al menos 5 caracteres.");
            else if (model.Descripcion.Trim().Length > 200)
                ModelState.AddModelError("Descripcion", "La descripción no puede superar 200 caracteres.");
        }
    }
}