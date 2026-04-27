using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class ComisionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ComisionesController> _logger;
        private readonly AuditoriaService _auditoria;
        private readonly ComprobantePlanillaService _servicioPDF;

        public ComisionesController(
            ApplicationDbContext context,
            ILogger<ComisionesController> logger,
            AuditoriaService auditoria,
            ComprobantePlanillaService servicioPDF)
        {
            _context = context;
            _logger = logger;
            _auditoria = auditoria;
            _servicioPDF = servicioPDF;
        }

        // INDEX

        public async Task<IActionResult> Index(
            string? busqueda, int? periodoId, bool verTodos = false)
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
                ViewBag.Busqueda = busqueda;
                ViewBag.PeriodoId = periodoId;
                ViewBag.VerTodos = verTodos;

                // KPIs vacíos por defecto
                ViewBag.TotalComisiones = 0;
                ViewBag.MontoTotal = 0m;
                ViewBag.PromedioMonto = 0m;
                ViewBag.EmpleadosConComision = 0;

                if (!verTodos &&
                    string.IsNullOrWhiteSpace(busqueda) &&
                    periodoId == null)
                    return View(new List<Comision>());

              var query = _context.Comisiones
      .Include(c => c.Empleado)
      .Include(c => c.PeriodoPago)
      .AsNoTracking()
      .AsQueryable();

                // Filtro por período
                if (periodoId.HasValue)
                {
                    var periodo = periodos.FirstOrDefault(p => p.PeriodoPagoId == periodoId);
                    if (periodo != null)
                        query = query.Where(c =>
                            c.Fecha >= periodo.FechaInicio &&
                            c.Fecha <= periodo.FechaFin);
                }

                // Filtro por búsqueda
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var t = busqueda.Trim().ToLower();
                    query = query.Where(c =>
                        c.Empleado.Nombre.ToLower().Contains(t) ||
                        c.Empleado.PrimerApellido.ToLower().Contains(t) ||
                        c.Empleado.Cedula.Contains(t) ||
                        c.Descripcion.ToLower().Contains(t));
                    
                }

                var comisiones = await query
                    .OrderByDescending(c => c.Fecha)
                    .ThenBy(c => c.Empleado.PrimerApellido)
                    .ToListAsync();

                ViewBag.TotalComisiones = comisiones.Count;
                ViewBag.MontoTotal = comisiones.Sum(c => c.Monto);
                ViewBag.PromedioMonto = comisiones.Any()
                    ? comisiones.Average(c => c.Monto) : 0m;
                ViewBag.EmpleadosConComision = comisiones
                    .Select(c => c.EmpleadoId).Distinct().Count();

                return View(comisiones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar comisiones");
                TempData["Error"] = "Error al cargar las comisiones.";
                return View(new List<Comision>());
            }
        }

        // CREATE

        public async Task<IActionResult> Create()
        {
            await CargarPeriodosViewBag();
            return View(new Comision { Fecha = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Comision model)
        {
            try
            {
                ModelState.Remove("Empleado");
                await AplicarValidacionesAsync(model);

                if (!ModelState.IsValid)
                {
                    await CargarPeriodosViewBag(model.PeriodoPagoId);
                    return View(model);
                }

                // Validar que no exista comisión duplicada (mismo empleado, misma fecha, mismo monto)
                var duplicado = await _context.Comisiones
                    .AnyAsync(c =>
                        c.EmpleadoId == model.EmpleadoId &&
                        c.Fecha.Date == model.Fecha.Date &&
                        c.Monto == Math.Round(model.Monto, 2));

                if (duplicado)
                {
                    ModelState.AddModelError(string.Empty,
                        "Ya existe una comisión con el mismo empleado, fecha y monto.");
                    await CargarPeriodosViewBag(model.PeriodoPagoId);
                    return View(model);
                }

                model.Descripcion = SanitizarTexto(model.Descripcion);
                model.Monto = Math.Round(model.Monto, 2);

                _context.Add(model);
                await _context.SaveChangesAsync();

                var emp = await _context.Empleados.FindAsync(model.EmpleadoId);
                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Crear comisión", "Comisiones",
                    $"{emp?.PrimerApellido} {emp?.Nombre} — " +
                    $"₡{model.Monto:N0} — {model.Fecha:dd/MM/yyyy}");

                TempData["Success"] =
                    $"Comisión de ₡{model.Monto:N0} registrada correctamente.";

                if (model.Monto > 500_000)
                    TempData["Warning"] = "La comisión supera ₡500,000. Verificá que el monto sea correcto.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear comisión");
                ModelState.AddModelError(string.Empty, "Error inesperado. Intentá de nuevo.");
                await CargarPeriodosViewBag(model.PeriodoPagoId);
                return View(model);
            }
        }

        // EDIT

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0) return NotFound();

            var comision = await _context.Comisiones
                .Include(c => c.Empleado)
                .FirstOrDefaultAsync(c => c.ComisionId == id);

            if (comision == null) return NotFound();

            ViewBag.EmpleadoNombre = $"{comision.Empleado.PrimerApellido} " +
                $"{comision.Empleado.SegundoApellido} {comision.Empleado.Nombre}".Trim();
            ViewBag.EmpleadoCedula = comision.Empleado.Cedula;

            await CargarPeriodosViewBag(comision.PeriodoPagoId);
            return View(comision);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Comision model)
        {
            try
            {
                if (id != model.ComisionId) return NotFound();

                // Recuperar empleado original — no se puede cambiar
                var original = await _context.Comisiones
                    .AsNoTracking()
                    .Include(c => c.Empleado)
                    .FirstOrDefaultAsync(c => c.ComisionId == id);

                if (original == null) return NotFound();

                // Forzar empleado original
                model.EmpleadoId = original.EmpleadoId;

                ModelState.Remove("Empleado");
                await AplicarValidacionesAsync(model);

                if (!ModelState.IsValid)
                {
                    ViewBag.EmpleadoNombre = $"{original.Empleado.PrimerApellido} " +
                        $"{original.Empleado.SegundoApellido} {original.Empleado.Nombre}".Trim();
                    ViewBag.EmpleadoCedula = original.Empleado.Cedula;
                    await CargarPeriodosViewBag(model.PeriodoPagoId);
                    return View(model);
                }

                model.Descripcion = SanitizarTexto(model.Descripcion);
                model.Monto = Math.Round(model.Monto, 2);

                _context.Update(model);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Editar comisión", "Comisiones",
                    $"ID: {id} — {original.Empleado.PrimerApellido} — ₡{model.Monto:N0}");

                TempData["Success"] =
                    $"Comisión de ₡{model.Monto:N0} actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrencia al editar comisión ID: {Id}", id);
                if (!await _context.Comisiones.AnyAsync(c => c.ComisionId == id))
                    return NotFound();
                TempData["Error"] = "El registro fue modificado. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar comisión ID: {Id}", id);
                ModelState.AddModelError(string.Empty, "Error inesperado. Intentá de nuevo.");
                await CargarPeriodosViewBag(model.PeriodoPagoId);
                return View(model);
            }
        }

        // DELETE

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

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Eliminar comisión", "Comisiones",
                    $"{comision.Empleado.PrimerApellido} {comision.Empleado.Nombre} — " +
                    $"₡{comision.Monto:N0}");

                TempData["Success"] =
                    $"Comisión de ₡{comision.Monto:N0} eliminada.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar comisión ID: {Id}", id);
                TempData["Error"] = "Error al eliminar la comisión.";
            }
            return RedirectToAction(nameof(Index));
        }

        // DESCARGAR PDF

        [HttpGet]
        public async Task<IActionResult> DescargarPDF(int id)
        {
            try
            {
                var comision = await _context.Comisiones
                    .Include(c => c.Empleado)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ComisionId == id);

                if (comision == null) return NotFound();

                var usuario = HttpContext.Session.GetString("Usuario") ?? "Sistema";
                var pdfBytes = _servicioPDF.GenerarPDFComision(comision, usuario);
                var nombre =
                    $"Comision_{comision.Empleado.PrimerApellido}_" +
                    $"{comision.Fecha:yyyyMMdd}.pdf";

                await _auditoria.RegistrarAsync(
                    usuario,
                    "Descargar PDF comisión", "Comisiones",
                    $"{comision.Empleado.PrimerApellido} {comision.Empleado.Nombre}");



                return File(pdfBytes, "application/pdf", nombre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF comisión ID: {Id}", id);
                TempData["Error"] = "Error al generar el PDF.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ENVIAR PDF POR EMAIL

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarPorEmail(int id)
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

                var correo = comision.Empleado.CorreoElectronico;
                if (string.IsNullOrWhiteSpace(correo))
                {
                    TempData["Error"] =
                        $"{comision.Empleado.PrimerApellido} " +
                        $"{comision.Empleado.Nombre} no tiene correo registrado.";
                    return RedirectToAction(nameof(Index));
                }

                var pdfBytes = _servicioPDF.GenerarPDFComisionSinFirmas(comision);
                var nombreArchivo =
                    $"Comision_{comision.Empleado.PrimerApellido}_" +
                    $"{comision.Fecha:yyyyMMdd}.pdf";

                var emailSvc = HttpContext.RequestServices
                    .GetRequiredService<EmailService>();

                var asunto = $"Boleta de Comisión — {comision.Fecha:dd/MM/yyyy}";
                var cuerpo = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;'>
    <div style='background:#1A1A2E;padding:20px;'>
        <h2 style='color:#FF7A00;margin:0;'>Ferretería El Pana SRL</h2>
        <p style='color:#888;margin:4px 0 0;font-size:13px;'>
            Departamento de Recursos Humanos
        </p>
    </div>
    <div style='padding:20px;border:1px solid #eee;'>
        <p>Estimado(a) <strong>{comision.Empleado.PrimerApellido}
           {comision.Empleado.Nombre}</strong>,</p>
        <p>Adjunto encontrará su boleta de comisión correspondiente al
           <strong>{comision.Fecha:dd/MM/yyyy}</strong>.</p>
        <table style='width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;'>
            <tr style='background:#f9f9f9;'>
                <td style='padding:8px;border:1px solid #eee;'>Fecha</td>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    {comision.Fecha:dd/MM/yyyy}
                </td>
            </tr>
            <tr style='background:#fff9f0;'>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    Monto
                </td>
                <td style='padding:8px;border:1px solid #eee;
                           font-weight:bold;font-size:16px;color:#FF7A00;'>
                    ₡{comision.Monto:N2}
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
                    $"{comision.Empleado.PrimerApellido} {comision.Empleado.Nombre}",
                    asunto, cuerpo, pdfBytes, nombreArchivo);

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Enviar boleta por email", "Comisiones",
                    $"{comision.Empleado.PrimerApellido} {comision.Empleado.Nombre} " +
                    $"→ {correo}");

                TempData[enviado ? "Success" : "Error"] = enviado
                    ? $"Boleta enviada a {correo}."
                    : "Error al enviar el correo. Verificá la configuración SMTP.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email comisión ID: {Id}", id);
                TempData["Error"] = "Error al enviar el correo. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> BuscarEmpleados(string? termino, int? periodoId)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return Json(new List<object>());

            var t = termino.Trim().ToLower();

            var empleados = await _context.Empleados
                .AsNoTracking()
                .Where(e => e.Activo && (
                    EF.Functions.Like(e.Nombre.ToLower(), $"%{t}%") ||
                    EF.Functions.Like(e.PrimerApellido.ToLower(), $"%{t}%") ||
                    (e.SegundoApellido != null &&
                     EF.Functions.Like(e.SegundoApellido.ToLower(), $"%{t}%")) ||
                    EF.Functions.Like(e.Cedula, $"%{t}%")))
                .OrderBy(e => e.PrimerApellido)
                .ThenBy(e => e.Nombre)
                .Take(15)
                .Select(e => new
                {
                    id = e.EmpleadoId,
                    nombre = $"{e.PrimerApellido} {e.SegundoApellido} {e.Nombre}".Trim(),
                    cedula = e.Cedula,
                    puesto = (from p in _context.Puestos where p.Nombre == e.Puesto select p.Codigo + " - " + e.Puesto).FirstOrDefault() ?? e.Puesto,
                    tipoPago = e.TipoPago.ToString()
                })
                .ToListAsync();

            return Json(empleados);
        }

        [HttpGet]
        public async Task<IActionResult> TodosLosEmpleados(int? periodoId)
        {
            var empleados = await _context.Empleados
                .AsNoTracking()
                .Where(e => e.Activo)
                .OrderBy(e => e.PrimerApellido)
                .ThenBy(e => e.Nombre)
                .Select(e => new
                {
                    id = e.EmpleadoId,
                    nombre = $"{e.PrimerApellido} {e.SegundoApellido} {e.Nombre}".Trim(),
                    cedula = e.Cedula,
                    puesto = (from p in _context.Puestos where p.Nombre == e.Puesto select p.Codigo + " - " + e.Puesto).FirstOrDefault() ?? e.Puesto,
                    tipoPago = e.TipoPago.ToString()
                })
                .ToListAsync();

            return Json(empleados);
        }
        // HELPERS

        private async Task CargarPeriodosViewBag(int? selectedId = null)
        {
            var hoy = DateTime.Today;
            ViewBag.PeriodosSelect = await _context.PeriodosPago
                .AsNoTracking()
                .Where(p => p.Anio == hoy.Year && p.Mes == hoy.Month)
                .OrderByDescending(p => p.Quincena)
                .Select(p => new
                {
                    p.PeriodoPagoId,
                    p.Descripcion,
                    p.FechaInicio,
                    p.FechaFin,
                    Seleccionado = p.PeriodoPagoId == selectedId
                })
                .ToListAsync();
        }

        private async Task AplicarValidacionesAsync(Comision model)
        {
            if (model.EmpleadoId <= 0)
                ModelState.AddModelError("EmpleadoId", "Seleccioná un empleado válido.");

            if (model.Monto <= 0)
                ModelState.AddModelError("Monto", "El monto debe ser mayor a cero.");
            else if (model.Monto > 9_999_999.99m)
                ModelState.AddModelError("Monto", "El monto excede el límite máximo.");

            if (model.Fecha == default)
            {
                ModelState.AddModelError("Fecha", "La fecha es obligatoria.");
            }
            else if (model.PeriodoPagoId.HasValue && model.PeriodoPagoId > 0)
            {
                var periodo = await _context.PeriodosPago
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PeriodoPagoId == model.PeriodoPagoId);

                if (periodo != null &&
                    (model.Fecha < periodo.FechaInicio || model.Fecha > periodo.FechaFin))
                {
                    ModelState.AddModelError("Fecha",
                        $"La fecha debe estar dentro del período: " +
                        $"{periodo.FechaInicio:dd/MM/yyyy} al {periodo.FechaFin:dd/MM/yyyy}.");
                }
            }
            else
            {
                ModelState.AddModelError("PeriodoPagoId",
                    "Seleccioná un período de pago.");
            }

            if (string.IsNullOrWhiteSpace(model.Descripcion))
                ModelState.AddModelError("Descripcion", "La descripción es obligatoria.");
            else if (model.Descripcion.Trim().Length < 5)
                ModelState.AddModelError("Descripcion",
                    "La descripción debe tener al menos 5 caracteres.");
            else if (model.Descripcion.Trim().Length > 200)
                ModelState.AddModelError("Descripcion",
                    "La descripción no puede superar 200 caracteres.");
        }

        private static string SanitizarTexto(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return Regex.Replace(input.Trim(), @"[<>""'%;()&]", string.Empty);
        }
    }
}