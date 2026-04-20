using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Services;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class PeriodosPagoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PeriodosPagoController> _logger;
        private readonly AuditoriaService _auditoria;

        private static readonly string[] NombresMeses = {
            "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
            "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
        };

        public PeriodosPagoController(
            ApplicationDbContext context,
            ILogger<PeriodosPagoController> logger,
            AuditoriaService auditoria)
        {
            _context = context;
            _logger = logger;
            _auditoria = auditoria;
        }

        // INDEX

        public async Task<IActionResult> Index(int? anio)
        {
            try
            {
                anio ??= DateTime.Today.Year;
                ViewBag.AnioActual = anio;
                ViewBag.AnioAnterior = anio - 1;
                ViewBag.AnioSiguiente = anio + 1;

                var periodos = await _context.PeriodosPago
                    .AsNoTracking()
                    .Where(p => p.Anio == anio)
                    .OrderByDescending(p => p.Mes)
                    .ThenByDescending(p => p.Quincena)
                    .ToListAsync();

                var ids = periodos.Select(p => p.PeriodoPagoId).ToList();

                var planillasRaw = await _context.PlanillasEmpleado
                    .Where(pe => ids.Contains(pe.PeriodoPagoId))
                    .Select(pe => new { pe.PeriodoPagoId, pe.NetoAPagar })
                    .ToListAsync();

                var conteos = planillasRaw
                    .GroupBy(pe => pe.PeriodoPagoId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var netosPorPeriodo = planillasRaw
                    .GroupBy(pe => pe.PeriodoPagoId)
                    .ToDictionary(g => g.Key, g => g.Sum(pe => pe.NetoAPagar));

                ViewBag.ConteosEmpleados = conteos;
                ViewBag.NetosPorPeriodo = netosPorPeriodo;
                ViewBag.TotalPeriodos = periodos.Count;
                ViewBag.TotalAbiertos = periodos.Count(p => p.Estado == EstadoPeriodo.Abierto);
                ViewBag.TotalCerrados = periodos.Count(p => p.Estado == EstadoPeriodo.Cerrado);

                return View(periodos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar períodos de pago");
                TempData["Error"] = "Ocurrió un error al cargar los períodos. Intentá de nuevo.";
                return View(new List<PeriodoPago>());
            }
        }

        // CREATE GET

        public IActionResult Create()
        {
            try
            {
                var hoy = DateTime.Today;
                var quincena = hoy.Day <= 15
                    ? NumeroQuincena.Primera
                    : NumeroQuincena.Segunda;

                DateTime fechaInicio, fechaFin;
                if (quincena == NumeroQuincena.Primera)
                {
                    fechaInicio = new DateTime(hoy.Year, hoy.Month, 1);
                    fechaFin = new DateTime(hoy.Year, hoy.Month, 15);
                }
                else
                {
                    fechaInicio = new DateTime(hoy.Year, hoy.Month, 16);
                    fechaFin = new DateTime(hoy.Year, hoy.Month,
                                      DateTime.DaysInMonth(hoy.Year, hoy.Month));
                }

                return View(new PeriodoPago
                {
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin,
                    Quincena = quincena,
                    Mes = hoy.Month,
                    Anio = hoy.Year,
                    TipoPeriodo = TipoPeriodo.Quincenal,
                    Estado = EstadoPeriodo.Abierto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de período");
                TempData["Error"] = "Error al cargar el formulario. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // CREATE POST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PeriodoPago model)
        {
            try
            {
                var hoy = DateTime.Today;
                var mesActual = hoy.Month;
                var anioActual = hoy.Year;

                // Restricción: solo ±1 mes del actual
                // Para períodos semanales se permite más holgura (±2 semanas)
                // pero mantenemos la misma regla por simplicidad.
                bool esMesPermitido =
                    (model.Anio == anioActual &&
                     model.Mes >= mesActual - 1 &&
                     model.Mes <= mesActual + 1) ||
                    (model.Anio == anioActual - 1 &&
                     model.Mes == 12 && mesActual == 1) ||
                    (model.Anio == anioActual + 1 &&
                     model.Mes == 1 && mesActual == 12);

                if (!esMesPermitido)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Solo podés crear períodos del mes anterior, actual o siguiente. " +
                        $"Intentaste crear: {NombresMeses[model.Mes]} {model.Anio}.");
                    return View(model);
                }

                // Validaciones básicas
                if (model.Mes < 1 || model.Mes > 12)
                    ModelState.AddModelError("Mes", "El mes debe estar entre 1 y 12.");

                if (model.Anio < 2020 || model.Anio > 2099)
                    ModelState.AddModelError("Anio", "El año debe estar entre 2020 y 2099.");

                if (model.FechaInicio == default)
                    ModelState.AddModelError("FechaInicio", "La fecha de inicio es obligatoria.");

                if (model.FechaFin == default)
                    ModelState.AddModelError("FechaFin", "La fecha de fin es obligatoria.");

                if (model.FechaInicio != default && model.FechaFin != default)
                {
                    if (model.FechaFin < model.FechaInicio)
                        ModelState.AddModelError("FechaFin",
                            "La fecha de fin no puede ser anterior a la de inicio.");

                    if (model.FechaFin == model.FechaInicio)
                        ModelState.AddModelError("FechaFin",
                            "La fecha de fin no puede ser igual a la de inicio.");

                    // Validar duración coherente con el tipo de período
                    var dias = (model.FechaFin - model.FechaInicio).TotalDays + 1;
                    switch (model.TipoPeriodo)
                    {
                        case TipoPeriodo.Semanal when dias > 9:
                            ModelState.AddModelError("FechaFin",
                                "Un período semanal no debería superar 9 días.");
                            break;
                        case TipoPeriodo.Quincenal when dias > 17:
                            ModelState.AddModelError("FechaFin",
                                "Un período quincenal no debería superar 17 días.");
                            break;
                        case TipoPeriodo.Mensual when dias > 32:
                            ModelState.AddModelError("FechaFin",
                                "Un período mensual no debería superar 32 días.");
                            break;
                    }
                }

                if (!ModelState.IsValid) return View(model);

                // Validación: duplicado (mismo mes/quincena/año/tipo)
                var existeDuplicado = await _context.PeriodosPago.AnyAsync(p =>
                    p.Anio == model.Anio &&
                    p.Mes == model.Mes &&
                    p.Quincena == model.Quincena &&
                    p.TipoPeriodo == model.TipoPeriodo);

                if (existeDuplicado)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Ya existe un período {model.TipoPeriodo} para " +
                        $"{NombresMeses[model.Mes]} {model.Anio} — " +
                        $"Quincena {(int)model.Quincena}.");
                    return View(model);
                }

                // Validación: solapamiento de fechas del mismo tipo
                var solapamiento = await _context.PeriodosPago.AnyAsync(p =>
                    p.TipoPeriodo == model.TipoPeriodo &&
                    p.FechaInicio <= model.FechaFin &&
                    p.FechaFin >= model.FechaInicio);

                if (solapamiento)
                {
                    var conflicto = await _context.PeriodosPago
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p =>
                            p.TipoPeriodo == model.TipoPeriodo &&
                            p.FechaInicio <= model.FechaFin &&
                            p.FechaFin >= model.FechaInicio);

                    ModelState.AddModelError(string.Empty,
                        $"Las fechas se solapan con el período existente: " +
                        $"\"{conflicto?.Descripcion}\" " +
                        $"({conflicto?.FechaInicio:dd/MM/yyyy} — " +
                        $"{conflicto?.FechaFin:dd/MM/yyyy}). " +
                        "Ajustá las fechas antes de continuar.");
                    return View(model);
                }

                // Guardar
                model.Estado = EstadoPeriodo.Abierto;
                _context.Add(model);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Crear período de pago", "Períodos",
                    $"{model.Descripcion} — Tipo: {model.TipoPeriodo}");

                TempData["Success"] =
                    $"Período {model.Descripcion} ({model.TipoPeriodo}) creado correctamente.";
                TempData["Recomendacion"] =
                    "Recordá calcular la planilla antes de cerrar este período.";

                return RedirectToAction(nameof(Index), new { anio = model.Anio });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al crear período");
                ModelState.AddModelError(string.Empty, "Error al guardar. Intentá de nuevo.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear período");
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                return View(model);
            }
        }

        // CERRAR

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cerrar(int id)
        {
            try
            {
                var periodo = await _context.PeriodosPago.FindAsync(id);
                if (periodo == null)
                {
                    TempData["Error"] = "Período no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if (periodo.Estado == EstadoPeriodo.Cerrado)
                {
                    TempData["Error"] = "Este período ya estaba cerrado.";
                    return RedirectToAction(nameof(Index), new { anio = periodo.Anio });
                }

                // Verificar si tiene planillas calculadas antes de cerrar
                var tienePlanillas = await _context.PlanillasEmpleado
                    .AnyAsync(pe => pe.PeriodoPagoId == id);

                if (!tienePlanillas)
                {
                    TempData["Warning"] = "Este período no tiene planillas calculadas. Se cerrará vacío.";
                }

                periodo.Estado = EstadoPeriodo.Cerrado;
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Cerrar período", "Períodos",
                    periodo.Descripcion);

                TempData["Success"] =
                    $"Período {periodo.Descripcion} cerrado. Los registros quedan inmutables.";

                return RedirectToAction(nameof(Index), new { anio = periodo.Anio });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar período ID: {Id}", id);
                TempData["Error"] = "Error al cerrar el período. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // REABRIR (solo Jefatura)

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize("Jefatura")]
        public async Task<IActionResult> Reabrir(int id)
        {
            try
            {
                var periodo = await _context.PeriodosPago.FindAsync(id);
                if (periodo == null)
                {
                    TempData["Error"] = "Período no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if (periodo.Estado == EstadoPeriodo.Abierto)
                {
                    TempData["Error"] = "Este período ya estaba abierto.";
                    return RedirectToAction(nameof(Index), new { anio = periodo.Anio });
                }

                var tienePlanillas = await _context.PlanillasEmpleado
                    .AnyAsync(pe => pe.PeriodoPagoId == id);

                periodo.Estado = EstadoPeriodo.Abierto;
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Reabrir período cerrado", "Períodos",
                    $"{periodo.Descripcion} — reabierto por Jefatura");

                if (tienePlanillas)
                    TempData["Warning"] =
                        $"Período {periodo.Descripcion} reabierto. " +
                        "IMPORTANTE: Los saldos de préstamos y créditos descontados al cerrar " +
                        "NO se revierten automáticamente desde aquí. " +
                        "Usá Reabrir desde el módulo de Planilla para revertir los saldos.";
                else
                    TempData["Success"] =
                        $"Período {periodo.Descripcion} reabierto correctamente.";

                return RedirectToAction(nameof(Index), new { anio = periodo.Anio });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reabrir período ID: {Id}", id);
                TempData["Error"] = "Error al reabrir el período. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ELIMINAR (solo Abiertos sin planilla)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                var periodo = await _context.PeriodosPago.FindAsync(id);
                if (periodo == null)
                {
                    TempData["Error"] = "Período no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if (periodo.Estado == EstadoPeriodo.Cerrado)
                {
                    TempData["Error"] =
                        "No se puede eliminar un período cerrado. " +
                        "Los registros de nómina deben conservarse por ley (mínimo 6 años).";
                    return RedirectToAction(nameof(Index), new { anio = periodo.Anio });
                }

                var tienePlanilla = await _context.PlanillasEmpleado
                    .AnyAsync(pe => pe.PeriodoPagoId == id);

                if (tienePlanilla)
                {
                    TempData["Error"] =
                        "No se puede eliminar un período que ya tiene planilla calculada. " +
                        "Eliminá primero la planilla desde el módulo de Planilla.";
                    return RedirectToAction(nameof(Index), new { anio = periodo.Anio });
                }

                var anio = periodo.Anio;
                var desc = periodo.Descripcion;

                _context.PeriodosPago.Remove(periodo);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Eliminar período abierto", "Períodos",
                    desc);

                TempData["Success"] = $"Período {desc} eliminado.";
                return RedirectToAction(nameof(Index), new { anio });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar período ID: {Id}", id);
                TempData["Error"] = "Error al eliminar el período. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}