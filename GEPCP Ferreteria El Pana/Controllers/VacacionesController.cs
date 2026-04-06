using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class VacacionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VacacionesController> _logger;
        private readonly AuditoriaService _auditoria;
        private readonly ComprobantePlanillaService _servicioPDF; // ← agregá esta línea

        private const decimal DiasLey = 14m;
        private const decimal SemanasLey = 50m;
        private const decimal DiasSalarioMensual = 30m;

        public VacacionesController(
    ApplicationDbContext context,
    ILogger<VacacionesController> logger,
    AuditoriaService auditoria,
    ComprobantePlanillaService servicioPDF) // ← agregá este parámetro
        {
            _context = context;
            _logger = logger;
            _auditoria = auditoria;
            _servicioPDF = servicioPDF;             // ← y esta línea
        }

        // ── INDEX ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(string? busqueda, string? estado, bool verTodos = false)
        {
            try
            {
                ViewBag.Busqueda = busqueda;
                ViewBag.Estado = estado;
                ViewBag.VerTodos = verTodos;

                var query = _context.Vacaciones
                    .Include(v => v.Empleado)
                    .AsNoTracking()
                    .AsQueryable();

                if (!verTodos && string.IsNullOrWhiteSpace(busqueda) && string.IsNullOrWhiteSpace(estado))
                    return View(new List<Vacacion>());

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var t = busqueda.Trim().ToLower();
                    query = query.Where(v =>
                        v.Empleado.Nombre.ToLower().Contains(t) ||
                        v.Empleado.PrimerApellido.ToLower().Contains(t) ||
                        v.Empleado.Cedula.Contains(t));
                }

                if (!string.IsNullOrWhiteSpace(estado) &&
                    Enum.TryParse<EstadoVacacion>(estado, out var estadoEnum))
                    query = query.Where(v => v.Estado == estadoEnum);

                var vacaciones = await query
                    .OrderByDescending(v => v.FechaInicio)
                    .ThenBy(v => v.Empleado.PrimerApellido)
                    .ToListAsync();

                ViewBag.TotalRegistros = vacaciones.Count;
                ViewBag.TotalDias = vacaciones.Sum(v => v.DiasHabiles);
                ViewBag.TotalMonto = vacaciones.Sum(v => v.MontoDeducido);
                ViewBag.TotalPendientes = vacaciones.Count(v => v.Estado == EstadoVacacion.Pendiente);

                return View(vacaciones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar vacaciones");
                TempData["Error"] = "Error al cargar las vacaciones.";
                return View(new List<Vacacion>());
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public IActionResult Create()
        {
            return View(new Vacacion
            {
                FechaInicio = DateTime.Today,
                FechaFin = DateTime.Today.AddDays(13),
                Tipo = TipoVacacion.ConPago,
                Estado = EstadoVacacion.Pendiente
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vacacion model)
        {
            try
            {
                ModelState.Remove("Empleado");
                ModelState.Remove("RegistradoPor");
                AplicarValidaciones(model);

                if (!ModelState.IsValid)
                    return View(model);

                var empleado = await _context.Empleados.FindAsync(model.EmpleadoId);
                if (empleado == null)
                {
                    ModelState.AddModelError("EmpleadoId", "Empleado no encontrado.");
                    return View(model);
                }

                // ── Validar traslape de fechas ────────────────────────────────
                var traslape = await _context.Vacaciones
                    .Where(v => v.EmpleadoId == model.EmpleadoId &&
                                v.Estado != EstadoVacacion.Rechazada &&
                                v.FechaInicio <= model.FechaFin &&
                                v.FechaFin >= model.FechaInicio)
                    .FirstOrDefaultAsync();

                if (traslape != null)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Ya existe una vacación que se traslapa con esas fechas: " +
                        $"{traslape.FechaInicio:dd/MM/yyyy} al {traslape.FechaFin:dd/MM/yyyy} " +
                        $"({traslape.Estado}). Ajustá las fechas.");
                    return View(model);
                }

                // ── Calcular días disponibles ─────────────────────────────────
                var (diasBase, diasTomados, disponibles) =
                    await CalcularDisponiblesInterno(model.EmpleadoId);

                // ── Validar días al aprobar ───────────────────────────────────
                if (model.Estado == EstadoVacacion.Aprobada &&
                    model.Tipo == TipoVacacion.ConPago &&
                    model.DiasHabiles > disponibles)
                {
                    ModelState.AddModelError("DiasHabiles",
                        $"No se puede aprobar: los días solicitados ({model.DiasHabiles}) " +
                        $"superan los días disponibles ({disponibles}). " +
                        $"Cambiá el estado a Pendiente o reducí los días.");
                    return View(model);
                }

                model.DiasDisponiblesAlRegistrar = disponibles;
                model.SalarioDiario = Math.Round(empleado.SalarioBase / DiasSalarioMensual, 2);
                model.RegistradoPor = HttpContext.Session.GetString("Usuario") ?? "";
                model.FechaRegistro = DateTime.Now;
                model.MontoDeducido = model.Tipo == TipoVacacion.SinPago
                    ? Math.Round(model.DiasHabiles * model.SalarioDiario, 2)
                    : 0;

                _context.Add(model);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    model.RegistradoPor, "Registrar vacación", "Vacaciones",
                    $"{empleado.PrimerApellido} {empleado.Nombre} — " +
                    $"{model.DiasHabiles} días — {model.Tipo} — {model.Estado}");

                TempData["Success"] = $"Vacación de {empleado.PrimerApellido} {empleado.Nombre} " +
                    $"registrada. {model.DiasHabiles} día(s) — {model.Tipo}.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear vacación");
                ModelState.AddModelError(string.Empty, "Error inesperado. Intentá de nuevo.");
                return View(model);
            }
        }

        // ── EDIT ──────────────────────────────────────────────────────────────

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var vacacion = await _context.Vacaciones
                .Include(v => v.Empleado)
                .FirstOrDefaultAsync(v => v.VacacionId == id);

            if (vacacion == null) return NotFound();

            var (diasBase, diasTomados, disponibles) =
                await CalcularDisponiblesInterno(vacacion.EmpleadoId, id);

            ViewBag.EmpleadoNombre = $"{vacacion.Empleado.PrimerApellido} " +
                $"{vacacion.Empleado.SegundoApellido} {vacacion.Empleado.Nombre}".Trim();
            ViewBag.EmpleadoCedula = vacacion.Empleado.Cedula;
            ViewBag.SalarioDiario = vacacion.SalarioDiario;
            ViewBag.DiasBase = diasBase;
            ViewBag.DiasTomados = diasTomados;
            ViewBag.DiasDisponibles = disponibles;

            return View(vacacion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Vacacion model)
        {
            try
            {
                if (id != model.VacacionId) return NotFound();

                ModelState.Remove("Empleado");
                ModelState.Remove("RegistradoPor");
                AplicarValidaciones(model);

                if (!ModelState.IsValid)
                {
                    var emp = await _context.Empleados.FindAsync(model.EmpleadoId);
                    var (db, dt, disp) = await CalcularDisponiblesInterno(model.EmpleadoId, id);
                    ViewBag.EmpleadoNombre = $"{emp?.PrimerApellido} {emp?.Nombre}";
                    ViewBag.EmpleadoCedula = emp?.Cedula;
                    ViewBag.SalarioDiario = model.SalarioDiario;
                    ViewBag.DiasBase = db;
                    ViewBag.DiasTomados = dt;
                    ViewBag.DiasDisponibles = disp;
                    return View(model);
                }

                var registro = await _context.Vacaciones
                    .Include(v => v.Empleado)
                    .FirstOrDefaultAsync(v => v.VacacionId == id);

                if (registro == null) return NotFound();

                // ── Validar traslape (excluyendo el registro actual) ───────────
                var traslape = await _context.Vacaciones
                    .Where(v => v.EmpleadoId == model.EmpleadoId &&
                                v.Estado != EstadoVacacion.Rechazada &&
                                v.VacacionId != id &&
                                v.FechaInicio <= model.FechaFin &&
                                v.FechaFin >= model.FechaInicio)
                    .FirstOrDefaultAsync();

                if (traslape != null)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Las fechas se solapan con otra vacación existente: " +
                        $"{traslape.FechaInicio:dd/MM/yyyy} al {traslape.FechaFin:dd/MM/yyyy} " +
                        $"({traslape.Estado}). Ajustá las fechas.");

                    var emp2 = await _context.Empleados.FindAsync(model.EmpleadoId);
                    var (db2, dt2, disp2) = await CalcularDisponiblesInterno(model.EmpleadoId, id);
                    ViewBag.EmpleadoNombre = $"{emp2?.PrimerApellido} {emp2?.Nombre}";
                    ViewBag.EmpleadoCedula = emp2?.Cedula;
                    ViewBag.SalarioDiario = registro.SalarioDiario;
                    ViewBag.DiasBase = db2;
                    ViewBag.DiasTomados = dt2;
                    ViewBag.DiasDisponibles = disp2;
                    return View(model);
                }

                // ── Validar días al aprobar ───────────────────────────────────
                if (model.Estado == EstadoVacacion.Aprobada &&
                    model.Tipo == TipoVacacion.ConPago)
                {
                    var (_, _, disponibles) = await CalcularDisponiblesInterno(model.EmpleadoId, id);
                    if (model.DiasHabiles > disponibles)
                    {
                        ModelState.AddModelError("DiasHabiles",
                            $"No se puede aprobar: los días solicitados ({model.DiasHabiles}) " +
                            $"superan los días disponibles ({disponibles}).");

                        var emp3 = await _context.Empleados.FindAsync(model.EmpleadoId);
                        var (db3, dt3, disp3) = await CalcularDisponiblesInterno(model.EmpleadoId, id);
                        ViewBag.EmpleadoNombre = $"{emp3?.PrimerApellido} {emp3?.Nombre}";
                        ViewBag.EmpleadoCedula = emp3?.Cedula;
                        ViewBag.SalarioDiario = registro.SalarioDiario;
                        ViewBag.DiasBase = db3;
                        ViewBag.DiasTomados = dt3;
                        ViewBag.DiasDisponibles = disp3;
                        return View(model);
                    }
                }

                var estadoAnterior = registro.Estado;
                var diasAnteriores = registro.DiasHabiles;
                var tipoAnterior = registro.Tipo;

                registro.FechaInicio = model.FechaInicio;
                registro.FechaFin = model.FechaFin;
                registro.DiasHabiles = model.DiasHabiles;
                registro.Tipo = model.Tipo;
                registro.Estado = model.Estado;
                registro.Observaciones = model.Observaciones;
                registro.DiasDisponiblesAlRegistrar = model.DiasDisponiblesAlRegistrar;
                registro.MontoDeducido = registro.Tipo == TipoVacacion.SinPago
                    ? Math.Round(registro.DiasHabiles * registro.SalarioDiario, 2)
                    : 0;

                _context.Update(registro);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Editar vacación", "Vacaciones",
                    $"{registro.Empleado.PrimerApellido} {registro.Empleado.Nombre} — " +
                    $"Estado: {estadoAnterior} → {registro.Estado}");

                var partes = new List<string>();
                if (registro.Estado != estadoAnterior) partes.Add($"Estado: {estadoAnterior} → {registro.Estado}");
                if (registro.DiasHabiles != diasAnteriores) partes.Add($"Días: {diasAnteriores} → {registro.DiasHabiles}");
                if (registro.Tipo != tipoAnterior) partes.Add($"Tipo: {tipoAnterior} → {registro.Tipo}");

                TempData["Success"] = partes.Any()
                    ? string.Join(" — ", partes)
                    : "Vacación actualizada sin cambios.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar vacación ID: {Id}", id);
                ModelState.AddModelError(string.Empty, "Error inesperado. Intentá de nuevo.");
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
                var registro = await _context.Vacaciones
                    .Include(v => v.Empleado)
                    .FirstOrDefaultAsync(v => v.VacacionId == id);

                if (registro == null)
                {
                    TempData["Error"] = "Registro no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if (registro.Estado == EstadoVacacion.Aprobada)
                {
                    TempData["Error"] = "No se puede eliminar una vacación aprobada " +
                        "(Art. 156 Código de Trabajo CR).";
                    return RedirectToAction(nameof(Index));
                }

                _context.Vacaciones.Remove(registro);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Eliminar vacación", "Vacaciones",
                    $"{registro.Empleado.PrimerApellido} {registro.Empleado.Nombre} — " +
                    $"{registro.DiasHabiles} días");

                TempData["Success"] = "Vacación eliminada.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar vacación ID: {Id}", id);
                TempData["Error"] = "Error al eliminar. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── API: Buscar empleados ─────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BuscarEmpleados(string? termino)
        {
            try
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
                        salarioDiario = Math.Round(e.SalarioBase / 30, 2),
                        fechaIngreso = e.FechaIngreso.ToString("yyyy-MM-dd")
                    })
                    .ToListAsync();

                return Json(empleados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar empleados");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> TodosLosEmpleados()
        {
            try
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
                        salarioDiario = Math.Round(e.SalarioBase / 30, 2),
                        fechaIngreso = e.FechaIngreso.ToString("yyyy-MM-dd")
                    })
                    .ToListAsync();

                return Json(empleados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar empleados");
                return Json(new List<object>());
            }
        }

        // ── API: Calcular días disponibles ────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> CalcularDisponibles(int empleadoId)
        {
            try
            {
                var empleado = await _context.Empleados
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId);

                if (empleado == null)
                    return Json(new { ok = false });

                var (diasBase, diasTomados, disponibles) =
                    await CalcularDisponiblesInterno(empleadoId);

                var salarioDiario = Math.Round(empleado.SalarioBase / DiasSalarioMensual, 2);
                var semanas = (decimal)(DateTime.Today - empleado.FechaIngreso).TotalDays / 7;
                var antiguedad = (DateTime.Today - empleado.FechaIngreso).TotalDays / 365;

                return Json(new
                {
                    ok = true,
                    diasBase,
                    diasTomados,
                    disponibles,
                    salarioDiario,
                    semanas = Math.Round(semanas, 1),
                    antiguedad = Math.Round((decimal)antiguedad, 1)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular disponibles");
                return Json(new { ok = false });
            }
        }

        // ── API: Calcular días entre fechas ───────────────────────────────────

        [HttpGet]
        public IActionResult CalcularDias(string fechaInicio, string fechaFin, bool excluirFds = true)
        {
            try
            {
                if (!DateTime.TryParse(fechaInicio, out var fi) ||
                    !DateTime.TryParse(fechaFin, out var ff))
                    return Json(new { ok = false, dias = 0 });

                if (ff < fi)
                    return Json(new { ok = false, dias = 0 });

                decimal dias = 0;
                var actual = fi;

                while (actual <= ff)
                {
                    if (!excluirFds ||
                        (actual.DayOfWeek != DayOfWeek.Saturday &&
                         actual.DayOfWeek != DayOfWeek.Sunday))
                        dias++;
                    actual = actual.AddDays(1);
                }

                return Json(new { ok = true, dias });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular días");
                return Json(new { ok = false, dias = 0 });
            }
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private async Task<(decimal diasBase, decimal diasTomados, decimal disponibles)>
            CalcularDisponiblesInterno(int empleadoId, int? excluirId = null)
        {
            var empleado = await _context.Empleados
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId);

            if (empleado == null) return (0, 0, 0);

            var semanas = (decimal)(DateTime.Today - empleado.FechaIngreso).TotalDays / 7;
            var periodos = Math.Floor(semanas / SemanasLey);
            var diasBase = periodos * DiasLey;

            var query = _context.Vacaciones
                .Where(v => v.EmpleadoId == empleadoId &&
                            v.Estado == EstadoVacacion.Aprobada &&
                            v.Tipo == TipoVacacion.ConPago);

            if (excluirId.HasValue)
                query = query.Where(v => v.VacacionId != excluirId.Value);

            var diasTomados = (await query.Select(v => v.DiasHabiles).ToListAsync()).Sum();
            var disponibles = diasBase - diasTomados;

            return (diasBase, diasTomados, disponibles);
        }

        private void AplicarValidaciones(Vacacion model)
        {
            if (model.EmpleadoId <= 0)
                ModelState.AddModelError("EmpleadoId", "Seleccioná un empleado válido.");

            if (model.FechaInicio == default)
                ModelState.AddModelError("FechaInicio", "La fecha de inicio es obligatoria.");

            if (model.FechaFin == default)
                ModelState.AddModelError("FechaFin", "La fecha de fin es obligatoria.");

            if (model.FechaInicio != default && model.FechaFin != default &&
                model.FechaFin < model.FechaInicio)
                ModelState.AddModelError("FechaFin",
                    "La fecha de fin no puede ser anterior a la de inicio.");

            if (model.DiasHabiles <= 0)
                ModelState.AddModelError("DiasHabiles",
                    "Los días deben ser mayor a cero.");
        }

        // ── BOLETA PDF ────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DescargarBoleta(int id)
        {
            try
            {
                var vacacion = await _context.Vacaciones
                    .Include(v => v.Empleado)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.VacacionId == id);

                if (vacacion == null) return NotFound();

                var (diasBase, diasTomados, disponibles) =
                    await CalcularDisponiblesInterno(vacacion.EmpleadoId);

                var emisor = HttpContext.Session.GetString("Usuario") ?? "";

                var pdfBytes = _servicioPDF.GenerarBoletaVacaciones(
                    vacacion, diasBase, diasTomados, disponibles, emisor);

                var nombreArchivo =
                    $"Boleta_Vacaciones_{vacacion.Empleado.PrimerApellido}_" +
                    $"{vacacion.FechaInicio:yyyyMMdd}.pdf";

                await _auditoria.RegistrarAsync(
                    emisor, "Descargar boleta vacaciones", "Vacaciones",
                    $"{vacacion.Empleado.PrimerApellido} {vacacion.Empleado.Nombre} — " +
                    $"{vacacion.DiasHabiles} días");

                return File(pdfBytes, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar boleta vacación ID: {Id}", id);
                TempData["Error"] = "Error al generar el PDF. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

    }
}