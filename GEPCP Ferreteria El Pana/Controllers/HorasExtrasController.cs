using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class HorasExtrasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HorasExtrasController> _logger;
        private readonly ComprobantePlanillaService _servicioPDF;
        private readonly AuditoriaService _auditoria;

        private const decimal LimiteHorasAdvertencia = 48m;

        public HorasExtrasController(
            ApplicationDbContext context,
            ILogger<HorasExtrasController> logger,
            ComprobantePlanillaService servicioPDF,
            AuditoriaService auditoria)
        {
            _context = context;
            _logger = logger;
            _servicioPDF = servicioPDF;
            _auditoria = auditoria;
        }

        // ── INDEX ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(int? periodoId, string? busqueda, bool verTodos = false)
        {
            try
            {
                ViewBag.Busqueda = busqueda;
                ViewBag.PeriodoId = periodoId;
                ViewBag.VerTodos = verTodos;

                var periodos = await _context.PeriodosPago
                    .AsNoTracking()
                    .OrderByDescending(p => p.Anio)
                    .ThenByDescending(p => p.Mes)
                    .ThenByDescending(p => p.Quincena)
                    .ToListAsync();

                ViewBag.Periodos = new SelectList(periodos, "PeriodoPagoId", "Descripcion", periodoId);

                // Si no hay filtros y no se pidió ver todos, pantalla vacía
                if (periodoId == null && string.IsNullOrWhiteSpace(busqueda) && !verTodos)
                    return View(new List<HorasExtras>());

                // Si verTodos, no filtrar por período
                if (!verTodos)
                {
                    if (periodoId == null)
                    {
                        var activo = periodos.FirstOrDefault(p => p.Estado == EstadoPeriodo.Abierto)
                                  ?? periodos.FirstOrDefault();
                        periodoId = activo?.PeriodoPagoId;
                        ViewBag.PeriodoId = periodoId;
                    }
                }

                var periodoActual = periodoId.HasValue
                    ? periodos.FirstOrDefault(p => p.PeriodoPagoId == periodoId.Value)
                    : null;

                ViewBag.PeriodoActual = verTodos ? null : periodoActual;
                ViewBag.PeriodoCerrado = periodoActual?.Estado == EstadoPeriodo.Cerrado;

                var query = _context.HorasExtras
                    .Include(h => h.Empleado)
                    .Include(h => h.PeriodoPago)
                    .AsNoTracking()
                    .AsQueryable();

                // Filtro por período solo si NO es verTodos
                if (!verTodos && periodoId.HasValue)
                    query = query.Where(h => h.PeriodoPagoId == periodoId.Value);

                // Filtro por búsqueda (aplica siempre)
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var termino = busqueda.Trim().ToLower();
                    query = query.Where(h =>
                        h.Empleado.Nombre.ToLower().Contains(termino) ||
                        h.Empleado.PrimerApellido.ToLower().Contains(termino) ||
                        h.Empleado.Cedula.Contains(termino));
                }

                var horasExtras = await query
                    .OrderByDescending(h => h.Fecha)
                    .ThenBy(h => h.Empleado.PrimerApellido)
                    .ToListAsync();

                ViewBag.TotalRegistros = horasExtras.Count;
                ViewBag.TotalHoras = horasExtras.Sum(h => h.TotalHoras);
                ViewBag.TotalMonto = horasExtras.Sum(h => h.MontoTotal);

                return View(horasExtras);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar horas extras");
                TempData["Error"] = "Error al cargar las horas extras.";
                return View(new List<HorasExtras>());
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public async Task<IActionResult> Create(int? periodoId)
        {
            try
            {
                await CargarViewBagPeriodos(periodoId);

                // Pasar rango del período a la vista para restringir el date picker
                if (periodoId.HasValue)
                {
                    var p = await _context.PeriodosPago.FindAsync(periodoId.Value);
                    if (p != null)
                    {
                        ViewBag.PeriodoFechaInicio = p.FechaInicio.ToString("yyyy-MM-dd");
                        ViewBag.PeriodoFechaFin = p.FechaFin.ToString("yyyy-MM-dd");
                        ViewBag.PeriodoDescripcion = p.Descripcion;
                    }
                }

                return View(new HorasExtras
                {
                    PeriodoPagoId = periodoId ?? 0,
                    Fecha = periodoId.HasValue
                                    ? (await _context.PeriodosPago.FindAsync(periodoId.Value))?.FechaInicio ?? DateTime.Today
                                    : DateTime.Today,
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
                    await CargarViewBagPeriodos(model.PeriodoPagoId);
                    return View(model);
                }

                var periodo = await _context.PeriodosPago.FindAsync(model.PeriodoPagoId);
                if (periodo == null || periodo.Estado == EstadoPeriodo.Cerrado)
                {
                    ModelState.AddModelError(string.Empty,
                        "No se pueden registrar horas extras en un período cerrado.");
                    await CargarViewBagPeriodos(model.PeriodoPagoId);
                    return View(model);
                }

                // ── Validar que la fecha esté dentro del rango del período ────
                if (model.Fecha != default &&
                    (model.Fecha < periodo.FechaInicio || model.Fecha > periodo.FechaFin))
                {
                    ModelState.AddModelError("Fecha",
                        $"La fecha debe estar dentro del período seleccionado: " +
                        $"{periodo.FechaInicio:dd/MM/yyyy} al {periodo.FechaFin:dd/MM/yyyy}.");
                    await CargarViewBagPeriodos(model.PeriodoPagoId);
                    return View(model);
                }

                // ── Validar duplicado ─────────────────────────────────────────
                var existe = await _context.HorasExtras.AnyAsync(h =>
                    h.EmpleadoId == model.EmpleadoId &&
                    h.PeriodoPagoId == model.PeriodoPagoId);

                if (existe)
                {
                    ModelState.AddModelError(string.Empty,
                        "Ya existe un registro de horas extras para este empleado en el período seleccionado. " +
                        "Editá el registro existente desde el listado.");
                    await CargarViewBagPeriodos(model.PeriodoPagoId);
                    return View(model);
                }

                var empleado = await _context.Empleados.FindAsync(model.EmpleadoId);
                if (empleado == null)
                {
                    ModelState.AddModelError("EmpleadoId", "Empleado no encontrado.");
                    await CargarViewBagPeriodos(model.PeriodoPagoId);
                    return View(model);
                }

                model.ValorHora = empleado.ValorHora;
                model.MontoTotal = Math.Round(model.TotalHoras * model.ValorHora * model.Porcentaje, 2);

                _context.Add(model);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Registrar horas extras", "Horas Extras",
                    $"{empleado.PrimerApellido} {empleado.Nombre} — Horas: {model.TotalHoras} — Monto: ₡{model.MontoTotal:N0}");

                _logger.LogInformation("Horas extras registradas: EmpleadoId {EId} Monto {M}",
                    model.EmpleadoId, model.MontoTotal);

                if (model.TotalHoras > LimiteHorasAdvertencia)
                    TempData["Warning"] = $"Horas extras registradas (₡{model.MontoTotal:N0}). " +
                        $"⚠️ El total de {model.TotalHoras} horas supera el límite recomendado de {LimiteHorasAdvertencia} hrs quincenales.";
                else
                    TempData["Success"] = $"Horas extras de ₡{model.MontoTotal:N0} registradas correctamente.";

                return RedirectToAction(nameof(Index), new { periodoId = model.PeriodoPagoId });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al registrar horas extras");
                ModelState.AddModelError(string.Empty, "Error al guardar. Intentá de nuevo.");
                await CargarViewBagPeriodos(model.PeriodoPagoId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al registrar horas extras");
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                await CargarViewBagPeriodos(model.PeriodoPagoId);
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
                    .Include(h => h.Empleado)
                    .FirstOrDefaultAsync(h => h.HorasExtrasId == id);

                if (horasExtras == null) return NotFound();

                if (horasExtras.PeriodoPago.Estado == EstadoPeriodo.Cerrado)
                {
                    TempData["Error"] = "No se pueden editar horas extras de un período cerrado.";
                    return RedirectToAction(nameof(Index),
                        new { periodoId = horasExtras.PeriodoPagoId });
                }

                await CargarViewBagPeriodos(horasExtras.PeriodoPagoId);

                ViewBag.EmpleadoNombre = $"{horasExtras.Empleado.PrimerApellido} " +
                    $"{horasExtras.Empleado.SegundoApellido} {horasExtras.Empleado.Nombre}".Trim();
                ViewBag.EmpleadoCedula = horasExtras.Empleado.Cedula;
                ViewBag.EmpleadoValorHora = horasExtras.Empleado.ValorHora;
                ViewBag.PeriodoFechaInicio = horasExtras.PeriodoPago.FechaInicio.ToString("yyyy-MM-dd");
                ViewBag.PeriodoFechaFin = horasExtras.PeriodoPago.FechaFin.ToString("yyyy-MM-dd");
                ViewBag.PeriodoDescripcion = horasExtras.PeriodoPago.Descripcion;

                return View(horasExtras);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar edición horas extras ID: {Id}", id);
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
                    await CargarViewBagPeriodos(model.PeriodoPagoId);
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

                // ── Validar que la fecha esté dentro del rango del período ────
                if (model.Fecha != default &&
                    (model.Fecha < registro.PeriodoPago.FechaInicio ||
                     model.Fecha > registro.PeriodoPago.FechaFin))
                {
                    ModelState.AddModelError("Fecha",
                        $"La fecha debe estar dentro del período: " +
                        $"{registro.PeriodoPago.FechaInicio:dd/MM/yyyy} al {registro.PeriodoPago.FechaFin:dd/MM/yyyy}.");
                    await CargarViewBagPeriodos(model.PeriodoPagoId);
                    ViewBag.EmpleadoNombre = $"{registro.Empleado.PrimerApellido} {registro.Empleado.Nombre}";
                    ViewBag.EmpleadoCedula = registro.Empleado.Cedula;
                    ViewBag.EmpleadoValorHora = registro.Empleado.ValorHora;
                    ViewBag.PeriodoFechaInicio = registro.PeriodoPago.FechaInicio.ToString("yyyy-MM-dd");
                    ViewBag.PeriodoFechaFin = registro.PeriodoPago.FechaFin.ToString("yyyy-MM-dd");
                    ViewBag.PeriodoDescripcion = registro.PeriodoPago.Descripcion;
                    return View(model);
                }

                // ── Capturar valores anteriores ANTES de modificar ────────────
                var fechaAnterior = registro.Fecha;
                var montoAnterior = registro.MontoTotal;
                var horasAnterior = registro.TotalHoras;
                var pctAnterior = registro.Porcentaje;

                // ── Aplicar cambios ───────────────────────────────────────────
                registro.TotalHoras = model.TotalHoras;
                registro.Porcentaje = model.Porcentaje;
                registro.Fecha = model.Fecha;
                registro.ValorHora = registro.Empleado.ValorHora;
                registro.MontoTotal = Math.Round(
                    registro.TotalHoras * registro.ValorHora * registro.Porcentaje, 2);

                _context.Update(registro);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Editar horas extras", "Horas Extras",
                    $"{registro.Empleado.PrimerApellido} {registro.Empleado.Nombre} — " +
                    $"Horas: {registro.TotalHoras} — Monto: ₡{registro.MontoTotal:N0}");

                _logger.LogInformation("Horas extras editadas: ID {Id} Monto {M}",
                    id, registro.MontoTotal);

                // ── Construir mensaje según qué cambió ────────────────────────
                var partes = new List<string>();

                bool cambioFecha = registro.Fecha != fechaAnterior;
                bool cambioMonto = registro.MontoTotal != montoAnterior;

                if (cambioFecha && !cambioMonto)
                {
                    partes.Add($"Fecha actualizada: {fechaAnterior:dd/MM/yyyy} → {registro.Fecha:dd/MM/yyyy}");
                }
                else if (cambioMonto && !cambioFecha)
                {
                    partes.Add($"Nuevo monto: ₡{registro.MontoTotal:N0} (anterior: ₡{montoAnterior:N0})");
                }
                else if (cambioFecha && cambioMonto)
                {
                    partes.Add($"Fecha: {fechaAnterior:dd/MM/yyyy} → {registro.Fecha:dd/MM/yyyy}");
                    partes.Add($"Nuevo monto: ₡{registro.MontoTotal:N0} (anterior: ₡{montoAnterior:N0})");
                }

                TempData["Success"] = partes.Any()
                    ? string.Join(" — ", partes)
                    : "Sin cambios detectados.";

                return RedirectToAction(nameof(Index), new { periodoId = registro.PeriodoPagoId });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error de concurrencia horas extras ID: {Id}", id);
                ModelState.AddModelError(string.Empty,
                    "El registro fue modificado. Recargá e intentá de nuevo.");
                await CargarViewBagPeriodos(model.PeriodoPagoId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al editar horas extras ID: {Id}", id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                await CargarViewBagPeriodos(model.PeriodoPagoId);
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

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Eliminar horas extras", "Horas Extras",
                    $"{registro.Empleado.PrimerApellido} {registro.Empleado.Nombre} — Monto: ₡{registro.MontoTotal:N0}");

                _logger.LogInformation("Horas extras eliminadas: ID {Id}", id);
                TempData["Success"] = $"Registro de {registro.Empleado.PrimerApellido} " +
                    $"{registro.Empleado.Nombre} eliminado.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar horas extras ID: {Id}", id);
                TempData["Error"] = "Error al eliminar el registro. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── API: Calcular monto ───────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> CalcularMonto(
            int empleadoId, decimal horas, decimal porcentaje)
        {
            try
            {
                if (empleadoId <= 0 || horas <= 0 || porcentaje <= 0)
                    return Json(new { monto = 0m, valorHora = 0m, advertencia = false });

                var empleado = await _context.Empleados
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId);

                if (empleado == null)
                    return Json(new { monto = 0m, valorHora = 0m, advertencia = false });

                var valorHora = empleado.ValorHora;
                var monto = Math.Round(horas * valorHora * porcentaje, 2);
                var advertencia = horas > LimiteHorasAdvertencia;

                return Json(new
                {
                    monto,
                    valorHora,
                    advertencia,
                    limiteHoras = LimiteHorasAdvertencia
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular monto horas extras");
                return Json(new { monto = 0m, valorHora = 0m, advertencia = false });
            }
        }

        // ── API: Buscar empleados ─────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BuscarEmpleados(string? termino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                    return Json(new List<object>());

                var t = termino.Trim().ToLower();

                var empleados = await _context.Empleados
                    .AsNoTracking()
                    .Where(e => e.Activo && (
                        e.Nombre.ToLower().Contains(t) ||
                        e.PrimerApellido.ToLower().Contains(t) ||
                        (e.SegundoApellido != null && e.SegundoApellido.ToLower().Contains(t)) ||
                        e.Cedula.Contains(t)))
                    .OrderBy(e => e.PrimerApellido)
                    .Take(10)
                    .Select(e => new
                    {
                        id = e.EmpleadoId,
                        nombre = $"{e.PrimerApellido} {e.SegundoApellido} {e.Nombre}".Trim(),
                        cedula = e.Cedula,
                        puesto = e.Puesto,
                        valorHora = e.SalarioBase /
                                    (e.TipoJornada == TipoJornada.Completa ? 240 : 120)
                    })
                    .ToListAsync();

                return Json(empleados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar empleados");
                return Json(new List<object>());
            }
        }

        // ── API: Todos los empleados ──────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> TodosLosEmpleados()
        {
            try
            {
                var empleados = await _context.Empleados
                    .AsNoTracking()
                    .Where(e => e.Activo)
                    .OrderBy(e => e.PrimerApellido)
                    .Select(e => new
                    {
                        id = e.EmpleadoId,
                        nombre = $"{e.PrimerApellido} {e.SegundoApellido} {e.Nombre}".Trim(),
                        cedula = e.Cedula,
                        puesto = e.Puesto,
                        valorHora = e.SalarioBase /
                                    (e.TipoJornada == TipoJornada.Completa ? 240 : 120)
                    })
                    .ToListAsync();

                return Json(empleados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar todos los empleados");
                return Json(new List<object>());
            }
        }

        // ── API: Rango del período ────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> ObtenerRangoPeriodo(int periodoId)
        {
            try
            {
                var p = await _context.PeriodosPago
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.PeriodoPagoId == periodoId);

                if (p == null)
                    return Json(new { ok = false });

                return Json(new
                {
                    ok = true,
                    fechaInicio = p.FechaInicio.ToString("yyyy-MM-dd"),
                    fechaFin = p.FechaFin.ToString("yyyy-MM-dd"),
                    descripcion = p.Descripcion
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener rango período");
                return Json(new { ok = false });
            }
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

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Descargar PDF horas extras", "Horas Extras",
                    $"{registro.Empleado.PrimerApellido} {registro.Empleado.Nombre}");

                return File(pdfBytes, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF horas extras ID: {Id}", id);
                TempData["Error"] = "Error al generar el PDF. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private async Task CargarViewBagPeriodos(int? periodoId = null)
        {
            var limite = DateTime.Today.AddMonths(-2);
            ViewBag.Periodos = new SelectList(
                await _context.PeriodosPago
                    .AsNoTracking()
                    .Where(p => p.Estado == EstadoPeriodo.Abierto &&
                                p.FechaInicio >= limite)
                    .OrderByDescending(p => p.Anio)
                    .ThenByDescending(p => p.Mes)
                    .ThenByDescending(p => p.Quincena)
                    .ToListAsync(),
                "PeriodoPagoId", "Descripcion", periodoId);
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
                ModelState.AddModelError("TotalHoras", "El total de horas no puede superar 999.99.");

            if (model.Porcentaje <= 0)
                ModelState.AddModelError("Porcentaje", "El porcentaje debe ser mayor a cero.");
            else if (model.Porcentaje > 3)
                ModelState.AddModelError("Porcentaje", "El porcentaje no puede superar 3 (300%).");

            if (model.Fecha == default)
                ModelState.AddModelError("Fecha", "La fecha es obligatoria.");
            else if (model.Fecha > DateTime.Today)
                ModelState.AddModelError("Fecha", "La fecha no puede ser futura.");
        }
    }
}