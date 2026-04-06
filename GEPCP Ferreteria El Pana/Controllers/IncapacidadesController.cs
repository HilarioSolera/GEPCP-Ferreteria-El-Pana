using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Migrations;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class IncapacidadesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IncapacidadesController> _logger;
        private readonly ComprobantePlanillaService _servicioPDF;
        private readonly ReglasNegocioConfig _reglas;
        private readonly AuditoriaService _auditoria;

        public IncapacidadesController(
            ApplicationDbContext context,
            ILogger<IncapacidadesController> logger,
            ComprobantePlanillaService servicioPDF,
            IOptions<ReglasNegocioConfig> reglas,
            AuditoriaService auditoria)
        {
            _context = context;
            _logger = logger;
            _servicioPDF = servicioPDF;
            _reglas = reglas.Value;
            _auditoria = auditoria;
        }

        // ── INDEX ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(string? busqueda, string? entidad)
        {
            try
            {
                ViewBag.Busqueda = busqueda;
                ViewBag.EntidadFiltro = entidad;
                ViewBag.TotalRegistros = 0;
                ViewBag.TotalDias = 0;
                ViewBag.TotalMonto = 0m;
                ViewBag.TotalPatrono = 0m;

                if (string.IsNullOrWhiteSpace(busqueda) && string.IsNullOrWhiteSpace(entidad))
                    return View(new List<Incapacidad>());

                var query = _context.Incapacidades
                    .Include(i => i.Empleado)
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(entidad) &&
                    Enum.TryParse<EntidadIncapacidad>(entidad, out var entidadEnum))
                    query = query.Where(i => i.Entidad == entidadEnum);

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var termino = busqueda.Trim().ToLower();
                    query = query.Where(i =>
                        i.Empleado.Nombre.ToLower().Contains(termino) ||
                        i.Empleado.PrimerApellido.ToLower().Contains(termino) ||
                        i.Empleado.Cedula.Contains(termino) ||
                        i.TipoIncapacidad.ToLower().Contains(termino) ||
                        (i.TiqueteCCSS != null && i.TiqueteCCSS.Contains(termino)));
                }

                var incapacidades = await query
                    .OrderByDescending(i => i.FechaInicio)
                    .ThenBy(i => i.Empleado.PrimerApellido)
                    .ToListAsync();

                ViewBag.TotalRegistros = incapacidades.Count;
                ViewBag.TotalDias = incapacidades.Sum(i => i.TotalDias);
                ViewBag.TotalMonto = incapacidades.Sum(i => i.MontoTotal);
                ViewBag.TotalPatrono = incapacidades
                    .Where(i => i.MontoTotal > 0)
                    .Sum(i => i.MontoTotal);

                return View(incapacidades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar incapacidades");
                TempData["Error"] = "Error al cargar las incapacidades.";
                return View(new List<Incapacidad>());
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public async Task<IActionResult> Create()
        {
            try
            {
                await CargarEmpleadosViewBag();
                return View(new Incapacidad
                {
                    FechaInicio = DateTime.Today,
                    FechaFin = DateTime.Today,
                    PorcentajePago = 50,
                    ResponsablePago = ResponsablePago.Patrono,
                    Entidad = EntidadIncapacidad.CCSS
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de incapacidad");
                TempData["Error"] = "Error al cargar el formulario. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Incapacidad model)
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

                var empleado = await _context.Empleados.FindAsync(model.EmpleadoId);
                if (empleado == null)
                {
                    ModelState.AddModelError("EmpleadoId", "Empleado no encontrado.");
                    await CargarEmpleadosViewBag();
                    return View(model);
                }

                var solapamiento = await _context.Incapacidades.AnyAsync(i =>
                    i.EmpleadoId == model.EmpleadoId &&
                    i.FechaInicio <= model.FechaFin &&
                    i.FechaFin >= model.FechaInicio);

                if (solapamiento)
                {
                    ModelState.AddModelError(string.Empty,
                        "Ya existe una incapacidad en ese período para este empleado.");
                    await CargarEmpleadosViewBag(model.EmpleadoId);
                    return View(model);
                }

                model.TotalDias = (model.FechaFin - model.FechaInicio).Days + 1;
                var divisor = _reglas.SalarioDivisorMensual > 0 ? _reglas.SalarioDivisorMensual : 30;
                var salarioDiario = Math.Round(empleado.SalarioBase / divisor, 2);
                model.MontoPorDia = Math.Round(salarioDiario * (model.PorcentajePago / 100m), 2);

                // CCSS: patrono paga primeros 3 días al 50%
                // INS:  patrono no paga nada, el INS cubre desde el día 1
                if (model.Entidad == EntidadIncapacidad.CCSS)
                {
                    model.DiasPagadosPatrono = Math.Min(model.TotalDias, 3);
                    model.MontoTotal = Math.Round(model.MontoPorDia * model.DiasPagadosPatrono, 2);
                    model.ResponsablePago = ResponsablePago.CCSS;
                }
                else // INS
                {
                    model.DiasPagadosPatrono = 0;
                    model.MontoTotal = 0m;
                    model.ResponsablePago = ResponsablePago.INS;
                }

                _context.Add(model);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Registrar incapacidad", "Incapacidades",
                    $"{empleado.PrimerApellido} {empleado.Nombre} — Días: {model.TotalDias} — " +
                    $"Días patrono: {model.DiasPagadosPatrono} — Monto: ₡{model.MontoTotal:N0}");

                _logger.LogInformation("Incapacidad registrada: EmpleadoId {EId} Días {D} Monto {M}",
                    model.EmpleadoId, model.TotalDias, model.MontoTotal);

                TempData["Success"] = $"Incapacidad de {model.TotalDias} día(s) registrada. " +
                    (model.Entidad == EntidadIncapacidad.CCSS
                        ? $"El patrono paga {model.DiasPagadosPatrono} día(s): ₡{model.MontoTotal:N0}."
                        : "El INS cubre el pago desde el día 1.");

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al registrar incapacidad");
                ModelState.AddModelError(string.Empty, "Error al guardar. Intentá de nuevo.");
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al registrar incapacidad");
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

                var incapacidad = await _context.Incapacidades
                    .Include(i => i.Empleado)
                    .FirstOrDefaultAsync(i => i.IncapacidadId == id);

                if (incapacidad == null) return NotFound();

                await CargarEmpleadosViewBag(incapacidad.EmpleadoId);
                return View(incapacidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar edición incapacidad ID: {Id}", id);
                TempData["Error"] = "Error al cargar el formulario de edición.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Incapacidad model)
        {
            try
            {
                if (id != model.IncapacidadId) return NotFound();

                ModelState.Remove("Empleado");
                AplicarValidaciones(model);

                if (!ModelState.IsValid)
                {
                    await CargarEmpleadosViewBag(model.EmpleadoId);
                    return View(model);
                }

                var registro = await _context.Incapacidades
                    .Include(i => i.Empleado)
                    .FirstOrDefaultAsync(i => i.IncapacidadId == id);

                if (registro == null) return NotFound();

                var solapamiento = await _context.Incapacidades.AnyAsync(i =>
                    i.EmpleadoId == model.EmpleadoId &&
                    i.IncapacidadId != model.IncapacidadId &&
                    i.FechaInicio <= model.FechaFin &&
                    i.FechaFin >= model.FechaInicio);

                if (solapamiento)
                {
                    ModelState.AddModelError(string.Empty,
                        "Ya existe una incapacidad en ese período para este empleado.");
                    await CargarEmpleadosViewBag(model.EmpleadoId);
                    return View(model);
                }

                registro.EmpleadoId = model.EmpleadoId;
                registro.Entidad = model.Entidad;
                registro.TipoIncapacidad = model.TipoIncapacidad;
                registro.FechaInicio = model.FechaInicio;
                registro.FechaFin = model.FechaFin;
                registro.TiqueteCCSS = model.TiqueteCCSS;
                registro.PorcentajePago = model.PorcentajePago;
                registro.Observaciones = model.Observaciones;
                registro.TotalDias = (registro.FechaFin - registro.FechaInicio).Days + 1;

                var empleado = await _context.Empleados.FindAsync(model.EmpleadoId);
                if (empleado != null)
                {
                    var divisor = _reglas.SalarioDivisorMensual > 0 ? _reglas.SalarioDivisorMensual : 30;
                    var salarioDiario = Math.Round(empleado.SalarioBase / divisor, 2);
                    registro.MontoPorDia = Math.Round(salarioDiario * (model.PorcentajePago / 100m), 2);

                    if (registro.Entidad == EntidadIncapacidad.CCSS)
                    {
                        registro.DiasPagadosPatrono = Math.Min(registro.TotalDias, 3);
                        registro.MontoTotal = Math.Round(registro.MontoPorDia * registro.DiasPagadosPatrono, 2);
                        registro.ResponsablePago = ResponsablePago.CCSS;
                    }
                    else
                    {
                        registro.DiasPagadosPatrono = 0;
                        registro.MontoTotal = 0m;
                        registro.ResponsablePago = ResponsablePago.INS;
                    }
                }

                _context.Update(registro);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Editar incapacidad", "Incapacidades",
                    $"{registro.Empleado.PrimerApellido} {registro.Empleado.Nombre} — " +
                    $"Días: {registro.TotalDias} — Monto patrono: ₡{registro.MontoTotal:N0}");

                _logger.LogInformation("Incapacidad editada: ID {Id}", id);
                TempData["Success"] = "Incapacidad actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error de concurrencia incapacidad ID: {Id}", id);
                ModelState.AddModelError(string.Empty,
                    "El registro fue modificado. Recargá e intentá de nuevo.");
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al editar incapacidad ID: {Id}", id);
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
                var registro = await _context.Incapacidades
                    .Include(i => i.Empleado)
                    .FirstOrDefaultAsync(i => i.IncapacidadId == id);

                if (registro == null)
                {
                    TempData["Error"] = "Registro no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Incapacidades.Remove(registro);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Eliminar incapacidad", "Incapacidades",
                    $"{registro.Empleado.PrimerApellido} {registro.Empleado.Nombre} — Días: {registro.TotalDias}");

                _logger.LogInformation("Incapacidad eliminada: ID {Id}", id);
                TempData["Success"] = $"Incapacidad de {registro.Empleado.PrimerApellido} " +
                    $"{registro.Empleado.Nombre} eliminada.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar incapacidad ID: {Id}", id);
                TempData["Error"] = "Error al eliminar el registro. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── API ───────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> CalcularMonto(
            int empleadoId, string fechaInicio, string fechaFin,
            decimal porcentaje, string entidad = "CCSS")
        {
            try
            {
                if (empleadoId <= 0 || porcentaje <= 0)
                    return Json(new
                    {
                        montoPorDia = 0m,
                        montoTotal = 0m,
                        totalDias = 0,
                        salarioDiario = 0m,
                        diasPatrono = 0
                    });

                if (!DateTime.TryParse(fechaInicio, out var fi) ||
                    !DateTime.TryParse(fechaFin, out var ff))
                    return Json(new
                    {
                        montoPorDia = 0m,
                        montoTotal = 0m,
                        totalDias = 0,
                        salarioDiario = 0m,
                        diasPatrono = 0
                    });

                var totalDias = (ff - fi).Days + 1;
                if (totalDias <= 0)
                    return Json(new
                    {
                        montoPorDia = 0m,
                        montoTotal = 0m,
                        totalDias = 0,
                        salarioDiario = 0m,
                        diasPatrono = 0
                    });

                var empleado = await _context.Empleados
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId);

                if (empleado == null)
                    return Json(new
                    {
                        montoPorDia = 0m,
                        montoTotal = 0m,
                        totalDias = 0,
                        salarioDiario = 0m,
                        diasPatrono = 0
                    });

                var divisor = _reglas.SalarioDivisorMensual > 0 ? _reglas.SalarioDivisorMensual : 30;
                var salarioDiario = Math.Round(empleado.SalarioBase / divisor, 2);
                var montoPorDia = Math.Round(salarioDiario * (porcentaje / 100m), 2);

                int diasPatrono;
                decimal montoTotal;

                if (entidad == "CCSS")
                {
                    diasPatrono = Math.Min(totalDias, 3);
                    montoTotal = Math.Round(montoPorDia * diasPatrono, 2);
                }
                else // INS
                {
                    diasPatrono = 0;
                    montoTotal = 0m;
                }

                return Json(new { montoPorDia, montoTotal, totalDias, salarioDiario, diasPatrono });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular monto incapacidad");
                return Json(new
                {
                    montoPorDia = 0m,
                    montoTotal = 0m,
                    totalDias = 0,
                    salarioDiario = 0m,
                    diasPatrono = 0
                });
            }
        }

        // ── DESCARGAR PDF ─────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> DescargarPDF(int id)
        {
            try
            {
                var registro = await _context.Incapacidades
                    .Include(i => i.Empleado)
                    .FirstOrDefaultAsync(i => i.IncapacidadId == id);

                if (registro == null) return NotFound();

                var pdfBytes = _servicioPDF.GenerarPDFIncapacidad(registro);
                var nombreArchivo = $"Incapacidad_{registro.Empleado.PrimerApellido}_" +
                    $"{registro.FechaInicio:ddMMyyyy}_al_{registro.FechaFin:ddMMyyyy}.pdf";

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Descargar PDF incapacidad", "Incapacidades",
                    $"{registro.Empleado.PrimerApellido} {registro.Empleado.Nombre}");

                return File(pdfBytes, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF incapacidad ID: {Id}", id);
                TempData["Error"] = "Error al generar el PDF. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private async Task CargarEmpleadosViewBag(int? selectedId = null)
        {
            ViewBag.Empleados = await _context.Empleados
                .Where(e => e.Activo)
                .OrderBy(e => e.PrimerApellido).ThenBy(e => e.Nombre)
                .Select(e => new SelectListItem
                {
                    Value = e.EmpleadoId.ToString(),
                    Text = $"{e.PrimerApellido} {e.SegundoApellido} {e.Nombre} — {e.Cedula}",
                    Selected = e.EmpleadoId == selectedId
                })
                .ToListAsync();
        }

        private void AplicarValidaciones(Incapacidad model)
        {
            if (model.EmpleadoId <= 0)
                ModelState.AddModelError("EmpleadoId", "Seleccioná un empleado válido.");

            if (string.IsNullOrWhiteSpace(model.TipoIncapacidad))
                ModelState.AddModelError("TipoIncapacidad", "El tipo de incapacidad es obligatorio.");

            if (model.FechaInicio == default)
                ModelState.AddModelError("FechaInicio", "La fecha de inicio es obligatoria.");

            if (model.FechaFin == default)
                ModelState.AddModelError("FechaFin", "La fecha de fin es obligatoria.");

            if (model.FechaInicio != default && model.FechaFin != default)
            {
                if (model.FechaFin < model.FechaInicio)
                    ModelState.AddModelError("FechaFin",
                        "La fecha de fin no puede ser anterior a la fecha de inicio.");
                if (model.FechaInicio > DateTime.Today.AddDays(1))
                    ModelState.AddModelError("FechaInicio",
                        "La fecha de inicio no puede ser futura.");
            }

            if (model.PorcentajePago <= 0 || model.PorcentajePago > 100)
                ModelState.AddModelError("PorcentajePago", "El porcentaje debe estar entre 1 y 100.");

            if (model.Entidad == EntidadIncapacidad.CCSS &&
                string.IsNullOrWhiteSpace(model.TiqueteCCSS))
                ModelState.AddModelError("TiqueteCCSS",
                    "El tiquete CCSS es obligatorio para incapacidades de la CCSS.");
        }
    }
}
