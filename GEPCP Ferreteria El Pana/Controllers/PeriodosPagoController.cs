using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Filters;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH")]
    public class PeriodosPagoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PeriodosPagoController> _logger;

        private static readonly string[] NombresMeses = {
            "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
            "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
        };

        public PeriodosPagoController(
            ApplicationDbContext context,
            ILogger<PeriodosPagoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── INDEX ─────────────────────────────────────────────────────────────

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

        // ── CREATE ────────────────────────────────────────────────────────────

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
                    fechaFin = new DateTime(
                        hoy.Year, hoy.Month,
                        DateTime.DaysInMonth(hoy.Year, hoy.Month));
                }

                return View(new PeriodoPago
                {
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin,
                    Quincena = quincena,
                    Mes = hoy.Month,
                    Anio = hoy.Year,
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PeriodoPago model)
        {
            try
            {
                AplicarValidaciones(model, null);

                if (!ModelState.IsValid)
                    return View(model);

                // Verificar que no exista ya ese período
                var existe = await _context.PeriodosPago.AnyAsync(p =>
                    p.Anio == model.Anio &&
                    p.Mes == model.Mes &&
                    p.Quincena == model.Quincena);

                if (existe)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Ya existe un período para {NombresMeses[model.Mes]} {model.Anio} — Quincena {(int)model.Quincena}.");
                    return View(model);
                }

                model.Estado = EstadoPeriodo.Abierto;
                _context.Add(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Período creado: {Mes}/{Anio} Q{Q}",
                    model.Mes, model.Anio, (int)model.Quincena);
                TempData["Success"] = $"Período {model.Descripcion} creado correctamente.";
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

        // ── CERRAR ────────────────────────────────────────────────────────────

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

                periodo.Estado = EstadoPeriodo.Cerrado;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Período cerrado: ID {Id} {Desc}", id, periodo.Descripcion);
                TempData["Success"] = $"Período {periodo.Descripcion} cerrado correctamente.";
                return RedirectToAction(nameof(Index), new { anio = periodo.Anio });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar período ID: {Id}", id);
                TempData["Error"] = "Error al cerrar el período. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── REABRIR ───────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
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

                periodo.Estado = EstadoPeriodo.Abierto;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Período reabierto: ID {Id} {Desc}", id, periodo.Descripcion);
                TempData["Success"] = $"Período {periodo.Descripcion} reabierto correctamente.";
                return RedirectToAction(nameof(Index), new { anio = periodo.Anio });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reabrir período ID: {Id}", id);
                TempData["Error"] = "Error al reabrir el período. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private void AplicarValidaciones(PeriodoPago model, int? idActual)
        {
            if (model.Mes < 1 || model.Mes > 12)
                ModelState.AddModelError("Mes", "El mes debe estar entre 1 y 12.");

            if (model.Anio < 2020 || model.Anio > 2099)
                ModelState.AddModelError("Anio", "El año debe estar entre 2020 y 2099.");

            if (model.FechaInicio == default)
                ModelState.AddModelError("FechaInicio", "La fecha de inicio es obligatoria.");

            if (model.FechaFin == default)
                ModelState.AddModelError("FechaFin", "La fecha de fin es obligatoria.");

            if (model.FechaInicio != default && model.FechaFin != default &&
                model.FechaFin < model.FechaInicio)
                ModelState.AddModelError("FechaFin",
                    "La fecha de fin no puede ser anterior a la fecha de inicio.");
        }
    }
}