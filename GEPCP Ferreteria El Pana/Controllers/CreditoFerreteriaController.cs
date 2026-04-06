using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public CreditoFerreteriaController(
            ApplicationDbContext context,
            ILogger<CreditoFerreteriaController> logger,
            AuditoriaService auditoria)
        {
            _context = context;
            _logger = logger;
            _auditoria = auditoria;
        }

        // ── INDEX ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(string? busqueda, string? estado)
        {
            try
            {
                ViewBag.Busqueda = busqueda;
                ViewBag.EstadoFiltro = estado;
                ViewBag.TotalCreditos = 0;
                ViewBag.TotalActivos = 0;
                ViewBag.TotalSaldo = 0m;
                ViewBag.TotalCuotasQuinc = 0m;

                if (string.IsNullOrWhiteSpace(busqueda) && string.IsNullOrWhiteSpace(estado))
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
                    var termino = busqueda.Trim().ToLower();
                    query = query.Where(c =>
                        c.Empleado.Nombre.ToLower().Contains(termino) ||
                        c.Empleado.PrimerApellido.ToLower().Contains(termino) ||
                        c.Empleado.Cedula.Contains(termino) ||
                        c.Descripcion.ToLower().Contains(termino));
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

        // ── CREATE ────────────────────────────────────────────────────────────

        public async Task<IActionResult> Create()
        {
            try
            {
                await CargarEmpleadosViewBag();
                return View(new CreditoFerreteria { FechaCredito = DateTime.Today });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de crédito");
                TempData["Error"] = "Error al cargar el formulario. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreditoFerreteria model)
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

                model.Saldo = Math.Round(model.MontoTotal, 2);
                model.MontoTotal = Math.Round(model.MontoTotal, 2);
                model.CuotaQuincenal = Math.Round(model.CuotaQuincenal, 2);
                model.Activo = true;

                _context.Add(model);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Crear crédito ferretería", "Créditos",
                    $"EmpleadoId: {model.EmpleadoId} — Monto: ₡{model.MontoTotal:N0} — Cuota: ₡{model.CuotaQuincenal:N0}");

                _logger.LogInformation("Crédito creado: EmpleadoId {EId} Monto {M}",
                    model.EmpleadoId, model.MontoTotal);

                TempData["Success"] = $"Crédito de ₡{model.MontoTotal:N0} registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al crear crédito");
                ModelState.AddModelError(string.Empty, "Error al guardar. Intentá de nuevo.");
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear crédito");
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }
        }

        // ── REGISTRAR ABONO ───────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAbono(int creditoId, decimal monto)
        {
            try
            {
                if (monto <= 0)
                {
                    TempData["Error"] = "El monto del abono debe ser mayor a cero.";
                    return RedirectToAction(nameof(Index));
                }

                if (monto > 9_999_999.99m)
                {
                    TempData["Error"] = "El monto del abono excede el límite permitido.";
                    return RedirectToAction(nameof(Index));
                }

                var credito = await _context.CreditosFerreteria
                    .Include(c => c.Empleado)
                    .FirstOrDefaultAsync(c => c.CreditoFerreteriaId == creditoId);

                if (credito == null)
                {
                    TempData["Error"] = "Crédito no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if (!credito.Activo)
                {
                    TempData["Error"] = "Este crédito ya está saldado.";
                    return RedirectToAction(nameof(Index));
                }

                if (monto > credito.Saldo)
                {
                    TempData["Error"] = $"El abono (₡{monto:N0}) no puede superar el saldo (₡{credito.Saldo:N0}).";
                    return RedirectToAction(nameof(Index));
                }

                var saldoAnterior = credito.Saldo;
                credito.Saldo = Math.Round(credito.Saldo - monto, 2);

                if (credito.Saldo <= 0)
                {
                    credito.Saldo = 0;
                    credito.Activo = false;
                    TempData["Success"] = $"Abono de ₡{monto:N0} registrado. " +
                        $"¡Crédito de {credito.Empleado.PrimerApellido} {credito.Empleado.Nombre} saldado!";
                }
                else
                {
                    TempData["Success"] = $"Abono de ₡{monto:N0} registrado. Saldo restante: ₡{credito.Saldo:N0}.";
                }

                // Registrar historial del abono
                _context.AbonosCreditoFerreteria.Add(new AbonoCreditoFerreteria
                {
                    CreditoFerreteriaId = creditoId,
                    Monto = Math.Round(monto, 2),
                    FechaAbono = DateTime.Now,
                    Observaciones = $"Abono manual — Saldo anterior: ₡{saldoAnterior:N0}"
                });

                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Registrar abono crédito", "Créditos",
                    $"CreditoId: {creditoId} — {credito.Empleado.PrimerApellido} {credito.Empleado.Nombre} — Monto: ₡{monto:N0} — Saldo: ₡{credito.Saldo:N0}");

                _logger.LogInformation("Abono crédito: CreditoId {Id} Monto {M} Saldo {S}",
                    creditoId, monto, credito.Saldo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar abono. CreditoId: {Id}", creditoId);
                TempData["Error"] = "Ocurrió un error al registrar el abono. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── CERRAR ────────────────────────────────────────────────────────────

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
                    return RedirectToAction(nameof(Index));
                }

                if (!credito.Activo)
                {
                    TempData["Error"] = "Este crédito ya estaba cerrado.";
                    return RedirectToAction(nameof(Index));
                }

                credito.Activo = false;
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Cerrar crédito manual", "Créditos",
                    $"{credito.Empleado.PrimerApellido} {credito.Empleado.Nombre} — Saldo: ₡{credito.Saldo:N0}");

                _logger.LogInformation("Crédito cerrado: ID {Id}", id);
                TempData["Success"] = $"Crédito de {credito.Empleado.PrimerApellido} " +
                    $"{credito.Empleado.Nombre} cerrado manualmente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar crédito ID: {Id}", id);
                TempData["Error"] = "Error al cerrar el crédito. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── REABRIR ───────────────────────────────────────────────────────────

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
                    return RedirectToAction(nameof(Index));
                }

                if (credito.Activo)
                {
                    TempData["Error"] = "Este crédito ya está activo.";
                    return RedirectToAction(nameof(Index));
                }

                credito.Activo = true;
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Reabrir crédito", "Créditos",
                    $"{credito.Empleado.PrimerApellido} {credito.Empleado.Nombre} — Saldo: ₡{credito.Saldo:N0}");

                _logger.LogInformation("Crédito reabierto: ID {Id}", id);
                TempData["Success"] = $"Crédito de {credito.Empleado.PrimerApellido} " +
                    $"{credito.Empleado.Nombre} reabierto correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reabrir crédito ID: {Id}", id);
                TempData["Error"] = "Error al reabrir el crédito. Intentá de nuevo.";
            }

            return RedirectToAction(nameof(Index));
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

        private void AplicarValidaciones(CreditoFerreteria model)
        {
            if (model.EmpleadoId <= 0)
                ModelState.AddModelError("EmpleadoId", "Seleccioná un empleado válido.");

            if (model.MontoTotal <= 0)
                ModelState.AddModelError("MontoTotal", "El monto debe ser mayor a cero.");
            else if (model.MontoTotal > 9_999_999.99m)
                ModelState.AddModelError("MontoTotal", "El monto excede el límite máximo permitido.");

            if (model.CuotaQuincenal <= 0)
                ModelState.AddModelError("CuotaQuincenal", "La cuota quincenal debe ser mayor a cero.");
            else if (model.CuotaQuincenal > model.MontoTotal && model.MontoTotal > 0)
                ModelState.AddModelError("CuotaQuincenal", "La cuota no puede ser mayor al monto total.");

            if (model.FechaCredito == default)
                ModelState.AddModelError("FechaCredito", "La fecha es obligatoria.");
            else if (model.FechaCredito > DateTime.Today.AddDays(1))
                ModelState.AddModelError("FechaCredito", "La fecha no puede ser futura.");
            else if (model.FechaCredito < DateTime.Today.AddYears(-2))
                ModelState.AddModelError("FechaCredito", "La fecha no puede ser anterior a 2 años.");

            if (string.IsNullOrWhiteSpace(model.Descripcion))
                ModelState.AddModelError("Descripcion", "La descripción es obligatoria.");
            else if (model.Descripcion.Trim().Length < 5)
                ModelState.AddModelError("Descripcion", "La descripción debe tener al menos 5 caracteres.");
            else if (model.Descripcion.Trim().Length > 200)
                ModelState.AddModelError("Descripcion", "La descripción no puede superar 200 caracteres.");
        }
    }
}