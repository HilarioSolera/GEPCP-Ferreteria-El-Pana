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
        private readonly ComprobantePlanillaService _servicioPDF;

        private const decimal DiasLey = 12m;
        private const decimal SemanasLey = 50m;
        private const decimal DiasSalarioMensual = 30m;

        public VacacionesController(
    ApplicationDbContext context,
    ILogger<VacacionesController> logger,
    AuditoriaService auditoria,
    ComprobantePlanillaService servicioPDF)
        {
            _context = context;
            _logger = logger;
            _auditoria = auditoria;
            _servicioPDF = servicioPDF;
        }

        // INDEX

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

        // CREATE

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

                // Validar traslape de fechas
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

                // Art. 153 CT CR: mínimo 50 semanas laboradas
                var diasLaborados = (DateTime.Today - empleado.FechaIngreso).TotalDays;
                if (diasLaborados < 350) // 50 semanas × 7 días
                {
                    ModelState.AddModelError(string.Empty,
                        $"El empleado no ha cumplido las 50 semanas de trabajo continuo requeridas " +
                        $"(Art. 153 CT CR). Lleva {diasLaborados:N0} días ({diasLaborados / 7:N0} semanas). " +
                        $"Faltan {350 - diasLaborados:N0} días.");
                    return View(model);
                }

                // Calcular días disponibles
                var (diasBase, diasTomados, disponibles) =
                    await CalcularDisponiblesInterno(model.EmpleadoId);

                // Art. 159 CT CR: máximo 2 fracciones por período
                var ingreso = empleado.FechaIngreso;
                var periodosCompletos = (int)((decimal)(DateTime.Today - ingreso).TotalDays / 350);
                var inicioPeriodoActual = ingreso.AddDays(periodosCompletos * 350);
                var solicitudesEnPeriodo = await _context.Vacaciones
                    .CountAsync(v => v.EmpleadoId == model.EmpleadoId &&
                                     v.Estado != EstadoVacacion.Rechazada &&
                                     v.FechaInicio >= inicioPeriodoActual);
                if (solicitudesEnPeriodo >= 2)
                {
                    ModelState.AddModelError(string.Empty,
                        "Ya existen 2 solicitudes de vacaciones en este período. " +
                        "Art. 159 CT CR: las vacaciones pueden fraccionarse hasta en dos períodos como máximo.");
                    return View(model);
                }

                // Validar días al aprobar
                if (model.Estado == EstadoVacacion.Aprobada &&
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
                model.MontoDeducido = 0;

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

        // EDIT

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

                // Validar traslape (excluyendo el registro actual)
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

                // Validar días al aprobar
                if (model.Estado == EstadoVacacion.Aprobada)
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

                // Art. 159 CT CR: máximo 2 fracciones por período
                var empEdit = await _context.Empleados.FindAsync(model.EmpleadoId);
                if (empEdit != null)
                {
                    var ingresoEdit = empEdit.FechaIngreso;
                    var periodosEdit = (int)((decimal)(DateTime.Today - ingresoEdit).TotalDays / 350);
                    var inicioPerEdit = ingresoEdit.AddDays(periodosEdit * 350);
                    var solicitudesEdit = await _context.Vacaciones
                        .CountAsync(v => v.EmpleadoId == model.EmpleadoId &&
                                         v.Estado != EstadoVacacion.Rechazada &&
                                         v.VacacionId != id &&
                                         v.FechaInicio >= inicioPerEdit);
                    if (solicitudesEdit >= 2)
                    {
                        ModelState.AddModelError(string.Empty,
                            "Ya existen 2 solicitudes en este período (Art. 159 CT CR: máximo 2 fracciones).");
                        var (db4, dt4, disp4) = await CalcularDisponiblesInterno(model.EmpleadoId, id);
                        ViewBag.EmpleadoNombre = $"{empEdit.PrimerApellido} {empEdit.Nombre}";
                        ViewBag.EmpleadoCedula = empEdit.Cedula;
                        ViewBag.SalarioDiario = registro.SalarioDiario;
                        ViewBag.DiasBase = db4;
                        ViewBag.DiasTomados = dt4;
                        ViewBag.DiasDisponibles = disp4;
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
                registro.MontoDeducido = 0;

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

        // DELETE

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

        // API: Buscar empleados

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

        // API: Calcular días disponibles

      [HttpGet]
        // API: Calcular días entre fechas

        [HttpGet]
        public IActionResult CalcularDias(string fechaInicio, string fechaFin)
        {
            try
            {
                if (!DateTime.TryParse(fechaInicio, out var fi) ||
                    !DateTime.TryParse(fechaFin, out var ff))
                    return Json(new { ok = false, dias = 0, error = "" });

                if (ff < fi)
                    return Json(new { ok = false, dias = 0, error = "La fecha de fin debe ser posterior a la de inicio." });

                // Validar que no sean fines de semana
                var errores = new List<string>();
                if (fi.DayOfWeek == DayOfWeek.Saturday || fi.DayOfWeek == DayOfWeek.Sunday)
                    errores.Add("La fecha de inicio no puede ser sábado ni domingo.");
                if (ff.DayOfWeek == DayOfWeek.Saturday || ff.DayOfWeek == DayOfWeek.Sunday)
                    errores.Add("La fecha de fin no puede ser sábado ni domingo.");
                if (errores.Any())
                    return Json(new { ok = false, dias = 0, error = string.Join(" ", errores) });

                // Art. 153 CT CR: siempre días hábiles (lun-vie)
                int dias = ContarDiasHabiles(fi, ff);

                // Validar máximo 12 días
                if (dias > (int)DiasLey)
                    return Json(new { ok = true, dias, error = $"Máximo {DiasLey} días hábiles por período (Art. 153 CT CR)." });

                return Json(new { ok = true, dias, error = "" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular días");
                return Json(new { ok = false, dias = 0, error = "Error al calcular." });
            }
        }

        // HELPERS

        // HELPER: Calcular disponibles
        private async Task<(decimal diasBase, decimal diasTomados, decimal disponibles)>
            CalcularDisponiblesInterno(int empleadoId, int? excluirId = null)
        {
            var empleado = await _context.Empleados
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId);

            if (empleado == null) return (0, 0, 0);

            // Regla CR: 12 días de vacaciones por cada período de 50 semanas (350 días)
            var hoy = DateTime.Today;
            var ingreso = empleado.FechaIngreso;
            var diasTrabajados = (decimal)(hoy - ingreso).TotalDays;

            // Determinar inicio del período actual (cada 350 días = 50 semanas)
            var periodosCompletos = (int)(diasTrabajados / 350);
            var inicioPeriodoActual = ingreso.AddDays(periodosCompletos * 350);

            // Días base por período = 12
            var diasBase = DiasLey;

            // Vacaciones tomadas solo en el período actual
            var query = _context.Vacaciones
                .Where(v => v.EmpleadoId == empleadoId &&
                            v.Estado == EstadoVacacion.Aprobada &&
                            v.Tipo == TipoVacacion.ConPago &&
                            v.FechaInicio >= inicioPeriodoActual);

            if (excluirId.HasValue)
                query = query.Where(v => v.VacacionId != excluirId.Value);

            var diasTomados = (await query
                .Select(v => v.DiasHabiles)
                .ToListAsync()).Sum();

            var disponibles = Math.Max(0, diasBase - diasTomados);

            return (diasBase, diasTomados, disponibles);
        }

        // API: Calcular días disponibles
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

                // Historial de vacaciones por período
                var ingreso = empleado.FechaIngreso;
                var hoy = DateTime.Today;
                var diasTrabajados = (decimal)(hoy - ingreso).TotalDays;
                var periodosCompletos = (int)(diasTrabajados / 350);

                var todasVacaciones = await _context.Vacaciones
                    .Where(v => v.EmpleadoId == empleadoId &&
                                v.Estado == EstadoVacacion.Aprobada &&
                                v.Tipo == TipoVacacion.ConPago)
                    .OrderBy(v => v.FechaInicio)
                    .Select(v => new { v.FechaInicio, v.FechaFin, v.DiasHabiles })
                    .ToListAsync();

                var historialPeriodos = new List<object>();
                for (int i = 0; i <= periodosCompletos; i++)
                {
                    var inicioPer = ingreso.AddDays(i * 350);
                    var finPer = ingreso.AddDays((i + 1) * 350);
                    var vacsPeriodo = todasVacaciones
                        .Where(v => v.FechaInicio >= inicioPer && v.FechaInicio < finPer)
                        .ToList();
                    var diasDisfrutados = vacsPeriodo.Sum(v => v.DiasHabiles);

                    historialPeriodos.Add(new
                    {
                        periodo = i + 1,
                        inicio = inicioPer.ToString("dd/MM/yyyy"),
                        fin = finPer.ToString("dd/MM/yyyy"),
                        diasBase = DiasLey,
                        diasDisfrutados,
                        diasRestantes = Math.Max(0, DiasLey - diasDisfrutados),
                        esPeriodoActual = i == periodosCompletos
                    });
                }

                return Json(new
                {
                    ok = true,
                    diasBase,
                    diasTomados,
                    disponibles,
                    resumen = $"{empleado.PrimerApellido} {empleado.Nombre} " +
                                    $"tiene {disponibles:N0} día(s) de vacaciones disponibles " +
                                    $"(período actual: {DiasLey:N0} días, " +
                                    $"{diasTomados} día(s) disfrutados)",
                    historialPeriodos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular disponibles");
                return Json(new { ok = false });
            }
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

            // Art. 153 CT CR: vacaciones son días hábiles, no pueden iniciar/terminar en fin de semana
            if (model.FechaInicio != default &&
                (model.FechaInicio.DayOfWeek == DayOfWeek.Saturday || model.FechaInicio.DayOfWeek == DayOfWeek.Sunday))
                ModelState.AddModelError("FechaInicio",
                    "La fecha de inicio no puede ser sábado ni domingo (Art. 153 CT CR: días hábiles).");

            if (model.FechaFin != default &&
                (model.FechaFin.DayOfWeek == DayOfWeek.Saturday || model.FechaFin.DayOfWeek == DayOfWeek.Sunday))
                ModelState.AddModelError("FechaFin",
                    "La fecha de fin no puede ser sábado ni domingo (Art. 153 CT CR: días hábiles).");

            // Art. 153 CT CR: máximo 12 días hábiles por período de 50 semanas
            if (model.DiasHabiles > DiasLey)
                ModelState.AddModelError("DiasHabiles",
                    $"No se pueden solicitar más de {DiasLey} días por período (Art. 153 CT CR).");

            // Días deben ser enteros (la ley no contempla medios días)
            if (model.DiasHabiles > 0 && model.DiasHabiles != Math.Floor(model.DiasHabiles))
                ModelState.AddModelError("DiasHabiles",
                    "Los días de vacaciones deben ser enteros (Art. 153 CT CR).");

            // Validar que los días hábiles coincidan con el rango de fechas
            if (model.FechaInicio != default && model.FechaFin != default && model.FechaFin >= model.FechaInicio)
            {
                var diasReales = ContarDiasHabiles(model.FechaInicio, model.FechaFin);
                if (model.DiasHabiles > diasReales)
                    ModelState.AddModelError("DiasHabiles",
                        $"Los días solicitados ({model.DiasHabiles}) superan los días hábiles del rango seleccionado ({diasReales}).");
            }
        }

        /// <summary>
        /// Cuenta días hábiles (lun-vie) entre dos fechas inclusivas.
        /// </summary>
        private static int ContarDiasHabiles(DateTime inicio, DateTime fin)
        {
            int dias = 0;
            var actual = inicio;
            while (actual <= fin)
            {
                if (actual.DayOfWeek != DayOfWeek.Saturday && actual.DayOfWeek != DayOfWeek.Sunday)
                    dias++;
                actual = actual.AddDays(1);
            }
            return dias;
        }

        // BOLETA PDF
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

        // ENVIAR PDF POR EMAIL

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarPorEmail(int id)
        {
            try
            {
                var vacacion = await _context.Vacaciones
                    .Include(v => v.Empleado)
                    .FirstOrDefaultAsync(v => v.VacacionId == id);

                if (vacacion == null)
                {
                    TempData["Error"] = "Vacación no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                var correo = vacacion.Empleado.CorreoElectronico;
                if (string.IsNullOrWhiteSpace(correo))
                {
                    TempData["Error"] =
                        $"{vacacion.Empleado.PrimerApellido} " +
                        $"{vacacion.Empleado.Nombre} no tiene correo registrado.";
                    return RedirectToAction(nameof(Index));
                }

                var (diasBase, diasTomados, disponibles) =
                    await CalcularDisponiblesInterno(vacacion.EmpleadoId);

                var emisor = HttpContext.Session.GetString("Usuario") ?? "";

                var pdfBytes = _servicioPDF.GenerarBoletaVacacionesSinFirmas(
                    vacacion, diasBase, diasTomados, disponibles, emisor);

                var nombreArchivo =
                    $"Boleta_Vacaciones_{vacacion.Empleado.PrimerApellido}_" +
                    $"{vacacion.FechaInicio:yyyyMMdd}.pdf";

                var emailSvc = HttpContext.RequestServices
                    .GetRequiredService<EmailService>();

                var tipoVacacion = vacacion.Tipo == TipoVacacion.ConPago ? "Con Pago" : "Sin Pago";
                var asunto = $"Boleta de Vacaciones — {tipoVacacion} — {vacacion.FechaInicio:dd/MM/yyyy}";
                var cuerpo = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;'>
    <div style='background:#1A1A2E;padding:20px;'>
        <h2 style='color:#FF7A00;margin:0;'>Ferretería El Pana SRL</h2>
        <p style='color:#888;margin:4px 0 0;font-size:13px;'>
            Departamento de Recursos Humanos
        </p>
    </div>
    <div style='padding:20px;border:1px solid #eee;'>
        <p>Estimado(a) <strong>{vacacion.Empleado.PrimerApellido}
           {vacacion.Empleado.Nombre}</strong>,</p>
        <p>Adjunto encontrará su boleta de vacaciones correspondiente al
           período <strong>{vacacion.FechaInicio:dd/MM/yyyy} - {vacacion.FechaFin:dd/MM/yyyy}</strong>.</p>
        <table style='width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;'>
            <tr style='background:#f9f9f9;'>
                <td style='padding:8px;border:1px solid #eee;'>Período</td>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    {vacacion.FechaInicio:dd/MM/yyyy} - {vacacion.FechaFin:dd/MM/yyyy}
                </td>
            </tr>
            <tr>
                <td style='padding:8px;border:1px solid #eee;'>Tipo</td>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    {tipoVacacion}
                </td>
            </tr>
            <tr>
                <td style='padding:8px;border:1px solid #eee;'>Días</td>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    {vacacion.DiasHabiles}
                </td>
            </tr>
            <tr style='background:#fff9f0;'>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    Deducción
                </td>
                <td style='padding:8px;border:1px solid #eee;
                           font-weight:bold;font-size:16px;color:#FF7A00;'>
                    ₡{vacacion.MontoDeducido:N2}
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
                    $"{vacacion.Empleado.PrimerApellido} {vacacion.Empleado.Nombre}",
                    asunto, cuerpo, pdfBytes, nombreArchivo);

                await _auditoria.RegistrarAsync(
                    emisor, "Enviar boleta por email", "Vacaciones",
                    $"{vacacion.Empleado.PrimerApellido} {vacacion.Empleado.Nombre} " +
                    $"→ {correo}");

                TempData[enviado ? "Success" : "Error"] = enviado
                    ? $"Boleta enviada a {correo}."
                    : "Error al enviar el correo. Verificá la configuración SMTP.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email vacación ID: {Id}", id);
                TempData["Error"] = "Error al enviar el correo. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

    }
}