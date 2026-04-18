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
    public class AguinaldoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AguinaldoController> _logger;
        private readonly ComprobantePlanillaService _servicioPDF;
        private readonly AuditoriaService _auditoria;

        public AguinaldoController(
            ApplicationDbContext context,
            ILogger<AguinaldoController> logger,
            ComprobantePlanillaService servicioPDF,
            AuditoriaService auditoria)
        {
            _context = context;
            _logger = logger;
            _servicioPDF = servicioPDF;
            _auditoria = auditoria;
        }

        // ── INDEX ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(int? anio, string? busqueda)
        {
            try
            {
                var anioActual = anio ?? DateTime.Today.Year;
                ViewBag.AnioActual = anioActual;
                ViewBag.Busqueda = busqueda;

                var anios = await _context.Aguinaldos
                    .Select(a => a.Anio).Distinct()
                    .OrderByDescending(a => a).ToListAsync();

                if (!anios.Contains(anioActual)) anios.Insert(0, anioActual);
                ViewBag.Anios = anios;

                if (anio == null && string.IsNullOrWhiteSpace(busqueda))
                {
                    ViewBag.TotalEmpleados = 0;
                    ViewBag.TotalMonto = 0m;
                    ViewBag.SinAguinaldo = 0;
                    return View(new List<Aguinaldo>());
                }

                var query = _context.Aguinaldos
                    .Include(a => a.Empleado)
                    .AsNoTracking()
                    .Where(a => a.Anio == anioActual);

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var t = busqueda.Trim().ToLower();
                    query = query.Where(a =>
                        a.Empleado.Nombre.ToLower().Contains(t) ||
                        a.Empleado.PrimerApellido.ToLower().Contains(t) ||
                        a.Empleado.Cedula.Contains(t));
                }

                var registros = await query
                    .OrderBy(a => a.Empleado.PrimerApellido)
                    .ThenBy(a => a.Empleado.Nombre)
                    .ToListAsync();

                ViewBag.TotalEmpleados = registros.Count;
                ViewBag.TotalMonto = registros.Sum(a => a.MontoTotal);

                var idsConAguinaldo = registros.Select(a => a.EmpleadoId).ToHashSet();
                ViewBag.SinAguinaldo = await _context.Empleados
                    .AsNoTracking()
                    .CountAsync(e => e.Activo && !idsConAguinaldo.Contains(e.EmpleadoId));

                return View(registros);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar aguinaldos");
                TempData["Error"] = "Error al cargar los aguinaldos.";
                ViewBag.TotalEmpleados = 0;
                ViewBag.TotalMonto = 0m;
                ViewBag.SinAguinaldo = 0;
                ViewBag.AnioActual = DateTime.Today.Year;
                ViewBag.Anios = new List<int> { DateTime.Today.Year };
                return View(new List<Aguinaldo>());
            }
        }

        // ── CALCULAR AGUINALDO ────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize("RRHH", "Jefatura")]
        public async Task<IActionResult> CalcularTodos(int anio)
        {
            try
            {
                var fechaInicio = new DateTime(anio - 1, 12, 1);
                var fechaFin = new DateTime(anio, 11, 30);

                var empleados = await _context.Empleados
                    .Where(e => e.Activo).ToListAsync();

                int calculados = 0;
                int actualizados = 0;

                foreach (var empleado in empleados)
                {
                    var sumaDevengados = (await _context.PlanillasEmpleado
                    .Include(pe => pe.PeriodoPago)
                    .Where(pe =>
                        pe.EmpleadoId == empleado.EmpleadoId &&
                        pe.PeriodoPago.Estado == EstadoPeriodo.Cerrado &&
                        pe.PeriodoPago.FechaInicio >= fechaInicio &&
                        pe.PeriodoPago.FechaFin <= fechaFin)
                    .Select(pe => pe.TotalDevengado)
                    .ToListAsync()).Sum();

                    if (sumaDevengados <= 0) continue;

                    var montoAguinaldo = Math.Round(sumaDevengados / 12m, 2);
                    var existente = await _context.Aguinaldos
                        .FirstOrDefaultAsync(a =>
                            a.EmpleadoId == empleado.EmpleadoId && a.Anio == anio);

                    if (existente != null)
                    {
                        existente.MontoTotal = montoAguinaldo;
                        existente.FechaInicio = fechaInicio;
                        existente.FechaFin = fechaFin;
                        actualizados++;
                    }
                    else
                    {
                        _context.Aguinaldos.Add(new Aguinaldo
                        {
                            EmpleadoId = empleado.EmpleadoId,
                            Anio = anio,
                            FechaInicio = fechaInicio,
                            FechaFin = fechaFin,
                            FechaPago = new DateTime(anio, 12, 20),
                            MontoTotal = montoAguinaldo,
                            CreadoEn = DateTime.Now
                        });
                        calculados++;
                    }
                }

                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Calcular aguinaldo", "Aguinaldo",
                    $"Año: {anio} — Nuevos: {calculados} Actualizados: {actualizados}");

                _logger.LogInformation("Aguinaldo calculado: Año {A} Nuevos {N} Actualizados {U}",
                    anio, calculados, actualizados);

                TempData["Success"] = $"Aguinaldo {anio} calculado: " +
                    $"{calculados} nuevos, {actualizados} actualizados.";

                return RedirectToAction(nameof(Index), new { anio });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular aguinaldos. Año: {A}", anio);
                TempData["Error"] = "Error al calcular el aguinaldo. Intentá de nuevo.";
                return RedirectToAction(nameof(Index), new { anio });
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        [CustomAuthorize("RRHH", "Jefatura")]
        public async Task<IActionResult> Create()
        {
            try
            {
                await CargarEmpleadosViewBag();
                return View(new Aguinaldo
                {
                    Anio = DateTime.Today.Year,
                    FechaInicio = new DateTime(DateTime.Today.Year - 1, 12, 1),
                    FechaFin = new DateTime(DateTime.Today.Year, 11, 30),
                    FechaPago = new DateTime(DateTime.Today.Year, 12, 20)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario aguinaldo");
                TempData["Error"] = "Error al cargar el formulario.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize("RRHH", "Jefatura")]
        public async Task<IActionResult> Create(Aguinaldo model)
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

                var existe = await _context.Aguinaldos
                    .AnyAsync(a => a.EmpleadoId == model.EmpleadoId && a.Anio == model.Anio);

                if (existe)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Este empleado ya tiene aguinaldo registrado para el año {model.Anio}.");
                    await CargarEmpleadosViewBag(model.EmpleadoId);
                    return View(model);
                }

                model.MontoTotal = Math.Round(model.MontoTotal, 2);
                model.CreadoEn = DateTime.Now;
                _context.Add(model);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Crear aguinaldo manual", "Aguinaldo",
                    $"EmpleadoId: {model.EmpleadoId} — Año: {model.Anio} — Monto: ₡{model.MontoTotal:N0}");

                _logger.LogInformation("Aguinaldo creado manualmente: EmpleadoId {E} Año {A}",
                    model.EmpleadoId, model.Anio);

                TempData["Success"] = $"Aguinaldo de ₡{model.MontoTotal:N0} registrado.";
                return RedirectToAction(nameof(Index), new { anio = model.Anio });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al crear aguinaldo");
                ModelState.AddModelError(string.Empty,
                    "Error al guardar. Verificá que no exista un registro duplicado.");
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear aguinaldo");
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado.");
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }
        }

        // ── EDIT ──────────────────────────────────────────────────────────────

        [CustomAuthorize("RRHH", "Jefatura")]
        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null || id <= 0)
                    return NotFound();

                var aguinaldo = await _context.Aguinaldos
                    .Include(a => a.Empleado)           // Relación principal
                    .FirstOrDefaultAsync(a => a.AguinaldoId == id);

                if (aguinaldo == null)
                {
                    TempData["Error"] = "Aguinaldo no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                // Diagnóstico
                if (aguinaldo.Empleado == null)
                {
                    _logger.LogWarning("Aguinaldo ID {Id} no tiene Empleado cargado. EmpleadoId = {EmpId}",
                        id, aguinaldo.EmpleadoId);

                    TempData["Error"] = $"No se encontró el empleado asociado (ID: {aguinaldo.EmpleadoId}).";
                    return RedirectToAction(nameof(Index));
                }

                await CargarEmpleadosViewBag(aguinaldo.EmpleadoId);
                return View(aguinaldo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar Edit Aguinaldo ID: {Id}", id);
                TempData["Error"] = "Error al cargar el aguinaldo.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize("RRHH", "Jefatura")]
        public async Task<IActionResult> Edit(int id, Aguinaldo model)
        {
            try
            {
                if (id != model.AguinaldoId) return NotFound();

                ModelState.Remove("Empleado");
                AplicarValidaciones(model);

                if (!ModelState.IsValid)
                {
                    await CargarEmpleadosViewBag(model.EmpleadoId);
                    return View(model);
                }

                var existe = await _context.Aguinaldos
                    .AnyAsync(a =>
                        a.EmpleadoId == model.EmpleadoId &&
                        a.Anio == model.Anio &&
                        a.AguinaldoId != model.AguinaldoId);

                if (existe)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Este empleado ya tiene aguinaldo para el año {model.Anio}.");
                    await CargarEmpleadosViewBag(model.EmpleadoId);
                    return View(model);
                }

                model.MontoTotal = Math.Round(model.MontoTotal, 2);
                _context.Update(model);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Editar aguinaldo", "Aguinaldo",
                    $"ID: {id} — EmpleadoId: {model.EmpleadoId} — Año: {model.Anio}");

                _logger.LogInformation("Aguinaldo editado: ID {Id}", id);
                TempData["Success"] = "Aguinaldo actualizado correctamente.";
                return RedirectToAction(nameof(Index), new { anio = model.Anio });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error de concurrencia aguinaldo ID: {Id}", id);
                if (!await _context.Aguinaldos.AnyAsync(a => a.AguinaldoId == id))
                    return NotFound();
                ModelState.AddModelError(string.Empty,
                    "El registro fue modificado. Recargá e intentá de nuevo.");
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar aguinaldo ID: {Id}", id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado.");
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize("RRHH", "Jefatura")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var aguinaldo = await _context.Aguinaldos
                    .Include(a => a.Empleado)
                    .FirstOrDefaultAsync(a => a.AguinaldoId == id);

                if (aguinaldo == null)
                {
                    TempData["Error"] = "Aguinaldo no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Aguinaldos.Remove(aguinaldo);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Eliminar aguinaldo", "Aguinaldo",
                    $"{aguinaldo.Empleado.PrimerApellido} {aguinaldo.Empleado.Nombre} — Año: {aguinaldo.Anio}");

                _logger.LogInformation("Aguinaldo eliminado: ID {Id}", id);
                TempData["Success"] = $"Aguinaldo de {aguinaldo.Empleado.PrimerApellido} " +
                    $"{aguinaldo.Empleado.Nombre} eliminado.";
                return RedirectToAction(nameof(Index), new { anio = aguinaldo.Anio });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar aguinaldo ID: {Id}", id);
                TempData["Error"] = "Error al eliminar el aguinaldo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── DESCARGAR PDF ─────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> DescargarPDF(int id)
        {
            try
            {
                var aguinaldo = await _context.Aguinaldos
                    .Include(a => a.Empleado)
                    .FirstOrDefaultAsync(a => a.AguinaldoId == id);

                if (aguinaldo == null) return NotFound();

                var pdfBytes = _servicioPDF.GenerarPDFAguinaldo(aguinaldo);
                var nombreArchivo = $"Aguinaldo_{aguinaldo.Empleado.PrimerApellido}_{aguinaldo.Anio}.pdf";

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Descargar PDF aguinaldo", "Aguinaldo",
                    $"{aguinaldo.Empleado.PrimerApellido} {aguinaldo.Empleado.Nombre} — Año: {aguinaldo.Anio}");

                return File(pdfBytes, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF aguinaldo ID: {Id}", id);
                TempData["Error"] = "Error al generar el PDF.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── ENVIAR PDF POR EMAIL ──────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarPorEmail(int id)
        {
            try
            {
                var aguinaldo = await _context.Aguinaldos
                    .Include(a => a.Empleado)
                    .FirstOrDefaultAsync(a => a.AguinaldoId == id);

                if (aguinaldo == null)
                {
                    TempData["Error"] = "Aguinaldo no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                var correo = aguinaldo.Empleado.CorreoElectronico;
                if (string.IsNullOrWhiteSpace(correo))
                {
                    TempData["Error"] =
                        $"{aguinaldo.Empleado.PrimerApellido} " +
                        $"{aguinaldo.Empleado.Nombre} no tiene correo registrado.";
                    return RedirectToAction(nameof(Index), new { anio = aguinaldo.Anio });
                }

                var pdfBytes = _servicioPDF.GenerarPDFAguinaldoSinFirmas(aguinaldo);
                var nombreArchivo =
                    $"Aguinaldo_{aguinaldo.Empleado.PrimerApellido}_{aguinaldo.Anio}.pdf";

                var emailSvc = HttpContext.RequestServices
                    .GetRequiredService<EmailService>();

                var asunto = $"Boleta de Aguinaldo — Año {aguinaldo.Anio}";
                var cuerpo = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;'>
    <div style='background:#1A1A2E;padding:20px;'>
        <h2 style='color:#FF7A00;margin:0;'>Ferretería El Pana SRL</h2>
        <p style='color:#888;margin:4px 0 0;font-size:13px;'>
            Departamento de Recursos Humanos
        </p>
    </div>
    <div style='padding:20px;border:1px solid #eee;'>
        <p>Estimado(a) <strong>{aguinaldo.Empleado.PrimerApellido}
           {aguinaldo.Empleado.Nombre}</strong>,</p>
        <p>Adjunto encontrará su boleta de aguinaldo correspondiente al
           año <strong>{aguinaldo.Anio}</strong>.</p>
        <table style='width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;'>
            <tr style='background:#f9f9f9;'>
                <td style='padding:8px;border:1px solid #eee;'>Período</td>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    {aguinaldo.FechaInicio:dd/MM/yyyy} - {aguinaldo.FechaFin:dd/MM/yyyy}
                </td>
            </tr>
            <tr>
                <td style='padding:8px;border:1px solid #eee;'>Fecha de Pago</td>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    {aguinaldo.FechaPago:dd/MM/yyyy}
                </td>
            </tr>
            <tr style='background:#fff9f0;'>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    Monto Total
                </td>
                <td style='padding:8px;border:1px solid #eee;
                           font-weight:bold;font-size:16px;color:#FF7A00;'>
                    ₡{aguinaldo.MontoTotal:N2}
                </td>
            </tr>
        </table>
        <p style='color:#888;font-size:12px;'>
            Documento generado automáticamente por el Sistema GEPCP.
            No responder a este correo.
        </p>
    </div>
    <div style='background:#f5f5f5;padding:12px;text-align:center;
                font-size:11px;color:#888;'>
        Ferretería El Pana SRL · Cédula Jurídica: 3-102-745359
    </div>
</div>";

                var enviado = await emailSvc.EnviarPDFAsync(
                    correo,
                    $"{aguinaldo.Empleado.PrimerApellido} {aguinaldo.Empleado.Nombre}",
                    asunto, cuerpo, pdfBytes, nombreArchivo);

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Enviar boleta por email", "Aguinaldo",
                    $"{aguinaldo.Empleado.PrimerApellido} {aguinaldo.Empleado.Nombre} " +
                    $"→ {correo}");

                TempData[enviado ? "Success" : "Error"] = enviado
                    ? $"Boleta enviada a {correo}."
                    : "Error al enviar el correo. Verificá la configuración SMTP.";

                return RedirectToAction(nameof(Index), new { anio = aguinaldo.Anio });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email aguinaldo ID: {Id}", id);
                TempData["Error"] = "Error al enviar el correo. Intentá de nuevo.";
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
                    Text = $"{e.PrimerApellido} {e.SegundoApellido} {e.Nombre} — {e.Puesto}",
                    Selected = e.EmpleadoId == selectedId
                })
                .ToListAsync();
        }

        private void AplicarValidaciones(Aguinaldo model)
        {
            if (model.EmpleadoId <= 0)
                ModelState.AddModelError("EmpleadoId", "Seleccioná un empleado válido.");

            if (model.MontoTotal <= 0)
                ModelState.AddModelError("MontoTotal", "El monto debe ser mayor a cero.");
            else if (model.MontoTotal > 9_999_999.99m)
                ModelState.AddModelError("MontoTotal", "El monto excede el límite permitido.");

            if (model.Anio < 2020 || model.Anio > DateTime.Today.Year + 1)
                ModelState.AddModelError("Anio", "El año no es válido.");

            if (model.FechaInicio == default)
                ModelState.AddModelError("FechaInicio", "La fecha de inicio es obligatoria.");

            if (model.FechaFin == default)
                ModelState.AddModelError("FechaFin", "La fecha fin es obligatoria.");

            if (model.FechaInicio != default && model.FechaFin != default &&
                model.FechaFin <= model.FechaInicio)
                ModelState.AddModelError("FechaFin",
                    "La fecha fin debe ser posterior a la fecha inicio.");

            if (model.FechaPago == default)
                ModelState.AddModelError("FechaPago", "La fecha de pago es obligatoria.");
        }
        // ── APIs PARA BUSCADOR ───────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BuscarEmpleados(string? termino)
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
                .Take(12)
                .Select(e => new
                {
                    id = e.EmpleadoId,
                    nombre = $"{e.PrimerApellido} {e.SegundoApellido} {e.Nombre}".Trim(),
                    cedula = e.Cedula,
                    puesto = e.Puesto,
                    departamento = e.Departamento
                })
                .ToListAsync();

            return Json(empleados);
        }

        [HttpGet]
        public async Task<IActionResult> TodosLosEmpleados()
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
                    departamento = e.Departamento
                })
                .ToListAsync();

            return Json(empleados);
        }
    }
}