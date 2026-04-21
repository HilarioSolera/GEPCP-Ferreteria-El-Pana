using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Services;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class CreditoFerreteriaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreditoFerreteriaController> _logger;
        private readonly AuditoriaService _auditoria;
        private readonly ComprobantePlanillaService _servicioPDF;

        public CreditoFerreteriaController(
            ApplicationDbContext context,
            ILogger<CreditoFerreteriaController> logger,
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
            string? busqueda, string? estado, bool verTodos = false)
        {
            try
            {
                ViewBag.Busqueda = busqueda;
                ViewBag.EstadoFiltro = estado;
                ViewBag.VerTodos = verTodos;
                ViewBag.TotalCreditos = 0;
                ViewBag.TotalActivos = 0;
                ViewBag.TotalSaldo = 0m;
                ViewBag.TotalCuotasQuinc = 0m;

                if (!verTodos &&
                    string.IsNullOrWhiteSpace(busqueda) &&
                    string.IsNullOrWhiteSpace(estado))
                    return View(new List<CreditoFerreteria>());

                var query = _context.CreditosFerreteria
                    .Include(c => c.Empleado)
                    .Include(c => c.AbonosCreditoFerreteria)
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(estado))
                {
                    var activo = estado == "Activo";
                    query = query.Where(c => c.Activo == activo);
                }

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var t = busqueda.Trim().ToLower();
                    query = query.Where(c =>
                        c.Empleado.Nombre.ToLower().Contains(t) ||
                        c.Empleado.PrimerApellido.ToLower().Contains(t) ||
                        c.Empleado.Cedula.Contains(t) ||
                        c.Descripcion.ToLower().Contains(t));
                }

                var creditos = await query
                    .OrderByDescending(c => c.FechaCredito)
                    .ThenBy(c => c.Empleado.PrimerApellido)
                    .ToListAsync();

                ViewBag.TotalCreditos = creditos.Count;
                ViewBag.TotalActivos = creditos.Count(c => c.Activo);
                ViewBag.TotalSaldo = creditos.Where(c => c.Activo).Sum(c => c.Saldo);
                ViewBag.TotalCuotasQuinc = creditos.Where(c => c.Activo).Sum(c => c.CuotaQuincenal);

                return View(creditos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar créditos");
                TempData["Error"] = "Error al cargar los créditos.";
                return View(new List<CreditoFerreteria>());
            }
        }

        // CREATE

        public IActionResult Create()
        {
            return View(new CreditoFerreteria { FechaCredito = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreditoFerreteria model)
        {
            try
            {
                ModelState.Remove("Empleado");

                var empleado = await _context.Empleados
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpleadoId == model.EmpleadoId);

                if (empleado == null)
                {
                    ModelState.AddModelError("EmpleadoId", "Empleado no encontrado.");
                    return View(model);
                }

                var salarioReferencia = Math.Round(
     empleado.SalarioBase / empleado.FactorCuotaPrestamo, 2);
                AplicarValidaciones(model, salarioReferencia);

                if (!ModelState.IsValid)
                    return View(model);

                model.Saldo = Math.Round(model.MontoTotal, 2);
                model.MontoTotal = Math.Round(model.MontoTotal, 2);
                model.CuotaQuincenal = Math.Round(model.CuotaQuincenal, 2);
                model.Activo = true;

                _context.Add(model);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Crear crédito ferretería", "Créditos",
                    $"{empleado.PrimerApellido} {empleado.Nombre} — " +
                    $"₡{model.MontoTotal:N0} — Cuota: ₡{model.CuotaQuincenal:N0}");

                TempData["Success"] =
                    $"Crédito de ₡{model.MontoTotal:N0} registrado para " +
                    $"{empleado.PrimerApellido} {empleado.Nombre}.";

                // Recomendacion si la cuota es muy alta respecto al salario
                var salarioQuinc = Math.Round(empleado.SalarioBase / empleado.FactorCuotaPrestamo, 2);
                if (salarioQuinc > 0 && model.CuotaQuincenal > salarioQuinc * 0.3m)
                    TempData["Warning"] = "La cuota quincenal supera el 30% del salario quincenal del empleado. Verificá que sea sostenible.";
                else
                    TempData["Recomendacion"] = "Recordá que las cuotas se deducen automáticamente al calcular la planilla.";

                return RedirectToAction(nameof(Index), new { verTodos = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear crédito");
                ModelState.AddModelError(string.Empty, "Error inesperado. Intentá de nuevo.");
                return View(model);
            }
        }

        // REGISTRAR ABONO

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAbono(
            int creditoId, decimal monto, string? observaciones)
        {
            try
            {
                if (monto <= 0)
                {
                    TempData["Error"] = "El monto del abono debe ser mayor a cero.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                var credito = await _context.CreditosFerreteria
                    .Include(c => c.Empleado)
                    .FirstOrDefaultAsync(c => c.CreditoFerreteriaId == creditoId);

                if (credito == null)
                {
                    TempData["Error"] = "Crédito no encontrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                if (!credito.Activo)
                {
                    TempData["Error"] = "Este crédito ya está saldado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                monto = Math.Round(monto, 2);

                if (monto > credito.Saldo)
                {
                    TempData["Error"] =
                        $"El abono (₡{monto:N0}) no puede superar " +
                        $"el saldo actual (₡{credito.Saldo:N0}).";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                var saldoAnterior = credito.Saldo;
                credito.Saldo = Math.Round(credito.Saldo - monto, 2);

                var obs = string.IsNullOrWhiteSpace(observaciones)
                    ? $"Abono manual — Saldo anterior: ₡{saldoAnterior:N0} → Nuevo saldo: ₡{credito.Saldo:N0}"
                    : $"{observaciones.Trim()} — Saldo anterior: ₡{saldoAnterior:N0} → Nuevo saldo: ₡{credito.Saldo:N0}";

                if (credito.Saldo <= 0)
                {
                    credito.Saldo = 0;
                    credito.Activo = false;
                }

                _context.AbonosCreditoFerreteria.Add(new AbonoCreditoFerreteria
                {
                    CreditoFerreteriaId = creditoId,
                    Monto = monto,
                    FechaAbono = DateTime.Now,
                    Observaciones = obs
                });

                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Abono crédito ferretería", "Créditos",
                    $"{credito.Empleado.PrimerApellido} {credito.Empleado.Nombre} — " +
                    $"Abono: ₡{monto:N0} — Saldo: ₡{credito.Saldo:N0}");

                TempData["Success"] = credito.Activo
                    ? $"Abono de ₡{monto:N0} registrado. Saldo restante: ₡{credito.Saldo:N0}."
                    : $"Abono de ₡{monto:N0} registrado. " +
                      $"¡Crédito de {credito.Empleado.PrimerApellido} {credito.Empleado.Nombre} saldado!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar abono. CreditoId: {Id}", creditoId);
                TempData["Error"] = "Error al registrar el abono. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index), new { verTodos = true });
        }

        // CORREGIR ABONO

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CorregirAbono(
            int abonoId, decimal montoNuevo, string? observaciones)
        {
            try
            {
                var abono = await _context.AbonosCreditoFerreteria
                    .Include(a => a.CreditoFerreteria)
                    .ThenInclude(c => c.Empleado)
                    .FirstOrDefaultAsync(a => a.AbonoCreditoFerreteriaId == abonoId);

                if (abono == null)
                {
                    TempData["Error"] = "Abono no encontrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                montoNuevo = Math.Round(montoNuevo, 2);

                if (montoNuevo <= 0)
                {
                    TempData["Error"] = "El monto corregido debe ser mayor a cero.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                var credito = abono.CreditoFerreteria;
                var montoAnterior = abono.Monto;
                var nuevoSaldo = Math.Round(credito.Saldo + montoAnterior - montoNuevo, 2);

                if (nuevoSaldo < 0)
                {
                    TempData["Error"] =
                        $"El monto corregido (₡{montoNuevo:N0}) genera un saldo negativo. " +
                        $"El máximo es ₡{Math.Round(credito.Saldo + montoAnterior, 2):N0}.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                abono.Monto = montoNuevo;
                abono.Observaciones =
                    $"[CORREGIDO] Monto anterior: ₡{montoAnterior:N0} → Nuevo: ₡{montoNuevo:N0}. " +
                    (string.IsNullOrWhiteSpace(observaciones) ? "" : observaciones.Trim());

                credito.Saldo = nuevoSaldo;
                credito.Activo = nuevoSaldo > 0;

                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Corregir abono crédito", "Créditos",
                    $"AbonoId: {abonoId} — " +
                    $"{credito.Empleado.PrimerApellido} {credito.Empleado.Nombre} — " +
                    $"Anterior: ₡{montoAnterior:N0} → Nuevo: ₡{montoNuevo:N0} — " +
                    $"Saldo: ₡{nuevoSaldo:N0}");

                TempData["Success"] =
                    $"Abono corregido. ₡{montoAnterior:N0} → ₡{montoNuevo:N0}. " +
                    $"Nuevo saldo: ₡{nuevoSaldo:N0}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al corregir abono crédito ID: {Id}", abonoId);
                TempData["Error"] = "Error al corregir el abono. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index), new { verTodos = true });
        }

        // ELIMINAR ABONO

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarAbono(int abonoId)
        {
            try
            {
                var abono = await _context.AbonosCreditoFerreteria
                    .Include(a => a.CreditoFerreteria)
                    .ThenInclude(c => c.Empleado)
                    .FirstOrDefaultAsync(a => a.AbonoCreditoFerreteriaId == abonoId);

                if (abono == null)
                {
                    TempData["Error"] = "Abono no encontrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                var credito = abono.CreditoFerreteria;
                var montoRevertido = abono.Monto;

                credito.Saldo = Math.Round(credito.Saldo + montoRevertido, 2);
                credito.Activo = true;

                _context.AbonosCreditoFerreteria.Remove(abono);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Eliminar abono crédito", "Créditos",
                    $"AbonoId: {abonoId} — " +
                    $"{credito.Empleado.PrimerApellido} {credito.Empleado.Nombre} — " +
                    $"Monto revertido: ₡{montoRevertido:N0} — Nuevo saldo: ₡{credito.Saldo:N0}");

                TempData["Success"] =
                    $"Abono de ₡{montoRevertido:N0} eliminado. " +
                    $"Saldo actualizado: ₡{credito.Saldo:N0}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar abono crédito ID: {Id}", abonoId);
                TempData["Error"] = "Error al eliminar el abono. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index), new { verTodos = true });
        }

        // ELIMINAR CRÉDITO CERRADO

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                var credito = await _context.CreditosFerreteria
                    .Include(c => c.Empleado)
                    .Include(c => c.AbonosCreditoFerreteria)
                    .FirstOrDefaultAsync(c => c.CreditoFerreteriaId == id);

                if (credito == null)
                {
                    TempData["Error"] = "Crédito no encontrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                if (credito.Activo)
                {
                    TempData["Error"] = "Solo se pueden eliminar créditos saldados.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                _context.AbonosCreditoFerreteria.RemoveRange(credito.AbonosCreditoFerreteria);
                _context.CreditosFerreteria.Remove(credito);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Eliminar crédito saldado", "Créditos",
                    $"{credito.Empleado.PrimerApellido} {credito.Empleado.Nombre} — " +
                    $"Monto original: ₡{credito.MontoTotal:N0}");

                TempData["Success"] =
                    $"Crédito de {credito.Empleado.PrimerApellido} " +
                    $"{credito.Empleado.Nombre} eliminado.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar crédito ID: {Id}", id);
                TempData["Error"] = "Error al eliminar. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index), new { verTodos = true });
        }

        // CERRAR

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cerrar(int id)
        {
            try
            {
                var credito = await _context.CreditosFerreteria
                    .Include(c => c.Empleado)
                    .FirstOrDefaultAsync(c => c.CreditoFerreteriaId == id);

                if (credito == null)
                {
                    TempData["Error"] = "Crédito no encontrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                if (!credito.Activo)
                {
                    TempData["Error"] = "Este crédito ya estaba cerrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                credito.Activo = false;
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Cerrar crédito manual", "Créditos",
                    $"{credito.Empleado.PrimerApellido} {credito.Empleado.Nombre} — " +
                    $"Saldo: ₡{credito.Saldo:N0}");

                TempData["Success"] =
                    $"Crédito de {credito.Empleado.PrimerApellido} " +
                    $"{credito.Empleado.Nombre} cerrado.";

                if (credito.Saldo > 0)
                    TempData["Warning"] = $"El crédito se cerró con un saldo pendiente de ₡{credito.Saldo:N0}. Este monto no se descontará en futuras planillas.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar crédito ID: {Id}", id);
                TempData["Error"] = "Error al cerrar. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index), new { verTodos = true });
        }

        // REABRIR

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reabrir(int id)
        {
            try
            {
                var credito = await _context.CreditosFerreteria
                    .Include(c => c.Empleado)
                    .FirstOrDefaultAsync(c => c.CreditoFerreteriaId == id);

                if (credito == null)
                {
                    TempData["Error"] = "Crédito no encontrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                if (credito.Activo)
                {
                    TempData["Error"] = "Este crédito ya está activo.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                credito.Activo = true;
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Reabrir crédito", "Créditos",
                    $"{credito.Empleado.PrimerApellido} {credito.Empleado.Nombre}");

                TempData["Success"] =
                    $"Crédito de {credito.Empleado.PrimerApellido} " +
                    $"{credito.Empleado.Nombre} reabierto.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reabrir crédito ID: {Id}", id);
                TempData["Error"] = "Error al reabrir. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index), new { verTodos = true });
        }

        // DESCARGAR PDF

        [HttpGet]
        public async Task<IActionResult> DescargarPDF(int id)
        {
            try
            {
                var credito = await _context.CreditosFerreteria
                    .Include(c => c.Empleado)
                    .Include(c => c.AbonosCreditoFerreteria)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CreditoFerreteriaId == id);

                if (credito == null) return NotFound();

                var pdfBytes = _servicioPDF.GenerarPDFCredito(credito);
                var tipo = credito.Activo ? "EstadoCredito" : "Finiquito";
                var nombre =
                    $"{tipo}_{credito.Empleado.PrimerApellido}_" +
                    $"{credito.FechaCredito:yyyyMMdd}.pdf";

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    credito.Activo ? "Descargar estado crédito" : "Descargar finiquito crédito",
                    "Créditos",
                    $"{credito.Empleado.PrimerApellido} {credito.Empleado.Nombre}");

                return File(pdfBytes, "application/pdf", nombre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF crédito ID: {Id}", id);
                TempData["Error"] = "Error al generar el PDF. Intentá de nuevo.";
                return RedirectToAction(nameof(Index), new { verTodos = true });
            }
        }

        // ENVIAR PDF POR EMAIL

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarPorEmail(int id)
        {
            try
            {
                var credito = await _context.CreditosFerreteria
                    .Include(c => c.Empleado)
                    .Include(c => c.AbonosCreditoFerreteria)
                    .FirstOrDefaultAsync(c => c.CreditoFerreteriaId == id);

                if (credito == null)
                {
                    TempData["Error"] = "Crédito no encontrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                var correo = credito.Empleado.CorreoElectronico;
                if (string.IsNullOrWhiteSpace(correo))
                {
                    TempData["Error"] =
                        $"{credito.Empleado.PrimerApellido} " +
                        $"{credito.Empleado.Nombre} no tiene correo registrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                var pdfBytes = _servicioPDF.GenerarPDFCreditoSinFirmas(credito);
                var tipo = credito.Activo ? "EstadoCredito" : "Finiquito";
                var nombreArchivo =
                    $"{tipo}_{credito.Empleado.PrimerApellido}_{credito.FechaCredito:yyyyMMdd}.pdf";

                var emailSvc = HttpContext.RequestServices
                    .GetRequiredService<EmailService>();

                var asunto = credito.Activo
                    ? $"Estado de Crédito Ferretería — Monto: ₡{credito.Saldo:N0}"
                    : $"Finiquito de Crédito Ferretería — {credito.Empleado.PrimerApellido}";

                var saldoFormato = credito.Activo
                    ? $"Saldo Actual: ₡{credito.Saldo:N2}"
                    : $"Saldo Finalizado: ₡{credito.Saldo:N2}";

                var cuerpo = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;'>
    <div style='background:#1A1A2E;padding:20px;'>
        <h2 style='color:#FF7A00;margin:0;'>Ferretería El Pana SRL</h2>
        <p style='color:#888;margin:4px 0 0;font-size:13px;'>
            Departamento de Recursos Humanos
        </p>
    </div>
    <div style='padding:20px;border:1px solid #eee;'>
        <p>Estimado(a) <strong>{credito.Empleado.PrimerApellido}
           {credito.Empleado.Nombre}</strong>,</p>
        <p>Adjunto encontrará su boleta de {(credito.Activo ? "estado de crédito ferretería" : "finiquito de crédito ferretería")}
           solicitado.</p>
        <table style='width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;'>
            <tr style='background:#f9f9f9;'>
                <td style='padding:8px;border:1px solid #eee;'>Fecha de Crédito</td>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    {credito.FechaCredito:dd/MM/yyyy}
                </td>
            </tr>
            <tr>
                <td style='padding:8px;border:1px solid #eee;'>Monto Original</td>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    ₡{credito.MontoTotal:N2}
                </td>
            </tr>
            <tr>
                <td style='padding:8px;border:1px solid #eee;'>Cuota Actual</td>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    ₡{credito.CuotaQuincenal:N2}
                </td>
            </tr>
            <tr style='background:#fff9f0;'>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    {saldoFormato.Split(':')[0]}
                </td>
                <td style='padding:8px;border:1px solid #eee;
                           font-weight:bold;font-size:16px;color:#FF7A00;'>
                    ₡{credito.Saldo:N2}
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
                    $"{credito.Empleado.PrimerApellido} {credito.Empleado.Nombre}",
                    asunto,
                    cuerpo,
                    pdfBytes,
                    nombreArchivo);

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Enviar crédito ferretería por email", "Créditos",
                    $"{credito.Empleado.PrimerApellido} {credito.Empleado.Nombre} — Monto: ₡{credito.MontoTotal:N2}");

                if (!enviado)
                    TempData["Error"] = "No se pudo enviar el correo. Intentá de nuevo.";
                else
                    TempData["Success"] = $"Boleta de {tipo.ToLower()} enviada exitosamente.";

                return RedirectToAction(nameof(Index), new { verTodos = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar crédito por email ID: {Id}", id);
                TempData["Error"] = "Error al enviar el correo. Intentá de nuevo.";
                return RedirectToAction(nameof(Index), new { verTodos = true });
            }
        }

        // APIs

        // BuscarEmpleados
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
                .Take(10)
                .Select(e => new
                {
                    id = e.EmpleadoId,
                    nombre = $"{e.PrimerApellido} {e.SegundoApellido} {e.Nombre}".Trim(),
                    cedula = e.Cedula,
                    puesto = (from p in _context.Puestos where p.Nombre == e.Puesto select p.Codigo + " - " + e.Puesto).FirstOrDefault() ?? e.Puesto,
                    salarioBase = e.SalarioBase,
                    salarioQuincenal = Math.Round(
                        e.SalarioBase / (e.TipoPago == TipoPago.Semanal ? 4m :
                                         e.TipoPago == TipoPago.Mensual ? 1m : 2m), 2),
                    tipoPago = e.TipoPago.ToString(),
                    tieneActivo = _context.Prestamos.Any(
                        p => p.EmpleadoId == e.EmpleadoId && p.Activo)
                })
                .ToListAsync();

            return Json(empleados);
        }

        // TodosLosEmpleados
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
                    puesto = (from p in _context.Puestos where p.Nombre == e.Puesto select p.Codigo + " - " + e.Puesto).FirstOrDefault() ?? e.Puesto,
                    salarioBase = e.SalarioBase,
                    salarioQuincenal = Math.Round(
                        e.SalarioBase / (e.TipoPago == TipoPago.Semanal ? 4m :
                                         e.TipoPago == TipoPago.Mensual ? 1m : 2m), 2),
                    tipoPago = e.TipoPago.ToString(),
                    tieneActivo = _context.Prestamos.Any(
                        p => p.EmpleadoId == e.EmpleadoId && p.Activo)
                })
                .ToListAsync();

            return Json(empleados);
        }

        // HELPERS

        private void AplicarValidaciones(CreditoFerreteria model, decimal salarioQuincenal)
        {
            if (model.EmpleadoId <= 0)
                ModelState.AddModelError("EmpleadoId", "Seleccioná un empleado válido.");

            if (model.MontoTotal <= 0)
                ModelState.AddModelError("MontoTotal", "El monto debe ser mayor a cero.");
            else if (model.MontoTotal > 9_999_999.99m)
                ModelState.AddModelError("MontoTotal", "El monto excede el límite máximo.");

            if (model.CuotaQuincenal <= 0)
            {
                ModelState.AddModelError("CuotaQuincenal",
                    "La cuota quincenal debe ser mayor a cero.");
            }
            else if (model.MontoTotal > 0 && model.CuotaQuincenal > model.MontoTotal)
            {
                ModelState.AddModelError("CuotaQuincenal",
                    "La cuota no puede ser mayor al monto total.");
            }
            else if (salarioQuincenal > 0 && model.CuotaQuincenal > salarioQuincenal)
            {
                ModelState.AddModelError("CuotaQuincenal",
                    $"La cuota (₡{model.CuotaQuincenal:N0}) no puede superar el salario " +
                    $"quincenal del empleado (₡{salarioQuincenal:N0}).");
            }

            if (model.FechaCredito == default)
                ModelState.AddModelError("FechaCredito", "La fecha es obligatoria.");
            else if (model.FechaCredito > DateTime.Today.AddDays(1))
                ModelState.AddModelError("FechaCredito", "La fecha no puede ser futura.");
            else if (model.FechaCredito < DateTime.Today.AddYears(-2))
                ModelState.AddModelError("FechaCredito",
                    "La fecha no puede ser anterior a 2 años.");

            if (string.IsNullOrWhiteSpace(model.Descripcion))
                ModelState.AddModelError("Descripcion", "La descripción es obligatoria.");
            else if (model.Descripcion.Trim().Length < 5)
                ModelState.AddModelError("Descripcion",
                    "La descripción debe tener al menos 5 caracteres.");
            else if (model.Descripcion.Trim().Length > 200)
                ModelState.AddModelError("Descripcion",
                    "La descripción no puede superar 200 caracteres.");
        }
    }
}