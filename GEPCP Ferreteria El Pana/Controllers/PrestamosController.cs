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

        // ── INDEX ─────────────────────────────────────────────────────────────

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

        // ── CREATE ────────────────────────────────────────────────────────────

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

                AplicarValidaciones(model, empleado.SalarioBase / 2m);

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

        // ── REGISTRAR ABONO ───────────────────────────────────────────────────

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

        // ── CORREGIR ABONO ────────────────────────────────────────────────────

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

        // ── ELIMINAR ABONO ────────────────────────────────────────────────────

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

        // ── ELIMINAR PRÉSTAMO CERRADO ─────────────────────────────────────────

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

        // ── CERRAR ────────────────────────────────────────────────────────────

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

        // ── DESCARGAR FINIQUITO PDF ───────────────────────────────────────────

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

                if (prestamo.Activo)
                {
                    TempData["Error"] =
                        "El finiquito solo está disponible cuando el préstamo está saldado.";
                    return RedirectToAction(nameof(Index), new { verTodos = true });
                }

                var pdfBytes = _servicioPDF.GenerarFiniquitoPrestamo(prestamo);
                var nombre =
                    $"Finiquito_Prestamo_{prestamo.Empleado.PrimerApellido}_" +
                    $"{prestamo.FechaPrestamo:yyyyMMdd}.pdf";

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Descargar finiquito préstamo", "Préstamos",
                    $"{prestamo.Empleado.PrimerApellido} {prestamo.Empleado.Nombre}");

                return File(pdfBytes, "application/pdf", nombre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar finiquito préstamo ID: {Id}", id);
                TempData["Error"] = "Error al generar el PDF. Intentá de nuevo.";
                return RedirectToAction(nameof(Index), new { verTodos = true });
            }
        }

        // ── APIs ──────────────────────────────────────────────────────────────

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
                    puesto = e.Puesto,
                    salarioBase = e.SalarioBase,
                    salarioQuincenal = Math.Round(e.SalarioBase / 2m, 2),
                    tieneActivo = _context.Prestamos.Any(
                        p => p.EmpleadoId == e.EmpleadoId && p.Activo)
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
                    salarioBase = e.SalarioBase,
                    salarioQuincenal = Math.Round(e.SalarioBase / 2m, 2),
                    tieneActivo = _context.Prestamos.Any(
                        p => p.EmpleadoId == e.EmpleadoId && p.Activo)
                })
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

        // ── HELPERS ───────────────────────────────────────────────────────────

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
            else if (salarioQuincenal > 0 && model.CuotaMensual > salarioQuincenal)
            {
                ModelState.AddModelError("CuotaMensual",
                    $"La cuota (₡{model.CuotaMensual:N0}) no puede superar el salario " +
                    $"quincenal del empleado (₡{salarioQuincenal:N0}).");
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