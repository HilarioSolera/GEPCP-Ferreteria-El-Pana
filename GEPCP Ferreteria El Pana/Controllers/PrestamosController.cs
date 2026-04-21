using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Services;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class PrestamosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PrestamosController> _logger;
        private readonly AuditoriaService _auditoria;
        private readonly ComprobantePlanillaService _servicioPDF;

        public PrestamosController(
            ApplicationDbContext context,
            ILogger<PrestamosController> logger,
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
                ViewBag.TotalSaldo = 0m;
                ViewBag.TotalCuotas = 0m;
                ViewBag.TotalPrestamos = 0;
                ViewBag.TotalActivos = 0;

                if (!verTodos &&
                    string.IsNullOrWhiteSpace(busqueda) &&
                    string.IsNullOrWhiteSpace(estado))
                    return View(new List<Prestamo>());

                var query = _context.Prestamos
                    .Include(p => p.Empleado)
                    .Include(p => p.AbonosPrestamo)
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(estado))
                {
                    var activo = estado == "Activo";
                    query = query.Where(p => p.Activo == activo);
                }

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var t = busqueda.Trim().ToLower();
                    query = query.Where(p =>
                        p.Empleado.Nombre.ToLower().Contains(t) ||
                        p.Empleado.PrimerApellido.ToLower().Contains(t) ||
                        p.Empleado.Cedula.Contains(t));
                }

                var prestamos = await query
                    .OrderByDescending(p => p.FechaPrestamo)
                    .ToListAsync();

                ViewBag.TotalSaldo = prestamos.Where(p => p.Activo).Sum(p => p.Monto);
                ViewBag.TotalCuotas = prestamos.Where(p => p.Activo).Sum(p => p.CuotaMensual);
                ViewBag.TotalPrestamos = prestamos.Count;
                ViewBag.TotalActivos = prestamos.Count(p => p.Activo);

                return View(prestamos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar préstamos");
                TempData["Error"] = "Error al cargar los préstamos.";
                return View(new List<Prestamo>());
            }
        }

        // CREATE

        public IActionResult Create()
        {
            return View(new PrestamoViewModel { FechaPrestamo = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PrestamoViewModel model)
        {
            try
            {
                var empleado = await _context.Empleados
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpleadoId == model.EmpleadoId);

                if (empleado == null)
                {
                    ModelState.AddModelError("EmpleadoId", "Empleado no encontrado.");
                    return View(model);
                }

                // Después — usar FactorCuotaPrestamo del empleado
                var salarioReferencia = Math.Round(
                    empleado.SalarioBase / empleado.FactorCuotaPrestamo, 2);
                AplicarValidaciones(model, salarioReferencia);

                if (!ModelState.IsValid)
                    return View(model);

                var tieneActivo = await _context.Prestamos
                    .AnyAsync(p => p.EmpleadoId == model.EmpleadoId && p.Activo);

                if (tieneActivo)
                {
                    ModelState.AddModelError(string.Empty,
                        "Este empleado ya tiene un préstamo activo. " +
                        "Debe saldarlo antes de otorgar uno nuevo.");
                    return View(model);
                }

                var montoRedondeado = Math.Round(model.MontoPrincipal, 2);
                var cuotaRedondeada = Math.Round(model.CuotaMensual, 2);

                var prestamo = new Prestamo
                {
                    EmpleadoId = model.EmpleadoId,
                    Monto = montoRedondeado,
                    MontoOriginal = montoRedondeado,
                    FechaPrestamo = model.FechaPrestamo,
                    Interes = 0,
                    Cuotas = model.CuotasTotal,
                    CuotaMensual = cuotaRedondeada,
                    Activo = true
                };

                _context.Add(prestamo);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Crear préstamo", "Préstamos",
                    $"{empleado.PrimerApellido} {empleado.Nombre} — " +
                    $"₡{montoRedondeado:N0} en {model.CuotasTotal} cuota(s) de ₡{cuotaRedondeada:N0}");

                TempData["Success"] =
                    $"Préstamo de ₡{montoRedondeado:N0} registrado para " +
                    $"{empleado.PrimerApellido} {empleado.Nombre}.";

                return RedirectToAction(nameof(Index), new { verTodos = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear préstamo");
                ModelState.AddModelError(string.Empty, "Error inesperado. Intentá de nuevo.");
                return View(model);
            }
        }

        // REGISTRAR ABONO

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAbono(
            int prestamoId, decimal monto, string? observaciones)
        {
            try
            {
                if (monto <= 0)
                {
                    TempData["Error"] = "El monto del abono debe ser mayor a cero.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                var prestamo = await _context.Prestamos
                    .Include(p => p.Empleado)
                    .FirstOrDefaultAsync(p => p.PrestamoId == prestamoId);

                if (prestamo == null)
                {
                    TempData["Error"] = "Préstamo no encontrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                if (!prestamo.Activo)
                {
                    TempData["Error"] = "Este préstamo ya está cerrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                monto = Math.Round(monto, 2);

                if (monto > prestamo.Monto)
                {
                    TempData["Error"] =
                        $"El abono (₡{monto:N0}) no puede superar " +
                        $"el saldo actual (₡{prestamo.Monto:N0}).";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                var saldoAnterior = prestamo.Monto;
                prestamo.Monto = Math.Round(prestamo.Monto - monto, 2);

                var obs = string.IsNullOrWhiteSpace(observaciones)
                    ? $"Abono manual — Saldo anterior: ₡{saldoAnterior:N0} → Nuevo saldo: ₡{prestamo.Monto:N0}"
                    : $"{observaciones.Trim()} — Saldo anterior: ₡{saldoAnterior:N0} → Nuevo saldo: ₡{prestamo.Monto:N0}";

                if (prestamo.Monto <= 0)
                {
                    prestamo.Monto = 0;
                    prestamo.Activo = false;
                }

                _context.AbonosPrestamo.Add(new AbonoPrestamo
                {
                    PrestamoId = prestamoId,
                    Monto = monto,
                    FechaAbono = DateTime.Now,
                    Observaciones = obs
                });

                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Abono préstamo", "Préstamos",
                    $"{prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre} — " +
                    $"Abono: ₡{monto:N0} — Saldo: ₡{prestamo.Monto:N0}");

                TempData["Success"] = prestamo.Activo
                    ? $"Abono de ₡{monto:N0} registrado. Saldo restante: ₡{prestamo.Monto:N0}."
                    : $"Abono de ₡{monto:N0} registrado. " +
                      $"¡Préstamo de {prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre} saldado!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar abono. PrestamoId: {P}", prestamoId);
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
                var abono = await _context.AbonosPrestamo
                    .Include(a => a.Prestamo)
                    .ThenInclude(p => p.Empleado)
                    .FirstOrDefaultAsync(a => a.AbonoPrestamoId == abonoId);

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

                var prestamo = abono.Prestamo;
                var montoAnterior = abono.Monto;
                var diferencia = montoNuevo - montoAnterior;

                // Recalcular saldo: revertir abono anterior y aplicar el nuevo
                var nuevoSaldo = Math.Round(prestamo.Monto + montoAnterior - montoNuevo, 2);

                if (nuevoSaldo < 0)
                {
                    TempData["Error"] =
                        $"El monto corregido (₡{montoNuevo:N0}) genera un saldo negativo. " +
                        $"El máximo permitido es ₡{Math.Round(prestamo.Monto + montoAnterior, 2):N0}.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                // Actualizar abono
                abono.Monto = montoNuevo;
                abono.Observaciones =
                    $"[CORREGIDO] Monto anterior: ₡{montoAnterior:N0} → Nuevo: ₡{montoNuevo:N0}. " +
                    (string.IsNullOrWhiteSpace(observaciones)
                        ? ""
                        : observaciones.Trim());

                // Actualizar saldo del préstamo
                prestamo.Monto = nuevoSaldo;
                prestamo.Activo = nuevoSaldo > 0;

                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Corregir abono préstamo", "Préstamos",
                    $"AbonoId: {abonoId} — " +
                    $"{prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre} — " +
                    $"Monto anterior: ₡{montoAnterior:N0} → Nuevo: ₡{montoNuevo:N0} — " +
                    $"Nuevo saldo: ₡{nuevoSaldo:N0}");

                TempData["Success"] =
                    $"Abono corregido. Monto anterior ₡{montoAnterior:N0} → ₡{montoNuevo:N0}. " +
                    $"Nuevo saldo del préstamo: ₡{nuevoSaldo:N0}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al corregir abono ID: {Id}", abonoId);
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
                var abono = await _context.AbonosPrestamo
                    .Include(a => a.Prestamo)
                    .ThenInclude(p => p.Empleado)
                    .FirstOrDefaultAsync(a => a.AbonoPrestamoId == abonoId);

                if (abono == null)
                {
                    TempData["Error"] = "Abono no encontrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                var prestamo = abono.Prestamo;
                var montoRevertido = abono.Monto;

                // Revertir el saldo
                prestamo.Monto = Math.Round(prestamo.Monto + montoRevertido, 2);
                prestamo.Activo = true; // Si se elimina un abono el préstamo vuelve a estar activo

                _context.AbonosPrestamo.Remove(abono);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Eliminar abono préstamo", "Préstamos",
                    $"AbonoId: {abonoId} — " +
                    $"{prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre} — " +
                    $"Monto revertido: ₡{montoRevertido:N0} — Nuevo saldo: ₡{prestamo.Monto:N0}");

                TempData["Success"] =
                    $"Abono de ₡{montoRevertido:N0} eliminado. " +
                    $"Saldo actualizado: ₡{prestamo.Monto:N0}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar abono ID: {Id}", abonoId);
                TempData["Error"] = "Error al eliminar el abono. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index), new { verTodos = true });
        }

        // ELIMINAR PRÉSTAMO CERRADO

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                var prestamo = await _context.Prestamos
                    .Include(p => p.Empleado)
                    .Include(p => p.AbonosPrestamo)
                    .FirstOrDefaultAsync(p => p.PrestamoId == id);

                if (prestamo == null)
                {
                    TempData["Error"] = "Préstamo no encontrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                if (prestamo.Activo)
                {
                    TempData["Error"] =
                        "Solo se pueden eliminar préstamos cerrados (saldados).";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                _context.AbonosPrestamo.RemoveRange(prestamo.AbonosPrestamo);
                _context.Prestamos.Remove(prestamo);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Eliminar préstamo cerrado", "Préstamos",
                    $"{prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre} — " +
                    $"Monto original: ₡{prestamo.MontoOriginal:N0}");

                TempData["Success"] =
                    $"Préstamo de {prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre} eliminado.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar préstamo ID: {Id}", id);
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
                var prestamo = await _context.Prestamos
                    .Include(p => p.Empleado)
                    .FirstOrDefaultAsync(p => p.PrestamoId == id);

                if (prestamo == null)
                {
                    TempData["Error"] = "Préstamo no encontrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                if (!prestamo.Activo)
                {
                    TempData["Error"] = "Este préstamo ya estaba cerrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                prestamo.Activo = false;
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Cerrar préstamo manual", "Préstamos",
                    $"{prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre} — " +
                    $"Saldo: ₡{prestamo.Monto:N0}");

                TempData["Success"] =
                    $"Préstamo de {prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre} cerrado.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar préstamo ID: {Id}", id);
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
                var prestamo = await _context.Prestamos
                    .Include(p => p.Empleado)
                    .FirstOrDefaultAsync(p => p.PrestamoId == id);

                if (prestamo == null)
                {
                    TempData["Error"] = "Préstamo no encontrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                if (prestamo.Activo)
                {
                    TempData["Error"] = "Este préstamo ya está activo.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                var tieneOtroActivo = await _context.Prestamos
                    .AnyAsync(p =>
                        p.EmpleadoId == prestamo.EmpleadoId &&
                        p.Activo &&
                        p.PrestamoId != id);

                if (tieneOtroActivo)
                {
                    TempData["Error"] = "El empleado ya tiene otro préstamo activo.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                prestamo.Activo = true;
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Reabrir préstamo", "Préstamos",
                    $"{prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre}");

                TempData["Success"] =
                    $"Préstamo de {prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre} reabierto.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reabrir préstamo ID: {Id}", id);
                TempData["Error"] = "Error al reabrir. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index), new { verTodos = true });
        }

        // DESCARGAR FINIQUITO PDF

        [HttpGet]
        public async Task<IActionResult> DescargarFiniquito(int id)
        {
            try
            {
                var prestamo = await _context.Prestamos
                    .Include(p => p.Empleado)
                    .Include(p => p.AbonosPrestamo)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PrestamoId == id);

                if (prestamo == null) return NotFound();

                // Funciona para activos (estado actual) y cerrados (finiquito)
                var pdfBytes = _servicioPDF.GenerarFiniquitoPrestamo(prestamo);

                var tipo = prestamo.Activo ? "EstadoPrestamo" : "Finiquito";
                var nombre =
                    $"{tipo}_{prestamo.Empleado.PrimerApellido}_" +
                    $"{prestamo.FechaPrestamo:yyyyMMdd}.pdf";

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    prestamo.Activo ? "Descargar estado préstamo" : "Descargar finiquito préstamo",
                    "Préstamos",
                    $"{prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre}");

                return File(pdfBytes, "application/pdf", nombre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF préstamo ID: {Id}", id);
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
                var prestamo = await _context.Prestamos
                    .Include(p => p.Empleado)
                    .Include(p => p.AbonosPrestamo)
                    .FirstOrDefaultAsync(p => p.PrestamoId == id);

                if (prestamo == null)
                {
                    TempData["Error"] = "Préstamo no encontrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                var correo = prestamo.Empleado.CorreoElectronico;
                if (string.IsNullOrWhiteSpace(correo))
                {
                    TempData["Error"] =
                        $"{prestamo.Empleado.PrimerApellido} " +
                        $"{prestamo.Empleado.Nombre} no tiene correo registrado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                var pdfBytes = _servicioPDF.GenerarFiniquitoPrestamoSinFirmas(prestamo);
                var tipo = prestamo.Activo ? "EstadoPrestamo" : "Finiquito";
                var nombreArchivo =
                    $"{tipo}_{prestamo.Empleado.PrimerApellido}_{prestamo.FechaPrestamo:yyyyMMdd}.pdf";

                var emailSvc = HttpContext.RequestServices
                    .GetRequiredService<EmailService>();

                var asunto = prestamo.Activo
                    ? $"Estado de Préstamo — Monto: ₡{prestamo.Monto:N0}"
                    : $"Finiquito de Préstamo — {prestamo.Empleado.PrimerApellido}";

                var saldoFormato = prestamo.Activo
                    ? $"Saldo Actual: ₡{prestamo.Monto:N2}"
                    : $"Saldo Finalizado: ₡{prestamo.Monto:N2}";

                var cuerpo = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;'>
    <div style='background:#1A1A2E;padding:20px;'>
        <h2 style='color:#FF7A00;margin:0;'>Ferretería El Pana SRL</h2>
        <p style='color:#888;margin:4px 0 0;font-size:13px;'>
            Departamento de Recursos Humanos
        </p>
    </div>
    <div style='padding:20px;border:1px solid #eee;'>
        <p>Estimado(a) <strong>{prestamo.Empleado.PrimerApellido}
           {prestamo.Empleado.Nombre}</strong>,</p>
        <p>Adjunto encontrará su boleta de {(prestamo.Activo ? "estado de préstamo" : "finiquito de préstamo")}
           solicitado.</p>
        <table style='width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;'>
            <tr style='background:#f9f9f9;'>
                <td style='padding:8px;border:1px solid #eee;'>Fecha de Préstamo</td>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    {prestamo.FechaPrestamo:dd/MM/yyyy}
                </td>
            </tr>
            <tr>
                <td style='padding:8px;border:1px solid #eee;'>Monto Original</td>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    ₡{(prestamo.MontoOriginal > 0 ? prestamo.MontoOriginal : prestamo.CuotaMensual * prestamo.Cuotas):N2}
                </td>
            </tr>
            <tr>
                <td style='padding:8px;border:1px solid #eee;'>Cuota Actual</td>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    ₡{prestamo.CuotaMensual:N2}
                </td>
            </tr>
            <tr style='background:#fff9f0;'>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    {saldoFormato.Split(':')[0]}
                </td>
                <td style='padding:8px;border:1px solid #eee;
                           font-weight:bold;font-size:16px;color:#FF7A00;'>
                    ₡{prestamo.Monto:N2}
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
                    $"{prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre}",
                    asunto,
                    cuerpo,
                    pdfBytes,
                    nombreArchivo);

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Enviar préstamo por email", "Préstamos",
                    $"{prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre} — Monto: ₡{(prestamo.MontoOriginal > 0 ? prestamo.MontoOriginal : prestamo.CuotaMensual * prestamo.Cuotas):N2}");

                if (!enviado)
                    TempData["Error"] = "No se pudo enviar el correo. Intentá de nuevo.";
                else
                    TempData["Success"] = $"Boleta de {tipo.ToLower()} enviada exitosamente.";

                return RedirectToAction(nameof(Index), new { verTodos = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar préstamo por email ID: {Id}", id);
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
            var empleados = await (from e in _context.Empleados
                .Where(e => e.Activo && (
                    e.Nombre.ToLower().Contains(t) ||
                    e.PrimerApellido.ToLower().Contains(t) ||
                    (e.SegundoApellido != null && e.SegundoApellido.ToLower().Contains(t)) ||
                    e.Cedula.Contains(t)))
                join p in _context.Puestos on e.Puesto equals p.Nombre into puestoGroup
                from p in puestoGroup.DefaultIfEmpty()
                orderby e.PrimerApellido
                select new
                {
                    id = e.EmpleadoId,
                    nombre = (e.PrimerApellido + " " + e.SegundoApellido + " " + e.Nombre).Trim(),
                    cedula = e.Cedula,
                    puesto = p != null ? p.Codigo + " - " + e.Puesto : e.Puesto,
                    salarioBase = e.SalarioBase,
                    salarioQuincenal = Math.Round(
                        e.SalarioBase / (e.TipoPago == TipoPago.Semanal ? 4m :
                                         e.TipoPago == TipoPago.Mensual ? 1m : 2m), 2),
                    tipoPago = e.TipoPago.ToString(),
                    tieneActivo = _context.Prestamos.Any(
                        p => p.EmpleadoId == e.EmpleadoId && p.Activo)
                })
                .AsNoTracking()
                .Take(10)
                .ToListAsync();

            return Json(empleados);
        }

        // TodosLosEmpleados
        [HttpGet]
        public async Task<IActionResult> TodosLosEmpleados()
        {
            var empleados = await (from e in _context.Empleados
                .Where(e => e.Activo)
                join p in _context.Puestos on e.Puesto equals p.Nombre into puestoGroup
                from p in puestoGroup.DefaultIfEmpty()
                orderby e.PrimerApellido
                select new
                {
                    id = e.EmpleadoId,
                    nombre = (e.PrimerApellido + " " + e.SegundoApellido + " " + e.Nombre).Trim(),
                    cedula = e.Cedula,
                    puesto = p != null ? p.Codigo + " - " + e.Puesto : e.Puesto,
                    salarioBase = e.SalarioBase,
                    salarioQuincenal = Math.Round(
                        e.SalarioBase / (e.TipoPago == TipoPago.Semanal ? 4m :
                                         e.TipoPago == TipoPago.Mensual ? 1m : 2m), 2),
                    tipoPago = e.TipoPago.ToString(),
                    tieneActivo = _context.Prestamos.Any(
                        p2 => p2.EmpleadoId == e.EmpleadoId && p2.Activo)
                })
                .AsNoTracking()
                .ToListAsync();

            return Json(empleados);
        }

        [HttpGet]
        public IActionResult CalcularCuota(decimal monto, int cuotas)
        {
            if (monto <= 0 || cuotas <= 0)
                return Json(new { cuota = (decimal?)null });
            var cuota = Math.Round(monto / cuotas, 2);
            return Json(new { cuota });
        }

        // HELPERS

        private void AplicarValidaciones(PrestamoViewModel model, decimal salarioQuincenal)
        {
            if (model.EmpleadoId <= 0)
                ModelState.AddModelError("EmpleadoId", "Seleccioná un empleado válido.");

            if (model.MontoPrincipal <= 0)
                ModelState.AddModelError("MontoPrincipal", "El monto debe ser mayor a cero.");
            else if (model.MontoPrincipal > 9_999_999.99m)
                ModelState.AddModelError("MontoPrincipal", "El monto excede el límite máximo.");

            if (model.CuotasTotal <= 0)
                ModelState.AddModelError("CuotasTotal", "El número de cuotas debe ser mayor a cero.");
            else if (model.CuotasTotal > 120)
                ModelState.AddModelError("CuotasTotal", "No puede superar 120 cuotas.");

            if (model.CuotaMensual <= 0)
            {
                ModelState.AddModelError("CuotaMensual", "La cuota mensual debe ser mayor a cero.");
            }
            else if (model.MontoPrincipal > 0 && model.CuotaMensual > model.MontoPrincipal)
            {
                ModelState.AddModelError("CuotaMensual",
                    "La cuota no puede ser mayor al monto del préstamo.");
            }
            else if (salarioQuincenal > 0 && model.CuotaMensual > salarioQuincenal * 0.50m)
            {
                // Art. 172 CT: las deducciones no pueden reducir el salario
                // por debajo de lo necesario. Límite prudencial: 50% del salario por período.
                ModelState.AddModelError("CuotaMensual",
                    $"La cuota (₡{model.CuotaMensual:N0}) no puede superar el 50% del salario " +
                    $"del período (₡{Math.Round(salarioQuincenal * 0.50m, 0):N0}). Art. 172 CT.");
            }

            if (model.FechaPrestamo == default)
                ModelState.AddModelError("FechaPrestamo", "La fecha es obligatoria.");
            else if (model.FechaPrestamo > DateTime.Today.AddDays(1))
                ModelState.AddModelError("FechaPrestamo", "La fecha no puede ser futura.");
            else if (model.FechaPrestamo < DateTime.Today.AddYears(-2))
                ModelState.AddModelError("FechaPrestamo",
                    "La fecha no puede ser anterior a 2 años.");
        }
    }
}