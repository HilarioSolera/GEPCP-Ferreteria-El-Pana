using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class FeriadosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FeriadosController> _logger;
        private readonly AuditoriaService _auditoria;
        private readonly ComprobantePlanillaService _servicioPDF;
        private readonly EmailService _email;

        public FeriadosController(
            ApplicationDbContext context,
            ILogger<FeriadosController> logger,
            AuditoriaService auditoria,
            ComprobantePlanillaService servicioPDF,
            EmailService email)
        {
            _context = context;
            _logger = logger;
            _auditoria = auditoria;
            _servicioPDF = servicioPDF;
            _email = email;
        }

        public async Task<IActionResult> Index(int? anio)
        {
            try
            {
                anio ??= DateTime.Today.Year;
                ViewBag.AnioActual = anio;
                ViewBag.AnioAnterior = anio - 1;
                ViewBag.AnioSiguiente = anio + 1;

                var feriados = await _context.Feriados
                    .AsNoTracking()
                    .Where(f => f.Fecha.Year == anio)
                    .OrderBy(f => f.Fecha)
                    .ToListAsync();

                ViewBag.TotalFeriados = feriados.Count;
                ViewBag.TotalObligatorios = feriados.Count(f => f.Tipo == TipoFeriado.Obligatorio);
                ViewBag.TotalNoObligat = feriados.Count(f => f.Tipo == TipoFeriado.NoObligatorio);

                return View(feriados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar feriados. Año: {A}", anio);
                TempData["Error"] = "Ocurrió un error al cargar los feriados. Intentá de nuevo.";
                return View(new List<Feriado>());
            }
        }

        public IActionResult Create()
        {
            return View(new Feriado { Fecha = DateTime.Today, Tipo = TipoFeriado.Obligatorio });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Feriado model)
        {
            try
            {
                AplicarValidaciones(model, null);
                if (!ModelState.IsValid) return View(model);

                _context.Add(model);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Crear feriado", "Feriados",
                    $"{model.Nombre} — {model.Fecha:dd/MM/yyyy} — {model.Tipo}");

                _logger.LogInformation("Feriado creado: {Nombre} {Fecha}", model.Nombre, model.Fecha);
                TempData["Success"] = $"Feriado '{model.Nombre}' registrado correctamente.";
                return RedirectToAction(nameof(Index), new { anio = model.Fecha.Year });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al crear feriado");
                ModelState.AddModelError(string.Empty, "Error al guardar. Intentá de nuevo.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear feriado");
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null || id <= 0) return NotFound();
                var feriado = await _context.Feriados.FindAsync(id);
                if (feriado == null) return NotFound();
                return View(feriado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar edición de feriado ID: {Id}", id);
                TempData["Error"] = "Error al cargar el formulario de edición.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Feriado model)
        {
            try
            {
                if (id != model.FeriadoId) return NotFound();
                AplicarValidaciones(model, id);
                if (!ModelState.IsValid) return View(model);

                var feriado = await _context.Feriados.FindAsync(id);
                if (feriado == null) return NotFound();

                feriado.Fecha = model.Fecha;
                feriado.Nombre = model.Nombre.Trim();
                feriado.Tipo = model.Tipo;

                _context.Update(feriado);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Editar feriado", "Feriados",
                    $"{feriado.Nombre} — {feriado.Fecha:dd/MM/yyyy} — {feriado.Tipo}");

                _logger.LogInformation("Feriado editado: ID {Id} {Nombre}", id, feriado.Nombre);
                TempData["Success"] = $"Feriado '{feriado.Nombre}' actualizado correctamente.";
                return RedirectToAction(nameof(Index), new { anio = feriado.Fecha.Year });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error de concurrencia al editar feriado ID: {Id}", id);
                ModelState.AddModelError(string.Empty,
                    "El registro fue modificado. Recargá e intentá de nuevo.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al editar feriado ID: {Id}", id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var feriado = await _context.Feriados
                    .Include(f => f.PagosFeriado)
                    .FirstOrDefaultAsync(f => f.FeriadoId == id);

                if (feriado == null)
                {
                    TempData["Error"] = "Feriado no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if (feriado.PagosFeriado.Any())
                {
                    TempData["Error"] = $"No se puede eliminar '{feriado.Nombre}' porque tiene pagos en planillas.";
                    return RedirectToAction(nameof(Index), new { anio = feriado.Fecha.Year });
                }

                var anio = feriado.Fecha.Year;
                _context.Feriados.Remove(feriado);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Eliminar feriado", "Feriados",
                    $"{feriado.Nombre} — {feriado.Fecha:dd/MM/yyyy}");

                _logger.LogInformation("Feriado eliminado: ID {Id} {Nombre}", id, feriado.Nombre);
                TempData["Success"] = $"Feriado '{feriado.Nombre}' eliminado correctamente.";
                return RedirectToAction(nameof(Index), new { anio });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar feriado ID: {Id}", id);
                TempData["Error"] = "Error al eliminar el feriado. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        private void AplicarValidaciones(Feriado model, int? idActual)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre))
                ModelState.AddModelError("Nombre", "El nombre del feriado es obligatorio.");
            else if (model.Nombre.Trim().Length < 3)
                ModelState.AddModelError("Nombre", "El nombre debe tener al menos 3 caracteres.");

            if (model.Fecha == default)
                ModelState.AddModelError("Fecha", "La fecha es obligatoria.");
            else if (model.Fecha.Year < 2020 || model.Fecha.Year > 2099)
                ModelState.AddModelError("Fecha", "El año debe estar entre 2020 y 2099.");

            if (model.Fecha != default && _context.Feriados.Any(f =>
                    f.Fecha.Date == model.Fecha.Date &&
                    (idActual == null || f.FeriadoId != idActual)))
                ModelState.AddModelError("Fecha", "Ya existe un feriado registrado para esa fecha.");
        }

        // ── PAGOS DE FERIADO POR PERÍODO ──────────────────────────────────────────

        public async Task<IActionResult> PagosPorPeriodo(int? periodoId)
        {
            try
            {
                var periodos = await _context.PeriodosPago
                    .AsNoTracking()
                    .OrderByDescending(p => p.Anio)
                    .ThenByDescending(p => p.Mes)
                    .ThenByDescending(p => p.Quincena)
                    .ToListAsync();

                ViewBag.Periodos = periodos;
                ViewBag.PeriodoId = periodoId;

                if (periodoId == null)
                    return View(new List<PagoFeriado>());

                var periodo = periodos.FirstOrDefault(p => p.PeriodoPagoId == periodoId);
                if (periodo == null) return NotFound();
                ViewBag.Periodo = periodo;

                // Feriados obligatorios que caen dentro del período
                var feriadosEnPeriodo = await _context.Feriados
                    .AsNoTracking()
                    .Where(f =>
                        f.Tipo == TipoFeriado.Obligatorio &&
                        f.Fecha >= periodo.FechaInicio &&
                        f.Fecha <= periodo.FechaFin)
                    .ToListAsync();

                ViewBag.FeriadosEnPeriodo = feriadosEnPeriodo;

                var pagos = await _context.PagosFeriado
                    .Include(pf => pf.Empleado)
                    .Include(pf => pf.Feriado)
                    .AsNoTracking()
                    .Where(pf => pf.PeriodoPagoId == periodoId)
                    .OrderBy(pf => pf.Feriado.Fecha)
                    .ThenBy(pf => pf.Empleado.PrimerApellido)
                    .ToListAsync();

                return View(pagos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar pagos de feriado. PeriodoId: {P}", periodoId);
                TempData["Error"] = "Error al cargar los pagos de feriado.";
                return View(new List<PagoFeriado>());
            }
        }

        // ── GENERAR PAGOS AUTOMÁTICAMENTE ─────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarPagosFeriado(int periodoId)
        {
            try
            {
                var periodo = await _context.PeriodosPago.FindAsync(periodoId);
                if (periodo == null)
                {
                    TempData["Error"] = "Período no encontrado.";
                    return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
                }

                if (periodo.Estado == EstadoPeriodo.Cerrado)
                {
                    TempData["Error"] = "No se pueden modificar pagos de un período cerrado.";
                    return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
                }

                // Feriados obligatorios en el período
                var feriadosEnPeriodo = await _context.Feriados
                    .Where(f =>
                        f.Tipo == TipoFeriado.Obligatorio &&
                        f.Fecha >= periodo.FechaInicio &&
                        f.Fecha <= periodo.FechaFin)
                    .ToListAsync();

                if (!feriadosEnPeriodo.Any())
                {
                    TempData["Error"] = "No hay feriados obligatorios en este período.";
                    return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
                }

                var empleados = await _context.Empleados
                    .Where(e => e.Activo).ToListAsync();

                int generados = 0;

                foreach (var feriado in feriadosEnPeriodo)
                {
                    foreach (var empleado in empleados)
                    {
                        // Si ya existe, no se sobreescribe
                        var existe = await _context.PagosFeriado
                            .AnyAsync(pf =>
                                pf.PeriodoPagoId == periodoId &&
                                pf.FeriadoId == feriado.FeriadoId &&
                                pf.EmpleadoId == empleado.EmpleadoId);

                        if (existe) continue;

                        // Valor del día = salario mensual / 30
                        var valorDia = Math.Round(empleado.SalarioBase / 30m, 2);
                        // Por defecto trabajado = true → pago doble
                        var monto = Math.Round(valorDia * 2m, 2);

                        _context.PagosFeriado.Add(new PagoFeriado
                        {
                            EmpleadoId = empleado.EmpleadoId,
                            FeriadoId = feriado.FeriadoId,
                            PeriodoPagoId = periodoId,
                            Trabajado = true,
                            MontoTotal = monto
                        });

                        generados++;
                    }
                }

                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Generar pagos feriado", "Feriados",
                    $"PeriodoId: {periodoId} — {periodo.Descripcion} — Registros: {generados}");

                TempData["Success"] = generados > 0
                    ? $"Se generaron {generados} pagos de feriado. Revisá si algún empleado no trabajó."
                    : "Los pagos ya estaban generados.";

                return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar pagos feriado. PeriodoId: {P}", periodoId);
                TempData["Error"] = "Error al generar los pagos. Intentá de nuevo.";
                return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
            }
        }

        // ── MARCAR NO TRABAJADO ───────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarNoTrabajado(int pagoFeriadoId, int periodoId)
        {
            try
            {
                var pago = await _context.PagosFeriado
                    .Include(pf => pf.Empleado)
                    .Include(pf => pf.PeriodoPago)
                    .FirstOrDefaultAsync(pf => pf.PagoFeriadoId == pagoFeriadoId);

                if (pago == null)
                {
                    TempData["Error"] = "Registro no encontrado.";
                    return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
                }

                if (pago.PeriodoPago.Estado == EstadoPeriodo.Cerrado)
                {
                    TempData["Error"] = "No se puede modificar un período cerrado.";
                    return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
                }

                // Alternar entre trabajado/no trabajado
                pago.Trabajado = !pago.Trabajado;

                // Recalcular monto
                var valorDia = Math.Round(pago.Empleado.SalarioBase / 30m, 2);
                pago.MontoTotal = pago.Trabajado
                    ? Math.Round(valorDia * 2m, 2)  // Trabajó → doble
                    : Math.Round(valorDia * 1m, 2); // No trabajó → normal (ya cubierto en salario ordinario)

                // Si no trabajó el monto es 0 porque ya está incluido en el salario ordinario
                if (!pago.Trabajado) pago.MontoTotal = 0m;

                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    pago.Trabajado ? "Feriado marcado como trabajado" : "Feriado marcado como no trabajado",
                    "Feriados",
                    $"{pago.Empleado.PrimerApellido} {pago.Empleado.Nombre}");

                TempData["Success"] = pago.Trabajado
                    ? $"{pago.Empleado.PrimerApellido} marcado como trabajó el feriado — pago doble."
                    : $"{pago.Empleado.PrimerApellido} marcado como NO trabajó el feriado — pago normal (incluido en salario).";

                return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar feriado. ID: {Id}", pagoFeriadoId);
                TempData["Error"] = "Error al actualizar. Intentá de nuevo.";
                return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
            }
        }

        // ── ELIMINAR PAGO FERIADO ─────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarPago(int pagoFeriadoId, int periodoId)
        {
            try
            {
                var pago = await _context.PagosFeriado
                    .Include(pf => pf.PeriodoPago)
                    .FirstOrDefaultAsync(pf => pf.PagoFeriadoId == pagoFeriadoId);

                if (pago == null)
                {
                    TempData["Error"] = "Registro no encontrado.";
                    return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
                }

                if (pago.PeriodoPago.Estado == EstadoPeriodo.Cerrado)
                {
                    TempData["Error"] = "No se puede eliminar de un período cerrado.";
                    return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
                }

                _context.PagosFeriado.Remove(pago);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Eliminar pago feriado", "Feriados",
                    $"PagoFeriadoId: {pagoFeriadoId} — PeriodoId: {periodoId}");

                TempData["Success"] = "Pago de feriado eliminado.";
                return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar pago feriado ID: {Id}", pagoFeriadoId);
                TempData["Error"] = "Error al eliminar. Intentá de nuevo.";
                return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
            }
        }

        // ── DESCARGAR BOLETA PDF ───────────────────────────────────────────────

        public async Task<IActionResult> DescargarBoletaFeriado(int id)
        {
            try
            {
                var pago = await _context.PagosFeriado
                    .Include(pf => pf.Empleado)
                    .Include(pf => pf.Feriado)
                    .Include(pf => pf.PeriodoPago)
                    .FirstOrDefaultAsync(pf => pf.PagoFeriadoId == id);

                if (pago == null)
                {
                    TempData["Error"] = "Pago de feriado no encontrado.";
                    return RedirectToAction(nameof(PagosPorPeriodo));
                }

                var pdf = _servicioPDF.GenerarPDFFeriado(pago);
                return File(pdf, "application/pdf",
                    $"Feriado_{pago.Empleado.PrimerApellido}_{pago.Feriado.Nombre}_{pago.Feriado.Fecha:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF de feriado ID: {Id}", id);
                TempData["Error"] = "Error al generar el PDF.";
                return RedirectToAction(nameof(PagosPorPeriodo));
            }
        }

        // ── ENVIAR POR EMAIL ──────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarPorEmail(int id, int periodoId)
        {
            try
            {
                var pago = await _context.PagosFeriado
                    .Include(pf => pf.Empleado)
                    .Include(pf => pf.Feriado)
                    .Include(pf => pf.PeriodoPago)
                    .FirstOrDefaultAsync(pf => pf.PagoFeriadoId == id);

                if (pago == null)
                {
                    TempData["Error"] = "Pago de feriado no encontrado.";
                    return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
                }

                var correo = pago.Empleado.CorreoElectronico;
                if (string.IsNullOrWhiteSpace(correo))
                {
                    TempData["Error"] = "El empleado no tiene correo registrado.";
                    return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
                }

                var pdf = _servicioPDF.GenerarPDFFeriadoSinFirmas(pago);
                var asunto = $"Boleta de Feriado — {pago.Feriado.Nombre} — {pago.Feriado.Fecha:dd/MM/yyyy}";

                await _email.EnviarPDFAsync(
                    correo,
                    $"{pago.Empleado.Nombre} {pago.Empleado.PrimerApellido}",
                    asunto,
                    $"Adjunto su boleta de pago de feriado: {pago.Feriado.Nombre} ({pago.Feriado.Fecha:dd/MM/yyyy}).",
                    pdf,
                    $"Feriado_{pago.Empleado.PrimerApellido}_{pago.Feriado.Nombre}.pdf");

                TempData["Success"] = $"Boleta enviada a {correo}.";
                return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email feriado ID: {Id}", id);
                TempData["Error"] = "Error al enviar el correo. Intentá de nuevo.";
                return RedirectToAction(nameof(PagosPorPeriodo), new { periodoId });
            }
        }
    }
}