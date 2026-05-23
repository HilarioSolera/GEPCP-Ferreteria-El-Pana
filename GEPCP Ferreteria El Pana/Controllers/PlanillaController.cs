using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class PlanillaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlanillaController> _logger;
        private readonly ComprobantePlanillaService _servicioPDF;
        private readonly AuditoriaService _auditoria;

        public PlanillaController(
            ApplicationDbContext context,
            ILogger<PlanillaController> logger,
            ComprobantePlanillaService servicioPDF,
            AuditoriaService auditoria)
        {
            _context = context;
            _logger = logger;
            _servicioPDF = servicioPDF;
            _auditoria = auditoria;
        }

        // Calculo del impuesto sobre la renta por tramos progresivos segun ley CR
        private decimal CalcularImpuestoRenta(decimal baseImponiblePeriodo, PeriodoPago periodo,
            int numHijos = 0, bool tieneConyuge = false)
        {
            // Si el periodo tiene tramos en 0, se usan valores CR 2026 por defecto
            decimal t1Hasta = periodo.ISR_Tramo1_Hasta > 0 ? periodo.ISR_Tramo1_Hasta : 918000m;
            decimal t2Desde = periodo.ISR_Tramo2_Desde > 0 ? periodo.ISR_Tramo2_Desde : 918000m;
            decimal t2Hasta = periodo.ISR_Tramo2_Hasta > 0 ? periodo.ISR_Tramo2_Hasta : 1347000m;
            decimal t2Pct   = periodo.ISR_Tramo2_Porcentaje > 0 ? periodo.ISR_Tramo2_Porcentaje : 10m;
            decimal t3Desde = periodo.ISR_Tramo3_Desde > 0 ? periodo.ISR_Tramo3_Desde : 1347000m;
            decimal t3Hasta = periodo.ISR_Tramo3_Hasta > 0 ? periodo.ISR_Tramo3_Hasta : 2364000m;
            decimal t3Pct   = periodo.ISR_Tramo3_Porcentaje > 0 ? periodo.ISR_Tramo3_Porcentaje : 15m;
            decimal t4Desde = periodo.ISR_Tramo4_Desde > 0 ? periodo.ISR_Tramo4_Desde : 2364000m;
            decimal t4Hasta = periodo.ISR_Tramo4_Hasta > 0 ? periodo.ISR_Tramo4_Hasta : 4727000m;
            decimal t4Pct   = periodo.ISR_Tramo4_Porcentaje > 0 ? periodo.ISR_Tramo4_Porcentaje : 20m;
            decimal t5Desde = periodo.ISR_Tramo5_Desde > 0 ? periodo.ISR_Tramo5_Desde : 4727000m;
            decimal t5Pct   = periodo.ISR_Tramo5_Porcentaje > 0 ? periodo.ISR_Tramo5_Porcentaje : 25m;

            // Conversion del devengado del periodo a base mensual
            decimal factorMensual = periodo.TipoPeriodo switch
            {
                TipoPeriodo.Semanal => 52m / 12m,   // ≈ 4.333
                TipoPeriodo.Mensual => 1m,
                _ => 2m                               // Quincenal
            };

            decimal baseImponibleMensual = baseImponiblePeriodo * factorMensual;
            decimal impuestoMensual = 0m;

            // Tramo 1: exento
            if (baseImponibleMensual <= t1Hasta)
                return 0m;

            // Tramo 2
            if (baseImponibleMensual > t2Desde)
            {
                decimal excedente = Math.Min(baseImponibleMensual, t2Hasta) - t2Desde;
                impuestoMensual += excedente * (t2Pct / 100m);
            }

            // Tramo 3
            if (baseImponibleMensual > t3Desde)
            {
                decimal excedente = Math.Min(baseImponibleMensual, t3Hasta) - t3Desde;
                impuestoMensual += excedente * (t3Pct / 100m);
            }

            // Tramo 4
            if (baseImponibleMensual > t4Desde)
            {
                decimal excedente = Math.Min(baseImponibleMensual, t4Hasta) - t4Desde;
                impuestoMensual += excedente * (t4Pct / 100m);
            }

            // Tramo 5
            if (baseImponibleMensual > t5Desde)
            {
                decimal excedente = baseImponibleMensual - t5Desde;
                impuestoMensual += excedente * (t5Pct / 100m);
            }

            // Se restan los creditos fiscales por hijos y conyuge
            decimal creditoHijos = numHijos * periodo.ISR_CreditoHijo;
            decimal creditoConyuge = tieneConyuge ? periodo.ISR_CreditoConyuge : 0m;
            impuestoMensual = Math.Max(0m, impuestoMensual - creditoHijos - creditoConyuge);

            // Se divide el impuesto mensual entre el factor del periodo
            return Math.Round(impuestoMensual / factorMensual, 2);
        }



        // INDEX
        public async Task<IActionResult> Index(int? periodoId, string orden)
        {
            try
            {
                var hoy = DateTime.Today;
                var periodos = await _context.PeriodosPago
                    .AsNoTracking()
                    .Where(p => p.Anio == hoy.Year && p.Mes == hoy.Month)
                    .OrderByDescending(p => p.Quincena)
                    .ToListAsync();

                if (!periodos.Any())
                {
                    var mesAnterior = hoy.AddMonths(-1);
                    periodos = await _context.PeriodosPago
                        .AsNoTracking()
                        .Where(p => p.Anio == mesAnterior.Year && p.Mes == mesAnterior.Month)
                        .OrderByDescending(p => p.Quincena)
                        .ToListAsync();
                }

                ViewBag.Periodos = periodos;
                ViewBag.PeriodoId = periodoId;
                ViewBag.Orden = orden ?? "departamento"; // Valor por defecto

                if (periodoId == null)
                {
                    var activo = periodos.FirstOrDefault(p => p.Estado == EstadoPeriodo.Abierto)
                              ?? periodos.FirstOrDefault();
                    periodoId = activo?.PeriodoPagoId;
                    ViewBag.PeriodoId = periodoId;
                }

                if (periodoId == null)
                    return View(new List<PlanillaEmpleado>());

                var periodo = periodos.FirstOrDefault(p => p.PeriodoPagoId == periodoId)
                           ?? await _context.PeriodosPago.AsNoTracking()
                                .FirstOrDefaultAsync(p => p.PeriodoPagoId == periodoId);

                if (periodo == null)
                    return View(new List<PlanillaEmpleado>());

                ViewBag.Periodo = periodo;

                var planillas = await _context.PlanillasEmpleado
                    .Include(pe => pe.Empleado)
                    .Include(pe => pe.PeriodoPago)
                    .AsNoTracking()
                    .Where(pe => pe.PeriodoPagoId == periodoId)
                    .ToListAsync(); // Removemos el orden aquí, se hará en la vista

                ViewBag.TotalEmpleados = planillas.Count;
                ViewBag.TotalDevengado = planillas.Sum(p => p.TotalDevengado);
                ViewBag.TotalDeducciones = planillas.Sum(p => p.TotalDeducciones);
                ViewBag.TotalNeto = planillas.Sum(p => p.NetoAPagar);

                return View(planillas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar planilla. PeriodoId: {P}", periodoId);
                TempData["Error"] = $"Ocurrió un error al cargar la planilla: {ex.Message}";
                TempData["ErrorDetail"] = ex.ToString(); // Para debugging
                return View(new List<PlanillaEmpleado>());
            }
        }

        // Calculo masivo de planilla para todos los empleados del periodo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calcular(int periodoId)
        {
            try
            {
                var periodo = await _context.PeriodosPago.FindAsync(periodoId);
                if (periodo == null)
                {
                    TempData["Error"] = "Período no encontrado.";
                    return RedirectToAction(nameof(Index), new { periodoId });
                }
                if (periodo.Estado == EstadoPeriodo.Cerrado)
                {
                    TempData["Error"] = "No se puede recalcular un período cerrado.";
                    return RedirectToAction(nameof(Index), new { periodoId });
                }

                var tipoPagoFiltro = (TipoPago)(int)periodo.TipoPeriodo;
                var todosEmpleados = await _context.Empleados
                    .Where(e => e.Activo &&
                        (e.TipoPago == tipoPagoFiltro ||
                         (periodo.TipoPeriodo == TipoPeriodo.Quincenal && (int)e.TipoPago == 0)))
                    .ToListAsync();

                // Solo incluir empleados cuya fecha de ingreso sea anterior o igual al FIN del período
                var empleadosExcluidos = todosEmpleados
                    .Where(e => e.FechaIngreso > periodo.FechaFin).ToList();
                var empleados = todosEmpleados
                    .Where(e => e.FechaIngreso <= periodo.FechaFin).ToList();

                var warningMessages = new List<string>();
                if (empleadosExcluidos.Any())
                    warningMessages.Add(
                        "Excluidos por fecha de ingreso posterior al período: " +
                        string.Join(", ", empleadosExcluidos
                            .Select(e => $"{e.PrimerApellido} {e.Nombre} (ingresó: {e.FechaIngreso:dd/MM/yyyy})")));

                if (!empleados.Any())
                {
                    TempData["Error"] = "No hay empleados elegibles para calcular en este período.";
                    return RedirectToAction(nameof(Index), new { periodoId });
                }

var abonosPeriodoExistentes = await _context.AbonosPrestamo
    .Include(a => a.Prestamo)
    .Where(a => a.Observaciones != null &&
                a.Observaciones.Contains("Deducción planilla") &&
                a.Observaciones.Contains(periodo.Descripcion))
    .Select(a => a.Prestamo.EmpleadoId)
    .Distinct()
    .ToListAsync();

var empleadoIds = empleados.Select(e => e.EmpleadoId).ToList();

// Pre-cargar todos los datos del período en memoria para evitar N+1 queries
var todasHorasExtras = await _context.HorasExtras
    .Where(h => h.PeriodoPagoId == periodoId &&
                h.Fecha >= periodo.FechaInicio &&
                h.Fecha <= periodo.FechaFin &&
                empleadoIds.Contains(h.EmpleadoId))
    .ToListAsync();

var todasComisiones = await _context.Comisiones
    .Where(c => c.Fecha >= periodo.FechaInicio &&
                c.Fecha <= periodo.FechaFin &&
                empleadoIds.Contains(c.EmpleadoId))
    .Select(c => new { c.EmpleadoId, c.Monto, c.Fecha })
    .ToListAsync();

var todosFeriados = await _context.PagosFeriado
    .Where(pf => pf.PeriodoPagoId == periodoId &&
                 empleadoIds.Contains(pf.EmpleadoId))
    .Select(pf => new { pf.EmpleadoId, pf.MontoTotal })
    .ToListAsync();

var todosPrestamos = await _context.Prestamos
    .Where(p => p.Activo &&
                p.FechaPrestamo <= periodo.FechaFin &&
                empleadoIds.Contains(p.EmpleadoId))
    .ToListAsync();

var todosCreditosActivos = await _context.CreditosFerreteria
    .Where(c => c.Activo &&
                c.FechaCredito <= periodo.FechaFin &&
                empleadoIds.Contains(c.EmpleadoId))
    .ToListAsync();

var todasIncapacidades = await _context.Incapacidades
    .Where(i => i.FechaInicio <= periodo.FechaFin &&
                i.FechaFin >= periodo.FechaInicio &&
                empleadoIds.Contains(i.EmpleadoId))
    .ToListAsync();

var todasVacaciones = await _context.Vacaciones
    .Where(v => v.Estado == EstadoVacacion.Aprobada &&
                v.Tipo == TipoVacacion.ConPago &&
                v.FechaInicio <= periodo.FechaFin &&
                v.FechaFin >= periodo.FechaInicio &&
                empleadoIds.Contains(v.EmpleadoId))
    .ToListAsync();

var todasPlanillasExistentes = await _context.PlanillasEmpleado
    .Where(p => p.PeriodoPagoId == periodoId &&
                empleadoIds.Contains(p.EmpleadoId))
    .ToListAsync();

foreach (var empleado in empleados)
{
    // ── Días efectivamente trabajados en este período ──────────────────────
    int diasDelPeriodo = (periodo.FechaFin - periodo.FechaInicio).Days + 1;
    int diasTrabajados = diasDelPeriodo;   // por defecto período completo
    bool esPeriodoParcial = false;

    if (empleado.FechaIngreso > periodo.FechaInicio)
    {
        // Empleado ingresó dentro del período: contar desde su FechaIngreso
        diasTrabajados = Math.Clamp(
            (periodo.FechaFin - empleado.FechaIngreso).Days + 1, 0, diasDelPeriodo);
        esPeriodoParcial = diasTrabajados < diasDelPeriodo;
    }

    // ── Salario ordinario ──────────────────────────────────────────────────
    // Período parcial → salario diario × días reales (Art. 164 CT)
    // Período completo → valor hora × horas ordinarias del período
    decimal salarioOrdinario;
    decimal horasOrdinariasEfectivas;

    if (esPeriodoParcial)
    {
        // Salario diario = SalarioBase / 30 (estándar CR)
        decimal salarioDiarioProrrateo = Math.Round(empleado.SalarioBase / 30m, 6);
        salarioOrdinario = Math.Round(salarioDiarioProrrateo * diasTrabajados, 2);

        // Horas consistentes con el salario pagado: salario ÷ valor hora
        horasOrdinariasEfectivas = empleado.ValorHora > 0
            ? Math.Round(salarioOrdinario / empleado.ValorHora, 2)
            : Math.Round(empleado.HorasPorPeriodo * (decimal)diasTrabajados / diasDelPeriodo, 2);
    }
    else
    {
        horasOrdinariasEfectivas = empleado.HorasPorPeriodo;
        salarioOrdinario = Math.Round(empleado.ValorHora * horasOrdinariasEfectivas, 2);
    }

    // Alerta de período parcial para que RRHH lo revise
    if (esPeriodoParcial)
        warningMessages.Add(
            $"Período parcial — {empleado.PrimerApellido} {empleado.Nombre}: " +
            $"{diasTrabajados} día(s) de {diasDelPeriodo} en este período " +
            $"(ingresó: {empleado.FechaIngreso:dd/MM/yyyy}). " +
            $"Salario calculado: ₡{Math.Round(empleado.SalarioBase / 30m * diasTrabajados, 2):N2}.");

    // DEVENGADOS — filtro en memoria, respetando fecha de ingreso
    var horasExtrasDelPeriodo = todasHorasExtras
        .Where(h => h.EmpleadoId == empleado.EmpleadoId &&
                    h.Fecha >= empleado.FechaIngreso)
        .ToList();
    var montoHorasExtras = horasExtrasDelPeriodo.Sum(h => h.MontoTotal);
    var totalHorasExtras  = horasExtrasDelPeriodo.Sum(h => h.TotalHoras);

    var montoComisiones = todasComisiones
        .Where(c => c.EmpleadoId == empleado.EmpleadoId &&
                    c.Fecha >= empleado.FechaIngreso)
        .Sum(c => c.Monto);

    var montoFeriados = todosFeriados
        .Where(pf => pf.EmpleadoId == empleado.EmpleadoId)
        .Sum(pf => pf.MontoTotal);

    // ✅ VACACIONES SE PAGAN POR SEPARADO - NO SUMAN EN PLANILLA

    // Incapacidades — son días NO trabajados: se deduce el salario de esos días.
    // El pago por incapacidad (CCSS/INS/Patrono) se refleja en el comprobante de incapacidad,
    // NO en la planilla. La planilla solo deduce los días no laborados.
    var divisorInc = _context.Database != null ? 30m : 30m; // salario diario CR estándar
    var salarioDiarioInc = Math.Round(empleado.SalarioBase / 30m, 6);
    var incapacidadesDelPeriodo = todasIncapacidades
        .Where(i => i.EmpleadoId == empleado.EmpleadoId &&
                    i.FechaInicio >= empleado.FechaIngreso)
        .ToList();

    // Calcular días de incapacidad que caen dentro del período
    int diasIncapacidadEnPeriodo = 0;
    foreach (var inc in incapacidadesDelPeriodo)
    {
        var inicioEfectivo = inc.FechaInicio < periodo.FechaInicio ? periodo.FechaInicio : inc.FechaInicio;
        var finEfectivo    = inc.FechaFin   > periodo.FechaFin   ? periodo.FechaFin   : inc.FechaFin;
        if (finEfectivo >= inicioEfectivo)
            diasIncapacidadEnPeriodo += (finEfectivo - inicioEfectivo).Days + 1;
    }
    var deduccionIncapacidad = Math.Round(salarioDiarioInc * diasIncapacidadEnPeriodo, 2);
    var montoIncapacidades = 0m; // Las incapacidades no son devengados en planilla

    // ✅ totalDevengado SIN vacaciones ni incapacidades (se pagan/deducen aparte)
    var totalDevengado = Math.Round(
        salarioOrdinario + montoHorasExtras +
        montoComisiones + montoFeriados, 2);

    // DEDUCCIONES
    var deduccionCCSS = Math.Round(
        totalDevengado * (periodo.PorcentajeCCSS / 100m), 2);

    var deduccionRenta = CalcularImpuestoRenta(
        totalDevengado, periodo, empleado.NumHijos, empleado.TieneConyuge);

    // Préstamos — SUMAR TODOS los préstamos activos (sin límite de cantidad)
    decimal deduccionPrestamo = 0m;
    if (!abonosPeriodoExistentes.Contains(empleado.EmpleadoId))
    {
        var prestamosEmpleado = todosPrestamos
            .Where(p =>
                p.EmpleadoId == empleado.EmpleadoId &&
                p.FechaPrestamo >= empleado.FechaIngreso)
            .ToList();

        foreach (var prestamo in prestamosEmpleado)
        {
            if (prestamo.Monto > 0)
            {
                var cuotaPrestamo = Math.Round(
                    Math.Min(prestamo.CuotaMensual, prestamo.Monto), 2);
                deduccionPrestamo += cuotaPrestamo;
            }
        }

        // Art. 172 CT: la deducción TOTAL no puede exceder el 50 % del salario ordinario
        var maxDeduccion = Math.Round(salarioOrdinario * 0.50m, 2);
        if (deduccionPrestamo > maxDeduccion)
        {
            warningMessages.Add(
                $"{empleado.PrimerApellido} {empleado.Nombre}: Cuota total de préstamos (₡{deduccionPrestamo:N2}) " +
                $"limitada al 50% del salario (₡{maxDeduccion:N2}) según Art. 172 CT. " +
                $"Empleado tiene {prestamosEmpleado.Count} préstamo(s) activo(s).");
            deduccionPrestamo = maxDeduccion;
        }
    }

    // Créditos ferretería — validando fecha ingreso
    var creditosActivos = todosCreditosActivos
        .Where(c => c.EmpleadoId == empleado.EmpleadoId &&
                    c.FechaCredito >= empleado.FechaIngreso)
        .ToList();
    var deduccionCredito = Math.Round(
        creditosActivos
            .Where(c => c.Saldo > 0)
            .Sum(c => Math.Min(c.CuotaQuincenal, c.Saldo)), 2);

    var totalDeducciones = Math.Round(
        deduccionCCSS + deduccionRenta +
        deduccionPrestamo + deduccionCredito + deduccionIncapacidad, 2);

    var netoAPagar = Math.Max(0,
        Math.Round(totalDevengado - totalDeducciones, 2));

    // Alerta acumulada — BUG 4 corregido: no sobreescribir TempData en el loop
    if (netoAPagar == 0 && totalDeducciones > 0)
        warningMessages.Add($"ADVERTENCIA: {empleado.PrimerApellido} {empleado.Nombre} tiene deducciones que igualan o exceden su salario (Neto = ₡0).");

    var planillaExistente = todasPlanillasExistentes
        .FirstOrDefault(p =>
            p.EmpleadoId == empleado.EmpleadoId);

    if (planillaExistente != null)
    {
        // BUG 2 corregido: preservar OtrasDeducciones y HorasNoLaboradas ajustadas manualmente
        var otrasDeduccionesGuardadas  = planillaExistente.OtrasDeducciones;
        var horasNoLaboradasGuardadas  = planillaExistente.HorasNoLaboradas;
        var descOtrasGuardada          = planillaExistente.DescripcionOtrasDeducciones;

        // BUG 1 corregido: recalcular DeduccionHorasNoLaboradas
        var deduccionHorasNoLab = Math.Round(empleado.ValorHora * horasNoLaboradasGuardadas, 2);

        var totalDeduccionesFinal = Math.Round(
            deduccionCCSS + deduccionRenta +
            deduccionPrestamo + deduccionCredito +
            deduccionHorasNoLab + otrasDeduccionesGuardadas + deduccionIncapacidad, 2);

        planillaExistente.HorasOrdinarias            = horasOrdinariasEfectivas;
        planillaExistente.HorasExtras                = totalHorasExtras;
        planillaExistente.HorasNoLaboradas           = horasNoLaboradasGuardadas;
        planillaExistente.ValorHora                  = empleado.ValorHora;
        planillaExistente.ValorHoraExtra             = empleado.ValorHoraExtra;
        planillaExistente.SalarioOrdinario           = salarioOrdinario;
        planillaExistente.AumentoAplicado            = montoComisiones;
        planillaExistente.MontoHorasExtras           = montoHorasExtras;
        planillaExistente.MontoFeriados              = montoFeriados;
        planillaExistente.MontoVacaciones            = 0m; // Vacaciones se pagan por separado
        planillaExistente.MontoIncapacidades         = 0m;  // Incapacidades no son devengado en planilla
        planillaExistente.TotalDevengado             = totalDevengado;
        planillaExistente.DeduccionCCSS              = deduccionCCSS;
        planillaExistente.DeduccionRenta             = deduccionRenta;
        planillaExistente.DeduccionPrestamos         = deduccionPrestamo;
        planillaExistente.DeduccionCreditoFerreteria = deduccionCredito;
        planillaExistente.DeduccionIncapacidad       = deduccionIncapacidad; // Deducción por días no trabajados
        planillaExistente.DeduccionVacaciones        = 0m;
        planillaExistente.DeduccionHorasNoLaboradas  = deduccionHorasNoLab;
        planillaExistente.OtrasDeducciones           = otrasDeduccionesGuardadas;
        planillaExistente.DescripcionOtrasDeducciones = descOtrasGuardada;
        planillaExistente.TotalDeducciones           = totalDeduccionesFinal;
        planillaExistente.DiasTrabajados             = diasTrabajados;
        planillaExistente.NetoAPagar                 = Math.Max(0, Math.Round(totalDevengado - totalDeduccionesFinal, 2));
    }
    else
    {
        _context.PlanillasEmpleado.Add(new PlanillaEmpleado
        {
            PeriodoPagoId              = periodoId,
            PorcentajeCCSS             = periodo.PorcentajeCCSS,
            EmpleadoId                 = empleado.EmpleadoId,
            HorasOrdinarias            = horasOrdinariasEfectivas,
            HorasExtras                = totalHorasExtras,
            HorasNoLaboradas           = 0m,
            ValorHora                  = empleado.ValorHora,
            ValorHoraExtra             = empleado.ValorHoraExtra,
            SalarioOrdinario           = salarioOrdinario,
            AumentoAplicado            = montoComisiones,
            MontoHorasExtras           = montoHorasExtras,
            MontoFeriados              = montoFeriados,
            MontoVacaciones            = 0m, // Vacaciones se pagan por separado
            MontoIncapacidades         = 0m,  // Incapacidades no son devengado en planilla
            TotalDevengado             = totalDevengado,
            DeduccionCCSS              = deduccionCCSS,
            DeduccionRenta             = deduccionRenta,
            DeduccionPrestamos         = deduccionPrestamo,
            DeduccionCreditoFerreteria = deduccionCredito,
            DeduccionIncapacidad       = deduccionIncapacidad, // Deducción por días no trabajados
            DeduccionVacaciones        = 0m,
            DeduccionHorasNoLaboradas  = 0m,
            OtrasDeducciones           = 0m,
            TotalDeducciones           = totalDeducciones,
            DiasTrabajados             = diasTrabajados,
            NetoAPagar                 = Math.Max(0, Math.Round(totalDevengado - totalDeducciones, 2))
        });
    }
}

                await _context.SaveChangesAsync();

                if (warningMessages.Any())
                    TempData["Warning"] = string.Join(" | ", warningMessages);

                TempData["Success"] = $"Planilla calculada correctamente para {empleados.Count} empleado(s).";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular planilla");
                TempData["Error"] = "Ocurrió un error al calcular la planilla.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
        }

        // Formulario de edicion de planilla individual
        public async Task<IActionResult> Editar(int? id)
        {
            try
            {
                if (id == null || id <= 0) return NotFound();

                var planilla = await _context.PlanillasEmpleado
                    .Include(pe => pe.Empleado)
                    .Include(pe => pe.PeriodoPago)
                    .FirstOrDefaultAsync(pe => pe.PlanillaEmpleadoId == id);

                if (planilla == null) return NotFound();

                if (planilla.PeriodoPago.Estado == EstadoPeriodo.Cerrado)
                {
                    TempData["Error"] = "No se puede editar una planilla cerrada.";
                    return RedirectToAction(nameof(Index), new { periodoId = planilla.PeriodoPagoId });
                }

                return View(planilla);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar edición planilla ID: {Id}", id);
                TempData["Error"] = "Error al cargar el formulario.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Guardar cambios de edicion de planilla individual
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, PlanillaEmpleado model)
        {
            try
            {
                var registro = await _context.PlanillasEmpleado
                    .Include(pe => pe.PeriodoPago)
                    .Include(pe => pe.Empleado)
                    .FirstOrDefaultAsync(pe => pe.PlanillaEmpleadoId == id);

                if (registro == null) return NotFound();

                if (registro.PeriodoPago.Estado == EstadoPeriodo.Cerrado)
                {
                    TempData["Error"] = "No se puede editar una planilla cerrada.";
                    return RedirectToAction(nameof(Index), new { periodoId = registro.PeriodoPagoId });
                }

                registro.AumentoAplicado  = Math.Round(model.AumentoAplicado, 2);
                registro.HorasNoLaboradas = Math.Round(model.HorasNoLaboradas, 2);
                registro.OtrasDeducciones = Math.Round(model.OtrasDeducciones, 2);
                registro.DescripcionOtrasDeducciones = model.DescripcionOtrasDeducciones?.Trim();

                registro.TotalDevengado = Math.Round(
                    registro.SalarioOrdinario + registro.AumentoAplicado +
                    registro.MontoHorasExtras + registro.MontoFeriados, 2);

                registro.DeduccionCCSS = Math.Round(
                    registro.TotalDevengado * (registro.PorcentajeCCSS / 100m), 2);

                registro.DeduccionRenta = CalcularImpuestoRenta(registro.TotalDevengado,
                    registro.PeriodoPago, registro.Empleado.NumHijos, registro.Empleado.TieneConyuge);

                // BUG 1 corregido: recalcular deducción de horas no laboradas con el valor hora actual
                registro.DeduccionHorasNoLaboradas = Math.Round(
                    registro.ValorHora * registro.HorasNoLaboradas, 2);

                // BUG 5: limpiar vacaciones (siempre 0)
                registro.DeduccionVacaciones = 0m;

                registro.TotalDeducciones = Math.Round(
                    registro.DeduccionCCSS +
                    registro.DeduccionRenta +
                    registro.DeduccionPrestamos +
                    registro.DeduccionCreditoFerreteria +
                    registro.DeduccionIncapacidad +
                    registro.DeduccionHorasNoLaboradas +
                    registro.OtrasDeducciones, 2);

                registro.NetoAPagar = Math.Max(0, Math.Round(registro.TotalDevengado - registro.TotalDeducciones, 2));

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Planilla actualizada - Neto: ₡{registro.NetoAPagar:N0}";
                return RedirectToAction(nameof(Index), new { periodoId = registro.PeriodoPagoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar planilla ID: {Id}", id);
                TempData["Error"] = "Error al guardar los cambios.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Cierre de periodo: aplica abonos a prestamos y creditos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarPeriodo(int periodoId)
        {
            try
            {
                var periodo = await _context.PeriodosPago.FindAsync(periodoId);
                if (periodo == null)
                {
                    TempData["Error"] = "Período no encontrado.";
                    return RedirectToAction(nameof(Index), new { periodoId });
                }
                if (periodo.Estado == EstadoPeriodo.Cerrado)
                {
                    TempData["Error"] = "Este período ya estaba cerrado.";
                    return RedirectToAction(nameof(Index), new { periodoId });
                }

                var tienePlanilla = await _context.PlanillasEmpleado
                    .AnyAsync(pe => pe.PeriodoPagoId == periodoId);

                if (!tienePlanilla)
                {
                    TempData["Error"] = "Calculá la planilla antes de cerrar el período.";
                    return RedirectToAction(nameof(Index), new { periodoId });
                }

                var planillas = await _context.PlanillasEmpleado
                    .Where(pe => pe.PeriodoPagoId == periodoId)
                    .ToListAsync();

                var empleadosConAbonoPrestamo = await _context.AbonosPrestamo
                    .Include(a => a.Prestamo)
                    .Where(a => a.Observaciones != null &&
                                a.Observaciones.Contains("Deducción planilla") &&
                                a.Observaciones.Contains(periodo.Descripcion))
                    .Select(a => a.Prestamo.EmpleadoId)
                    .ToListAsync();

                var empleadosConAbonoCredito = await _context.AbonosCreditoFerreteria
                    .Include(a => a.CreditoFerreteria)
                    .Where(a => a.Observaciones != null &&
                                a.Observaciones.Contains("Deducción planilla") &&
                                a.Observaciones.Contains(periodo.Descripcion))
                    .Select(a => a.CreditoFerreteria.EmpleadoId)
                    .ToListAsync();

                // Préstamos - aplicar abonos a TODOS los préstamos activos
                foreach (var planilla in planillas.Where(p => p.DeduccionPrestamos > 0))
                {
                    if (empleadosConAbonoPrestamo.Contains(planilla.EmpleadoId)) continue;

                    var prestamosActivos = await _context.Prestamos
                        .Where(p => p.EmpleadoId == planilla.EmpleadoId && p.Activo)
                        .OrderBy(p => p.FechaPrestamo) // Abonar primero a los más antiguos
                        .ToListAsync();

                    if (!prestamosActivos.Any()) continue;

                    var montoRestantePorDistribuir = planilla.DeduccionPrestamos;

                    foreach (var prestamo in prestamosActivos)
                    {
                        if (montoRestantePorDistribuir <= 0) break;

                        var saldoAnterior = prestamo.Monto;
                        var montoAbono = Math.Round(Math.Min(montoRestantePorDistribuir, prestamo.Monto), 2);
                        prestamo.Monto = Math.Max(0, Math.Round(prestamo.Monto - montoAbono, 2));
                        if (prestamo.Monto <= 0) prestamo.Activo = false;

                        montoRestantePorDistribuir -= montoAbono;

                        _context.AbonosPrestamo.Add(new AbonoPrestamo
                        {
                            PrestamoId = prestamo.PrestamoId,
                            Monto = montoAbono,
                            FechaAbono = DateTime.Now,
                            Observaciones = $"Deducción planilla — {periodo.Descripcion} " +
                                            $"— Saldo anterior: ₡{saldoAnterior:N0} " +
                                            $"— Nuevo saldo: ₡{prestamo.Monto:N0}"
                        });
                    }
                }

                // Créditos Ferretería
                foreach (var planilla in planillas.Where(p => p.DeduccionCreditoFerreteria > 0))
                {
                    if (empleadosConAbonoCredito.Contains(planilla.EmpleadoId)) continue;

                    var creditos = await _context.CreditosFerreteria
                        .Where(c => c.EmpleadoId == planilla.EmpleadoId && c.Activo && c.Saldo > 0)
                        .ToListAsync();

                    var montoRestante = planilla.DeduccionCreditoFerreteria;
                    foreach (var credito in creditos)
                    {
                        if (montoRestante <= 0) break;
                        var abono = Math.Round(Math.Min(Math.Min(credito.CuotaQuincenal, credito.Saldo), montoRestante), 2);
                        var saldoAnterior = credito.Saldo;
                        credito.Saldo = Math.Max(0, Math.Round(credito.Saldo - abono, 2));
                        montoRestante -= abono;
                        if (credito.Saldo <= 0) credito.Activo = false;

                        _context.AbonosCreditoFerreteria.Add(new AbonoCreditoFerreteria
                        {
                            CreditoFerreteriaId = credito.CreditoFerreteriaId,
                            Monto = abono,
                            FechaAbono = DateTime.Now,
                            Observaciones = $"Deducción planilla — {periodo.Descripcion} " +
                                            $"— Saldo anterior: ₡{saldoAnterior:N0} " +
                                            $"— Nuevo saldo: ₡{credito.Saldo:N0}"
                        });
                    }
                }

                periodo.Estado = EstadoPeriodo.Cerrado;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Cerrar período con planilla", "Planilla",
                    $"PeriodoId: {periodoId} — {periodo.Descripcion} — Empleados: {planillas.Count}");

                TempData["Success"] =
                    $"Período {periodo.Descripcion} cerrado correctamente.\n\n" +
                    "Se aplicaron las deducciones obligatorias:\n" +
                    "• CCSS\n" +
                    "• Retención del Impuesto sobre la Renta (ISR)";

                return RedirectToAction(nameof(Index), new { periodoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar período planilla: {P}", periodoId);
                TempData["Error"] = "Error al cerrar el período. Intentá de nuevo.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
        }

        // REABRIR PERÍODO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReabrirPeriodo(int periodoId)
        {
            try
            {
                var periodo = await _context.PeriodosPago.FindAsync(periodoId);
                if (periodo == null)
                {
                    TempData["Error"] = "Período no encontrado.";
                    return RedirectToAction(nameof(Index), new { periodoId });
                }
                if (periodo.Estado == EstadoPeriodo.Abierto)
                {
                    TempData["Error"] = "Este período ya estaba abierto.";
                    return RedirectToAction(nameof(Index), new { periodoId });
                }

                var descPeriodo = periodo.Descripcion;
                var abonosPrestamo = await _context.AbonosPrestamo
                    .Include(a => a.Prestamo)
                    .Where(a => a.Observaciones != null &&
                                a.Observaciones.Contains("Deducción planilla") &&
                                a.Observaciones.Contains(descPeriodo))
                    .ToListAsync();

                foreach (var abono in abonosPrestamo)
                {
                    abono.Prestamo.Monto = Math.Round(abono.Prestamo.Monto + abono.Monto, 2);
                    abono.Prestamo.Activo = true;
                }
                _context.AbonosPrestamo.RemoveRange(abonosPrestamo);

                var abonosCredito = await _context.AbonosCreditoFerreteria
                    .Include(a => a.CreditoFerreteria)
                    .Where(a => a.Observaciones != null &&
                                a.Observaciones.Contains("Deducción planilla") &&
                                a.Observaciones.Contains(descPeriodo))
                    .ToListAsync();

                foreach (var abono in abonosCredito)
                {
                    abono.CreditoFerreteria.Saldo = Math.Round(abono.CreditoFerreteria.Saldo + abono.Monto, 2);
                    abono.CreditoFerreteria.Activo = true;
                }
                _context.AbonosCreditoFerreteria.RemoveRange(abonosCredito);

                periodo.Estado = EstadoPeriodo.Abierto;
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Reabrir período desde planilla", "Planilla",
                    $"PeriodoId: {periodoId} — {descPeriodo}");

                TempData["Success"] = $"Período {descPeriodo} reabierto correctamente. Podés recalcular la planilla.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reabrir período: {P}", periodoId);
                TempData["Error"] = "Error al reabrir el período. Intentá de nuevo.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
        }

        // DESCARGAR PDF INDIVIDUAL
        [HttpGet]
        public async Task<IActionResult> DescargarPDF(int id)
        {
            try
            {
                var planilla = await _context.PlanillasEmpleado
                    .Include(pe => pe.Empleado)
                    .Include(pe => pe.PeriodoPago)
                    .FirstOrDefaultAsync(pe => pe.PlanillaEmpleadoId == id);

                if (planilla == null)
                {
                    TempData["Error"] = $"No se encontró la planilla con ID {id}.";
                    return RedirectToAction(nameof(Index));
                }

                var usuario = HttpContext.Session.GetString("Usuario") ?? "Sistema";

                _logger.LogInformation("Generando PDF para planilla ID: {Id}, Empleado: {Emp}",
                    id, $"{planilla.Empleado.PrimerApellido} {planilla.Empleado.Nombre}");

                byte[] pdfBytes;
                try
                {
                    pdfBytes = _servicioPDF.GenerarPDF(planilla, usuario);
                }
                catch (Exception pdfEx)
                {
                    _logger.LogError(pdfEx, "Error interno en GenerarPDF para planilla ID: {Id}", id);
                    TempData["Error"] = $"Error al generar el PDF: {pdfEx.Message}";
                    return RedirectToAction(nameof(Index), new { periodoId = planilla.PeriodoPagoId });
                }

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    TempData["Error"] = "El PDF generado está vacío. Contactá al administrador.";
                    return RedirectToAction(nameof(Index), new { periodoId = planilla.PeriodoPagoId });
                }

                var nombre = $"Comprobante_{planilla.Empleado.PrimerApellido}_{planilla.PeriodoPago.Descripcion.Replace(" ", "_").Replace("—", "").Replace("/", "-")}.pdf";

                await _auditoria.RegistrarAsync(
                    usuario,
                    "Descargar PDF planilla", "Planilla",
                    $"{planilla.Empleado.PrimerApellido} {planilla.Empleado.Nombre}");

                _logger.LogInformation("PDF generado OK: {Nombre} ({Bytes} bytes)", nombre, pdfBytes.Length);

                return File(pdfBytes, "application/pdf", nombre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al generar PDF planilla ID: {Id} — Tipo: {Type} — Mensaje: {Msg}",
                    id, ex.GetType().Name, ex.Message);
                TempData["Error"] = $"Error al generar el PDF: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // EXPORTAR EXCEL

        [HttpGet]
        public async Task<IActionResult> ExportarExcel(int periodoId)
        {
            try
            {
                var periodo = await _context.PeriodosPago.FindAsync(periodoId);
                if (periodo == null) return NotFound();

                var planillas = await _context.PlanillasEmpleado
                    .Include(pe => pe.Empleado)
                    .Include(pe => pe.PeriodoPago)
                    .AsNoTracking()
                    .Where(pe => pe.PeriodoPagoId == periodoId)
                    .OrderBy(pe => pe.Empleado.Departamento)
                    .ThenBy(pe => pe.Empleado.PrimerApellido)
                    .ToListAsync();

                if (!planillas.Any())
                {
                    TempData["Error"] = "No hay datos de planilla para exportar.";
                    return RedirectToAction(nameof(Index), new { periodoId });
                }

                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var ws = workbook.Worksheets.Add("Planilla");

                ws.Cell(1, 1).Value = "FERRETERÍA EL PANA SRL";
                ws.Range(1, 1, 1, 17).Merge().Style
                    .Font.SetBold(true).Font.SetFontSize(14)
                    .Alignment.SetHorizontal(
                        ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

                ws.Cell(2, 1).Value = "DEPARTAMENTO DE RECURSOS HUMANOS";
                ws.Range(2, 1, 2, 17).Merge().Style
                    .Font.SetBold(true).Font.SetFontSize(12)
                    .Alignment.SetHorizontal(
                        ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

                var tipoTextoExcel = periodo.TipoPeriodo switch
                {
                    TipoPeriodo.Semanal => "SEMANAL",
                    TipoPeriodo.Mensual => "MENSUAL",
                    _ => "QUINCENAL"
                };
                ws.Cell(3, 1).Value =
                    $"PLANILLA {tipoTextoExcel} — {periodo.Descripcion}";
                ws.Range(3, 1, 3, 17).Merge().Style
                    .Font.SetBold(true).Font.SetFontSize(11)
                    .Alignment.SetHorizontal(
                        ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

                ws.Cell(4, 1).Value =
                    $"PERÍODO: {periodo.FechaInicio:dd/MM/yyyy} " +
                    $"AL {periodo.FechaFin:dd/MM/yyyy}";
                ws.Range(4, 1, 4, 17).Merge().Style
                    .Font.SetItalic(true)
                    .Alignment.SetHorizontal(
                        ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

                var headers = new[]
                {
                    "Nombre y Apellidos", "Cédula", "Departamento", "Puesto",
                    "Sal. Ordinario", "Hrs. Extras", "Comisión", "Feriados",
                    "Total Devengado", $"CCSS {periodo.PorcentajeCCSS}%", "Renta (ISR)", "Préstamo",
                    "Cré. Ferretería", "Incapacidad",
                    "Hrs. No Lab.", "Total Deducciones", "Neto a Pagar"
                };
                const int NCOLS = 17;

                int fila = 6;
                for (int col = 1; col <= headers.Length; col++)
                {
                    ws.Cell(fila, col).Value = headers[col - 1];
                    ws.Cell(fila, col).Style
                        .Font.SetBold(true)
                        .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                        .Fill.SetBackgroundColor(
                            ClosedXML.Excel.XLColor.FromHtml("#FF7A00"))
                        .Alignment.SetHorizontal(
                            ClosedXML.Excel.XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(
                            ClosedXML.Excel.XLBorderStyleValues.Thin);
                }

                fila = 7;
                var grupos = planillas.GroupBy(p => p.Empleado.Departamento);

                foreach (var grupo in grupos)
                {
                    ws.Cell(fila, 1).Value = grupo.Key.ToUpper();
                    ws.Range(fila, 1, fila, NCOLS).Merge().Style
                        .Font.SetBold(true)
                        .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                        .Fill.SetBackgroundColor(
                            ClosedXML.Excel.XLColor.FromHtml("#333333"))
                        .Alignment.SetHorizontal(
                            ClosedXML.Excel.XLAlignmentHorizontalValues.Left);
                    fila++;

                    foreach (var p in grupo)
                    {
                        ws.Cell(fila, 1).Value =
                            $"{p.Empleado.PrimerApellido} " +
                            $"{p.Empleado.SegundoApellido} {p.Empleado.Nombre}";
                        ws.Cell(fila, 2).Value = p.Empleado.Cedula;
                        ws.Cell(fila, 3).Value = p.Empleado.Departamento;
                        ws.Cell(fila, 4).Value = p.Empleado.Puesto;
                        ws.Cell(fila, 5).Value = p.SalarioOrdinario;
                        ws.Cell(fila, 6).Value = p.MontoHorasExtras;
                        ws.Cell(fila, 7).Value = p.AumentoAplicado;
                        ws.Cell(fila, 8).Value = p.MontoFeriados;
                        ws.Cell(fila, 9).Value = p.TotalDevengado;
                        ws.Cell(fila, 10).Value = p.DeduccionCCSS;
                        ws.Cell(fila, 11).Value = p.DeduccionRenta;
                        ws.Cell(fila, 12).Value = p.DeduccionPrestamos;
                        ws.Cell(fila, 13).Value = p.DeduccionCreditoFerreteria;
                        ws.Cell(fila, 14).Value = p.DeduccionIncapacidad;
                        ws.Cell(fila, 15).Value = p.DeduccionHorasNoLaboradas;
                        ws.Cell(fila, 16).Value = p.TotalDeducciones;
                        ws.Cell(fila, 17).Value = p.NetoAPagar;

                        for (int col = 5; col <= NCOLS; col++)
                            ws.Cell(fila, col).Style
                                .NumberFormat.Format = "₡#,##0.00";

                        if (fila % 2 == 0)
                            ws.Range(fila, 1, fila, NCOLS).Style
                                .Fill.SetBackgroundColor(
                                    ClosedXML.Excel.XLColor.FromHtml("#FFF3E0"));

                        ws.Range(fila, 1, fila, NCOLS).Style
                            .Border.SetOutsideBorder(
                                ClosedXML.Excel.XLBorderStyleValues.Thin)
                            .Border.SetInsideBorder(
                                ClosedXML.Excel.XLBorderStyleValues.Hair);
                        fila++;
                    }

                    ws.Cell(fila, 1).Value = $"Subtotal — {grupo.Key}";
                    ws.Range(fila, 1, fila, 4).Merge();
                    ws.Cell(fila, 5).Value = grupo.Sum(p => p.SalarioOrdinario);
                    ws.Cell(fila, 6).Value = grupo.Sum(p => p.MontoHorasExtras);
                    ws.Cell(fila, 7).Value = grupo.Sum(p => p.AumentoAplicado);
                    ws.Cell(fila, 8).Value = grupo.Sum(p => p.MontoFeriados);
                    ws.Cell(fila, 9).Value = grupo.Sum(p => p.TotalDevengado);
                    ws.Cell(fila, 10).Value = grupo.Sum(p => p.DeduccionCCSS);
                    ws.Cell(fila, 11).Value = grupo.Sum(p => p.DeduccionRenta);
                    ws.Cell(fila, 12).Value = grupo.Sum(p => p.DeduccionPrestamos);
                    ws.Cell(fila, 13).Value = grupo.Sum(p => p.DeduccionCreditoFerreteria);
                    ws.Cell(fila, 14).Value = grupo.Sum(p => p.DeduccionIncapacidad);
                    ws.Cell(fila, 15).Value = grupo.Sum(p => p.DeduccionHorasNoLaboradas);
                    ws.Cell(fila, 16).Value = grupo.Sum(p => p.TotalDeducciones);
                    ws.Cell(fila, 17).Value = grupo.Sum(p => p.NetoAPagar);

                    ws.Range(fila, 1, fila, NCOLS).Style
                        .Font.SetBold(true)
                        .Fill.SetBackgroundColor(
                            ClosedXML.Excel.XLColor.FromHtml("#FFE0B2"))
                        .Border.SetOutsideBorder(
                            ClosedXML.Excel.XLBorderStyleValues.Medium);

                    for (int col = 5; col <= NCOLS; col++)
                        ws.Cell(fila, col).Style.NumberFormat.Format = "₡#,##0.00";

                    fila++;
                }

                ws.Cell(fila, 1).Value = "TOTAL GENERAL";
                ws.Range(fila, 1, fila, 4).Merge();
                ws.Cell(fila, 5).Value = planillas.Sum(p => p.SalarioOrdinario);
                ws.Cell(fila, 6).Value = planillas.Sum(p => p.MontoHorasExtras);
                ws.Cell(fila, 7).Value = planillas.Sum(p => p.AumentoAplicado);
                ws.Cell(fila, 8).Value = planillas.Sum(p => p.MontoFeriados);
                ws.Cell(fila, 9).Value = planillas.Sum(p => p.TotalDevengado);
                ws.Cell(fila, 10).Value = planillas.Sum(p => p.DeduccionCCSS);
                ws.Cell(fila, 11).Value = planillas.Sum(p => p.DeduccionRenta);
                ws.Cell(fila, 12).Value = planillas.Sum(p => p.DeduccionPrestamos);
                ws.Cell(fila, 13).Value = planillas.Sum(p => p.DeduccionCreditoFerreteria);
                ws.Cell(fila, 14).Value = planillas.Sum(p => p.DeduccionIncapacidad);
                ws.Cell(fila, 15).Value = planillas.Sum(p => p.DeduccionHorasNoLaboradas);
                ws.Cell(fila, 16).Value = planillas.Sum(p => p.TotalDeducciones);
                ws.Cell(fila, 17).Value = planillas.Sum(p => p.NetoAPagar);

                ws.Range(fila, 1, fila, NCOLS).Style
                    .Font.SetBold(true).Font.SetFontSize(11)
                    .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                    .Fill.SetBackgroundColor(
                        ClosedXML.Excel.XLColor.FromHtml("#FF7A00"))
                    .Border.SetOutsideBorder(
                        ClosedXML.Excel.XLBorderStyleValues.Medium);

                for (int col = 5; col <= NCOLS; col++)
                    ws.Cell(fila, col).Style.NumberFormat.Format = "₡#,##0.00";

                ws.Column(1).Width = 30;
                ws.Column(2).Width = 14;
                ws.Column(3).Width = 14;
                ws.Column(4).Width = 20;
                for (int col = 5; col <= NCOLS; col++) ws.Column(col).Width = 16;
                ws.SheetView.FreezeRows(6);

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Exportar Excel planilla", "Planilla",
                    $"PeriodoId: {periodoId} — {periodo.Descripcion}");

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var nombre =
                    $"Planilla_{periodo.Descripcion.Replace(" ", "_").Replace("—", "")}.xlsx";

                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    nombre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error al exportar Excel planilla. PeriodoId: {P}", periodoId);
                TempData["Error"] = "Error al generar el Excel. Intentá de nuevo.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
        }

        // EXPORTAR PDF GENERAL

        [HttpGet]
        public async Task<IActionResult> ExportarPDF(int periodoId)
        {
            try
            {
                var periodo = await _context.PeriodosPago.FindAsync(periodoId);
                if (periodo == null) return NotFound();

                var planillas = await _context.PlanillasEmpleado
                    .Include(pe => pe.Empleado)
                    .Include(pe => pe.PeriodoPago)
                    .AsNoTracking()
                    .Where(pe => pe.PeriodoPagoId == periodoId)
                    .OrderBy(pe => pe.Empleado.Departamento)
                    .ThenBy(pe => pe.Empleado.PrimerApellido)
                    .ToListAsync();

                if (!planillas.Any())
                {
                    TempData["Error"] = "No hay datos de planilla para exportar.";
                    return RedirectToAction(nameof(Index), new { periodoId });
                }

                var usuario = HttpContext.Session.GetString("Usuario") ?? "Sistema";
                var pdfBytes = _servicioPDF.GenerarPDFPlanillaGeneral(planillas, periodo, usuario);
                var nombre =
                    $"Planilla_{periodo.Descripcion.Replace(" ", "_").Replace("—", "")}.pdf";

                await _auditoria.RegistrarAsync(
                    usuario,
                    "Exportar PDF planilla general", "Planilla",
                    $"PeriodoId: {periodoId} — {periodo.Descripcion}");

                return File(pdfBytes, "application/pdf", nombre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error al exportar PDF planilla. PeriodoId: {P}", periodoId);
                TempData["Error"] = "Error al generar el PDF. Intentá de nuevo.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
        }

        // ENVIAR PDF POR EMAIL

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarPorEmail(int id)
        {
            try
            {
                var planilla = await _context.PlanillasEmpleado
                    .Include(pe => pe.Empleado)
                    .Include(pe => pe.PeriodoPago)
                    .FirstOrDefaultAsync(pe => pe.PlanillaEmpleadoId == id);

                if (planilla == null)
                {
                    TempData["Error"] = "Planilla no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                var correo = planilla.Empleado.CorreoElectronico;
                if (string.IsNullOrWhiteSpace(correo))
                {
                    TempData["Error"] =
                        $"{planilla.Empleado.PrimerApellido} " +
                        $"{planilla.Empleado.Nombre} no tiene correo registrado.";
                    return RedirectToAction(nameof(Index),
                        new { periodoId = planilla.PeriodoPagoId });
                }

                var pdfBytes = _servicioPDF.GenerarPDFSinFirmas(planilla);
                var nombreArchivo =
                    $"Comprobante_{planilla.Empleado.PrimerApellido}_" +
                    $"{planilla.PeriodoPago.Descripcion.Replace(" ", "_").Replace("—", "")}.pdf";

                var emailSvc = HttpContext.RequestServices
                    .GetRequiredService<EmailService>();

                var asunto = $"Comprobante de Pago — {planilla.PeriodoPago.Descripcion}";
                var cuerpo = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;'>
    <div style='background:#1A1A2E;padding:20px;'>
        <h2 style='color:#FF7A00;margin:0;'>Ferretería El Pana SRL</h2>
        <p style='color:#888;margin:4px 0 0;font-size:13px;'>
            Departamento de Recursos Humanos
        </p>
    </div>
    <div style='padding:20px;border:1px solid #eee;'>
        <p>Estimado(a) <strong>{planilla.Empleado.PrimerApellido}
           {planilla.Empleado.Nombre}</strong>,</p>
        <p>Adjunto encontrará su comprobante de pago correspondiente al
           período <strong>{planilla.PeriodoPago.Descripcion}</strong>.</p>
        <table style='width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;'>
            <tr style='background:#f9f9f9;'>
                <td style='padding:8px;border:1px solid #eee;'>Total Devengado</td>
                <td style='padding:8px;border:1px solid #eee;
                           font-weight:bold;color:#1B5E20;'>
                    ₡{planilla.TotalDevengado:N2}
                </td>
            </tr>
            <tr>
                <td style='padding:8px;border:1px solid #eee;'>Total Deducciones</td>
                <td style='padding:8px;border:1px solid #eee;color:#B71C1C;'>
                    ₡{planilla.TotalDeducciones:N2}
                </td>
            </tr>
            <tr style='background:#fff9f0;'>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    Neto a Pagar
                </td>
                <td style='padding:8px;border:1px solid #eee;
                           font-weight:bold;font-size:16px;color:#FF7A00;'>
                    ₡{planilla.NetoAPagar:N2}
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
                    $"{planilla.Empleado.PrimerApellido} {planilla.Empleado.Nombre}",
                    asunto, cuerpo, pdfBytes, nombreArchivo);

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Enviar comprobante por email", "Planilla",
                    $"{planilla.Empleado.PrimerApellido} {planilla.Empleado.Nombre} " +
                    $"→ {correo}");

                TempData[enviado ? "Success" : "Error"] = enviado
                    ? $"Comprobante enviado a {correo}."
                    : "Error al enviar el correo. Verificá usuario, app password de Gmail y puerto SMTP.";

                return RedirectToAction(nameof(Index),
                    new { periodoId = planilla.PeriodoPagoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error al enviar email planilla ID: {Id}", id);
                TempData["Error"] = "Error al enviar el correo. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ENVIAR PDF A TODOS POR EMAIL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarTodosPorEmail(int periodoId)
        {
            try
            {
                var planillas = await _context.PlanillasEmpleado
                    .Include(pe => pe.Empleado)
                    .Include(pe => pe.PeriodoPago)
                    .Where(pe => pe.PeriodoPagoId == periodoId)
                    .ToListAsync();

                if (!planillas.Any())
                {
                    TempData["Error"] = "No hay planillas para este período.";
                    return RedirectToAction(nameof(Index), new { periodoId });
                }

                var emailSvc = HttpContext.RequestServices.GetRequiredService<EmailService>();
                int enviados = 0;

                foreach (var planilla in planillas)
                {
                    var correo = planilla.Empleado.CorreoElectronico;
                    var nombreCompleto = $"{planilla.Empleado.PrimerApellido} {planilla.Empleado.Nombre}";

                    if (string.IsNullOrWhiteSpace(correo))
                        continue;

                    try
                    {
                        var pdfBytes = _servicioPDF.GenerarPDFSinFirmas(planilla,
                            HttpContext.Session.GetString("Usuario") ?? "Sistema");
                        var nombreArchivo =
                            $"Comprobante_{planilla.Empleado.PrimerApellido}_" +
                            $"{planilla.PeriodoPago.Descripcion.Replace(" ", "_").Replace("—", "")}.pdf";

                        var asunto = $"Comprobante de Pago — {planilla.PeriodoPago.Descripcion}";
                        var cuerpo = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;'>
    <div style='background:#1A1A2E;padding:20px;'>
        <h2 style='color:#FF7A00;margin:0;'>Ferretería El Pana SRL</h2>
        <p style='color:#888;margin:4px 0 0;font-size:13px;'>
            Departamento de Recursos Humanos
        </p>
    </div>
    <div style='padding:20px;border:1px solid #eee;'>
        <p>Estimado(a) <strong>{planilla.Empleado.PrimerApellido}
           {planilla.Empleado.Nombre}</strong>,</p>
        <p>Adjunto encontrará su comprobante de pago correspondiente al
           período <strong>{planilla.PeriodoPago.Descripcion}</strong>.</p>
        <table style='width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;'>
            <tr style='background:#f9f9f9;'>
                <td style='padding:8px;border:1px solid #eee;'>Total Devengado</td>
                <td style='padding:8px;border:1px solid #eee;
                           font-weight:bold;color:#1B5E20;'>
                    ₡{planilla.TotalDevengado:N2}
                </td>
            </tr>
            <tr>
                <td style='padding:8px;border:1px solid #eee;'>Total Deducciones</td>
                <td style='padding:8px;border:1px solid #eee;color:#B71C1C;'>
                    ₡{planilla.TotalDeducciones:N2}
                </td>
            </tr>
            <tr style='background:#fff9f0;'>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    Neto a Pagar
                </td>
                <td style='padding:8px;border:1px solid #eee;
                           font-weight:bold;font-size:16px;color:#FF7A00;'>
                    ₡{planilla.NetoAPagar:N2}
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
                            nombreCompleto,
                            asunto, cuerpo, pdfBytes, nombreArchivo);

                        if (enviado)
                            enviados++;
                    }
                    catch
                    {
                        // Continuar con el siguiente sin detenerse
                    }
                }

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Enviar comprobantes masivo por email", "Planilla",
                    $"PeriodoId: {periodoId} — Enviados: {enviados}");

                TempData["Success"] = "Se enviaron los correos exitosamente.";

                return RedirectToAction(nameof(Index), new { periodoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correos masivos. PeriodoId: {P}", periodoId);
                TempData["Error"] = "Error al enviar los correos. Intentá de nuevo.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
        }
    }
}