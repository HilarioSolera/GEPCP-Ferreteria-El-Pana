using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Filters;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH")]
    public class PrestamosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PrestamosController> _logger;

        public PrestamosController(ApplicationDbContext context, ILogger<PrestamosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── INDEX ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(string? busqueda, string? estado)
        {
            try
            {
                ViewBag.Busqueda = busqueda;
                ViewBag.EstadoFiltro = estado;

                var query = _context.Prestamos
                    .Include(p => p.Empleado)
                    .AsNoTracking()
                    .AsQueryable();

                // Filtro por estado
                if (!string.IsNullOrWhiteSpace(estado))
                {
                    var activo = estado == "Activo";
                    query = query.Where(p => p.Activo == activo);
                }

                // Filtro por búsqueda
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var termino = busqueda.Trim().ToLower();
                    query = query.Where(p =>
                        p.Empleado.Nombre.ToLower().Contains(termino) ||
                        p.Empleado.PrimerApellido.ToLower().Contains(termino) ||
                        p.Empleado.Cedula.Contains(termino)
                    );
                }

                var prestamos = await query
                    .OrderByDescending(p => p.FechaPrestamo)
                    .ToListAsync();

                // Totales para el resumen
                ViewBag.TotalSaldo = prestamos.Where(p => p.Activo).Sum(p => p.Monto);
                ViewBag.TotalCuotas = prestamos.Where(p => p.Activo).Sum(p => p.CuotaMensual);
                ViewBag.TotalPrestamos = prestamos.Count;
                ViewBag.TotalActivos = prestamos.Count(p => p.Activo);

                return View(prestamos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar listado de préstamos. Búsqueda: {B} Estado: {E}", busqueda, estado);
                TempData["Error"] = "Ocurrió un error al cargar los préstamos. Intentá de nuevo.";
                return View(new List<Prestamo>());
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public async Task<IActionResult> Create()
        {
            try
            {
                await CargarEmpleadosViewBag();
                return View(new PrestamoViewModel { FechaPrestamo = DateTime.Today });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de préstamo");
                TempData["Error"] = "Error al cargar el formulario. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PrestamoViewModel model)
        {
            try
            {
                AplicarValidaciones(model);

                if (!ModelState.IsValid)
                {
                    await CargarEmpleadosViewBag(model.EmpleadoId);
                    return View(model);
                }

                // Verificar que el empleado no tenga ya un préstamo activo
                var tienePrestamoActivo = await _context.Prestamos
                    .AnyAsync(p => p.EmpleadoId == model.EmpleadoId && p.Activo);

                if (tienePrestamoActivo)
                {
                    ModelState.AddModelError(string.Empty,
                        "Este empleado ya tiene un préstamo activo. Debe saldarlo antes de otorgar uno nuevo.");
                    await CargarEmpleadosViewBag(model.EmpleadoId);
                    return View(model);
                }

                var prestamo = new Prestamo
                {
                    EmpleadoId = model.EmpleadoId,
                    Monto = Math.Round(model.MontoPrincipal, 2),
                    FechaPrestamo = model.FechaPrestamo,
                    Interes = 0,
                    Cuotas = model.CuotasTotal,
                    CuotaMensual = Math.Round(model.CuotaMensual, 2),
                    Activo = true
                };

                _context.Add(prestamo);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Préstamo creado: EmpleadoId {EId} Monto {M}", prestamo.EmpleadoId, prestamo.Monto);
                TempData["Success"] = $"Préstamo de ₡{prestamo.Monto:N0} registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al crear préstamo");
                ModelState.AddModelError(string.Empty, "Error al guardar en la base de datos. Intentá de nuevo.");
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear préstamo");
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }
        }

        // ── REGISTRAR ABONO ───────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAbono(int prestamoId, decimal monto)
        {
            try
            {
                // Validar monto del abono
                if (monto <= 0)
                {
                    TempData["Error"] = "El monto del abono debe ser mayor a cero.";
                    return RedirectToAction(nameof(Index));
                }

                if (monto > 9_999_999.99m)
                {
                    TempData["Error"] = "El monto del abono excede el límite permitido.";
                    return RedirectToAction(nameof(Index));
                }

                var prestamo = await _context.Prestamos
                    .Include(p => p.Empleado)
                    .FirstOrDefaultAsync(p => p.PrestamoId == prestamoId);

                if (prestamo == null)
                {
                    TempData["Error"] = "Préstamo no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if (!prestamo.Activo)
                {
                    TempData["Error"] = "Este préstamo ya está cerrado. No se pueden registrar abonos.";
                    return RedirectToAction(nameof(Index));
                }

                if (monto > prestamo.Monto)
                {
                    TempData["Error"] = $"El abono (₡{monto:N0}) no puede superar el saldo actual (₡{prestamo.Monto:N0}).";
                    return RedirectToAction(nameof(Index));
                }

                prestamo.Monto = Math.Round(prestamo.Monto - monto, 2);

                if (prestamo.Monto <= 0)
                {
                    prestamo.Monto = 0;
                    prestamo.Activo = false;
                    TempData["Success"] = $"Abono de ₡{monto:N0} registrado. ¡Préstamo de {prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre} saldado completamente!";
                }
                else
                {
                    TempData["Success"] = $"Abono de ₡{monto:N0} registrado. Saldo restante: ₡{prestamo.Monto:N0}.";
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Abono registrado: PrestamoId {PId} Monto {M} SaldoRestante {S}",
                    prestamoId, monto, prestamo.Monto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar abono. PrestamoId: {PId}", prestamoId);
                TempData["Error"] = "Ocurrió un error al registrar el abono. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── CERRAR MANUAL ─────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cerrar(int id)
        {
            try
            {
                var prestamo = await _context.Prestamos
                    .Include(p => p.Empleado)
                    .FirstOrDefaultAsync(p => p.PrestamoId == id);

                if (prestamo == null)
                {
                    TempData["Error"] = "Préstamo no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if (!prestamo.Activo)
                {
                    TempData["Error"] = "Este préstamo ya estaba cerrado.";
                    return RedirectToAction(nameof(Index));
                }

                prestamo.Activo = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Préstamo cerrado manualmente: ID {Id}", id);
                TempData["Success"] = $"Préstamo de {prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre} cerrado manualmente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar préstamo ID: {Id}", id);
                TempData["Error"] = "Error al cerrar el préstamo. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── REABRIR ───────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reabrir(int id)
        {
            try
            {
                var prestamo = await _context.Prestamos
                    .Include(p => p.Empleado)
                    .FirstOrDefaultAsync(p => p.PrestamoId == id);

                if (prestamo == null)
                {
                    TempData["Error"] = "Préstamo no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if (prestamo.Activo)
                {
                    TempData["Error"] = "Este préstamo ya está activo.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar que no tenga otro préstamo activo
                var tieneOtroActivo = await _context.Prestamos
                    .AnyAsync(p => p.EmpleadoId == prestamo.EmpleadoId
                                && p.Activo
                                && p.PrestamoId != id);

                if (tieneOtroActivo)
                {
                    TempData["Error"] = "El empleado ya tiene otro préstamo activo. No se puede reabrir este.";
                    return RedirectToAction(nameof(Index));
                }

                prestamo.Activo = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Préstamo reabierto: ID {Id}", id);
                TempData["Success"] = $"Préstamo de {prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre} reabierto correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reabrir préstamo ID: {Id}", id);
                TempData["Error"] = "Error al reabrir el préstamo. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── API: cuota sugerida ───────────────────────────────────────────────

        [HttpGet]
        public IActionResult CalcularCuota(decimal monto, int cuotas)
        {
            try
            {
                if (monto <= 0 || cuotas <= 0)
                    return Json(new { cuota = (decimal?)null });

                var cuota = Math.Round(monto / cuotas, 2);
                return Json(new { cuota });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular cuota");
                return Json(new { cuota = (decimal?)null });
            }
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
                    Text = $"{e.PrimerApellido} {e.SegundoApellido} {e.Nombre} — {e.Cedula}",
                    Selected = e.EmpleadoId == selectedId
                })
                .ToListAsync();
        }

        private void AplicarValidaciones(PrestamoViewModel model)
        {
            // Empleado
            if (model.EmpleadoId <= 0)
                ModelState.AddModelError("EmpleadoId", "Seleccioná un empleado válido.");

            // Monto
            if (model.MontoPrincipal <= 0)
                ModelState.AddModelError("MontoPrincipal", "El monto debe ser mayor a cero.");
            else if (model.MontoPrincipal > 9_999_999.99m)
                ModelState.AddModelError("MontoPrincipal", "El monto excede el límite máximo permitido (₡9,999,999.99).");

            // Cuotas
            if (model.CuotasTotal <= 0)
                ModelState.AddModelError("CuotasTotal", "El número de cuotas debe ser mayor a cero.");
            else if (model.CuotasTotal > 120)
                ModelState.AddModelError("CuotasTotal", "El número de cuotas no puede superar 120 (10 años).");

            // Cuota mensual
            if (model.CuotaMensual <= 0)
                ModelState.AddModelError("CuotaMensual", "La cuota mensual debe ser mayor a cero.");
            else if (model.CuotaMensual > model.MontoPrincipal && model.MontoPrincipal > 0)
                ModelState.AddModelError("CuotaMensual", "La cuota mensual no puede ser mayor al monto del préstamo.");

            // Fecha
            if (model.FechaPrestamo == default)
                ModelState.AddModelError("FechaPrestamo", "La fecha del préstamo es obligatoria.");
            else if (model.FechaPrestamo > DateTime.Today.AddDays(1))
                ModelState.AddModelError("FechaPrestamo", "La fecha no puede ser futura.");
            else if (model.FechaPrestamo < DateTime.Today.AddYears(-1))
                ModelState.AddModelError("FechaPrestamo", "La fecha no puede ser anterior a un año.");
        }
    }
}