using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH")]
    public class HorasExtrasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HorasExtrasController> _logger;
        private readonly ComprobantePlanillaService _servicioPDF;

        public HorasExtrasController(
            ApplicationDbContext context,
            ILogger<HorasExtrasController> logger,
            ComprobantePlanillaService servicioPDF)
        {
            _context = context;
            _logger = logger;
            _servicioPDF = servicioPDF;
        }

        // ── INDEX ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(int? periodoId, string? busqueda)
        {
            try
            {
                ViewBag.Busqueda = busqueda;

                // Periodos disponibles para filtro
                var periodos = await _context.PeriodosPago
                    .AsNoTracking()
                    .OrderByDescending(p => p.Anio)
                    .ThenByDescending(p => p.Mes)
                    .ThenByDescending(p => p.Quincena)
                    .ToListAsync();

                ViewBag.Periodos = new SelectList(periodos, "PeriodoPagoId", "Descripcion", periodoId);
                ViewBag.PeriodoId = periodoId;

                // Si no hay período seleccionado, usar el más reciente abierto
                if (periodoId == null)
                {
                    var activo = periodos.FirstOrDefault(p => p.Estado == EstadoPeriodo.Abierto)
                              ?? periodos.FirstOrDefault();
                    periodoId = activo?.PeriodoPagoId;
                    ViewBag.PeriodoId = periodoId;
                }

                var query = _context.HorasExtras
                    .Include(h => h.Empleado)
                    .Include(h => h.PeriodoPago)
                    .AsNoTracking()
                    .AsQueryable();

                if (periodoId.HasValue)
                    query = query.Where(h => h.PeriodoPagoId == periodoId.Value);

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var termino = busqueda.Trim().ToLower();
                    query = query.Where(h =>
                        h.Empleado.Nombre.ToLower().Contains(termino) ||
                        h.Empleado.PrimerApellido.ToLower().Contains(termino) ||
                        h.Empleado.Cedula.Contains(termino)
                    );
                }

                var horasExtras = await query
                    .OrderBy(h => h.Empleado.PrimerApellido)
                    .ThenBy(h => h.Empleado.Nombre)
                    .ToListAsync();

                // KPIs
                ViewBag.TotalRegistros = horasExtras.Count;
                ViewBag.TotalMonto = horasExtras.Sum(h => h.MontoTotal);
                ViewBag.TotalHoras = horasExtras.Sum(h => h.TotalHoras);
                ViewBag.PeriodoActual = periodos.FirstOrDefault(p => p.PeriodoPagoId == periodoId);

                return View(horasExtras);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar horas extras. PeriodoId: {P}", periodoId);
                TempData["Error"] = "Ocurrió un error al cargar las horas extras. Intentá de nuevo.";
                return View(new List<HorasExtras>());
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public async Task<IActionResult> Create(int? periodoId)
        {
            try
            {
                await CargarViewBags(periodoId);
                return View(new HorasExtras
                {
                    PeriodoPagoId = periodoId ?? 0,
                    Porcentaje = 1.5m
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de horas extras");
                TempData["Error"] = "Error al cargar el formulario. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HorasExtras model)
        {
            try
            {
                ModelState.Remove("Empleado");
                ModelState.Remove("PeriodoPago");
                AplicarValidaciones(model);

                if (!ModelState.IsValid)
                {
                    await CargarViewBags(model.PeriodoPagoId);
                    return View(model);
                }

                // Verificar período abierto
                var periodo = await _context.PeriodosPago.FindAsync(model.PeriodoPagoId);
                if (periodo == null || periodo.Estado == EstadoPeriodo.Cerrado)
                {
                    ModelState.AddModelError(string.Empty,
                        "No se pueden registrar horas extras en un período cerrado.");
                    await CargarViewBags(model.PeriodoPagoId);
                    return View(model);
                }

                // Verificar si ya existe registro para este empleado en este período
                var existe = await _context.HorasExtras.AnyAsync(h =>
                    h.EmpleadoId == model.EmpleadoId &&
                    h.PeriodoPagoId == model.PeriodoPagoId);

                if (existe)
                {
                    ModelState.AddModelError(string.Empty,
                        "Ya existe un registro de horas extras para este empleado en el período seleccionado. Editá el registro existente.");
                    await CargarViewBags(model.PeriodoPagoId);
                    return View(model);
                }

                // Obtener valor hora del empleado
                var empleado = await _context.Empleados.FindAsync(model.EmpleadoId);
                if (empleado == null)
                {
                    ModelState.AddModelError("EmpleadoId", "Empleado no encontrado.");
                    await CargarViewBags(model.PeriodoPagoId);
                    return View(model);
                }

                model.ValorHora = empleado.ValorHora;
                model.MontoTotal = Math.Round(model.TotalHoras * model.ValorHora * model.Porcentaje, 2);

                _context.Add(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Horas extras registradas: EmpleadoId {EId} PeriodoId {PId} Monto {M}",
                    model.EmpleadoId, model.PeriodoPagoId, model.MontoTotal);
                TempData["Success"] = $"Horas extras de ₡{model.MontoTotal:N0} registradas correctamente.";
                return RedirectToAction(nameof(Index), new { periodoId = model.PeriodoPagoId });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al registrar horas extras");
                ModelState.AddModelError(string.Empty, "Error al guardar. Intentá de nuevo.");
                await CargarViewBags(model.PeriodoPagoId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al registrar horas extras");
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                await CargarViewBags(model.PeriodoPagoId);
                return View(model);
            }
        }

        // ── EDIT ──────────────────────────────────────────────────────────────

        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null || id <= 0) return NotFound();

                var horasExtras = await _context.HorasExtras
                    .Include(h => h.PeriodoPago)
                    .FirstOrDefaultAsync(h => h.HorasExtrasId == id);

                if (horasExtras == null) return NotFound();

                if (horasExtras.PeriodoPago.Estado == EstadoPeriodo.Cerrado)
                {
                    TempData["Error"] = "No se pueden editar horas extras de un período cerrado.";
                    return RedirectToAction(nameof(Index),
                        new { periodoId = horasExtras.PeriodoPagoId });
                }

                await CargarViewBags(horasExtras.PeriodoPagoId, horasExtras.EmpleadoId);
                return View(horasExtras);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar edición de horas extras ID: {Id}", id);
                TempData["Error"] = "Error al cargar el formulario de edición.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HorasExtras model)
        {
            try
            {
                if (id != model.HorasExtrasId) return NotFound();

                ModelState.Remove("Empleado");
                ModelState.Remove("PeriodoPago");
                AplicarValidaciones(model);

                if (!ModelState.IsValid)
                {
                    await CargarViewBags(model.PeriodoPagoId, model.EmpleadoId);
                    return View(model);
                }

                var registro = await _context.HorasExtras
                    .Include(h => h.PeriodoPago)
                    .Include(h => h.Empleado)
                    .FirstOrDefaultAsync(h => h.HorasExtrasId == id);

                if (registro == null) return NotFound();

                if (registro.PeriodoPago.Estado == EstadoPeriodo.Cerrado)
                {
                    TempData["Error"] = "No se pueden editar horas extras de un período cerrado.";
                    return RedirectToAction(nameof(Index),
                        new { periodoId = registro.PeriodoPagoId });
                }

                registro.TotalHoras = model.TotalHoras;
                registro.Porcentaje = model.Porcentaje;
                registro.ValorHora = registro.Empleado.ValorHora;
                registro.MontoTotal = Math.Round(
                    registro.TotalHoras * registro.ValorHora * registro.Porcentaje, 2);

                _context.Update(registro);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Horas extras editadas: ID {Id} Monto {M}",
                    id, registro.MontoTotal);
                TempData["Success"] = $"Horas extras actualizadas. Nuevo monto: ₡{registro.MontoTotal:N0}.";
                return RedirectToAction(nameof(Index),
                    new { periodoId = registro.PeriodoPagoId });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error de concurrencia al editar horas extras ID: {Id}", id);
                ModelState.AddModelError(string.Empty,
                    "El registro fue modificado por otro usuario. Recargá e intentá de nuevo.");
                await CargarViewBags(model.PeriodoPagoId, model.EmpleadoId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al editar horas extras ID: {Id}", id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                await CargarViewBags(model.PeriodoPagoId, model.EmpleadoId);
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
                var registro = await _context.HorasExtras
                    .Include(h => h.PeriodoPago)
                    .Include(h => h.Empleado)
                    .FirstOrDefaultAsync(h => h.HorasExtrasId == id);

                if (registro == null)
                {
                    TempData["Error"] = "Registro no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if (registro.PeriodoPago.Estado == EstadoPeriodo.Cerrado)
                {
                    TempData["Error"] = "No se pueden eliminar horas extras de un período cerrado.";
                    return RedirectToAction(nameof(Index),
                        new { periodoId = registro.PeriodoPagoId });
                }

                var periodoId = registro.PeriodoPagoId;
                _context.HorasExtras.Remove(registro);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Horas extras eliminadas: ID {Id} Empleado {E}",
                    id, registro.Empleado.PrimerApellido);
                TempData["Success"] = $"Registro de horas extras de {registro.Empleado.PrimerApellido} {registro.Empleado.Nombre} eliminado.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar horas extras ID: {Id}", id);
                TempData["Error"] = "Error al eliminar el registro. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── API: calcular monto ───────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> CalcularMonto(int empleadoId, decimal horas, decimal porcentaje)
        {
            try
            {
                if (empleadoId <= 0 || horas <= 0 || porcentaje <= 0)
                    return Json(new { monto = 0m, valorHora = 0m });

                var empleado = await _context.Empleados
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId);

                if (empleado == null)
                    return Json(new { monto = 0m, valorHora = 0m });

                var valorHora = empleado.ValorHora;
                var monto = Math.Round(horas * valorHora * porcentaje, 2);

                return Json(new { monto, valorHora });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular monto horas extras");
                return Json(new { monto = 0m, valorHora = 0m });
            }
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private async Task CargarViewBags(int? periodoId = null, int? empleadoId = null)
        {
            ViewBag.Periodos = new SelectList(
                await _context.PeriodosPago
                    .AsNoTracking()
                    .Where(p => p.Estado == EstadoPeriodo.Abierto)
                    .OrderByDescending(p => p.Anio)
                    .ThenByDescending(p => p.Mes)
                    .ThenByDescending(p => p.Quincena)
                    .ToListAsync(),
                "PeriodoPagoId", "Descripcion", periodoId);

            ViewBag.Empleados = await _context.Empleados
                .Where(e => e.Activo)
                .OrderBy(e => e.PrimerApellido)
                .ThenBy(e => e.Nombre)
                .Select(e => new SelectListItem
                {
                    Value = e.EmpleadoId.ToString(),
                    Text = $"{e.PrimerApellido} {e.SegundoApellido} {e.Nombre} — ₡{e.SalarioBase / (e.TipoJornada == TipoJornada.Completa ? 240 : 120):N2}/hr",
                    Selected = e.EmpleadoId == empleadoId
                })
                .ToListAsync();
        }

        private void AplicarValidaciones(HorasExtras model)
        {
            if (model.EmpleadoId <= 0)
                ModelState.AddModelError("EmpleadoId", "Seleccioná un empleado válido.");

            if (model.PeriodoPagoId <= 0)
                ModelState.AddModelError("PeriodoPagoId", "Seleccioná un período válido.");

            if (model.TotalHoras <= 0)
                ModelState.AddModelError("TotalHoras", "Las horas deben ser mayor a cero.");
            else if (model.TotalHoras > 999.99m)
                ModelState.AddModelError("TotalHoras",
                    "El total de horas no puede superar 999.99.");

            if (model.Porcentaje <= 0)
                ModelState.AddModelError("Porcentaje", "El porcentaje debe ser mayor a cero.");
            else if (model.Porcentaje > 3)
                ModelState.AddModelError("Porcentaje",
                    "El porcentaje no puede superar 3 (300%).");
        }

        // ── DESCARGAR PDF ─────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> DescargarPDF(int id)
        {
            try
            {
                var registro = await _context.HorasExtras
                    .Include(h => h.Empleado)
                    .Include(h => h.PeriodoPago)
                    .FirstOrDefaultAsync(h => h.HorasExtrasId == id);

                if (registro == null) return NotFound();

                var pdfBytes = _servicioPDF.GenerarPDFHorasExtras(registro);
                var nombreArchivo = $"HrsExtras_{registro.Empleado.PrimerApellido}_" +
                    $"{registro.PeriodoPago.Descripcion.Replace(" ", "_").Replace("—", "")}.pdf";

                return File(pdfBytes, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF horas extras ID: {Id}", id);
                TempData["Error"] = "Error al generar el PDF. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}