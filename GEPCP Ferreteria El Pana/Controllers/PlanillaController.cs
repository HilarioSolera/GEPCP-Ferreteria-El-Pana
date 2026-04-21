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
                TempData["Error"] = "Ocurrió un error al cargar la planilla.";
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

                // Validación: Solo incluir empleados cuya fecha de ingreso sea anterior o igual al inicio del período
                var empleadosExcluidos = todosEmpleados
                    .Where(e => e.FechaIngreso > periodo.FechaInicio).ToList();
                var empleados = todosEmpleados
                    .Where(e => e.FechaIngreso <= periodo.FechaInicio).ToList();

                if (empleadosExcluidos.Any())
                    TempData["Warning"] =
                        $"Excluidos por fecha de ingreso posterior al período: " +
                        string.Join(", ", empleadosExcluidos
                            .Select(e => $"{e.PrimerApellido} {e.Nombre} (ingresó: {e.FechaIngreso:dd/MM/yyyy})"));

                if (!empleados.Any())
                {
                    TempData["Error"] = "No hay empleados elegibles para calcular en este período.";
                    return RedirectToAction(nameof(Index), new { periodoId });
                }

var abonosPeriodoExistentes = await _context.AbonosPrestamo
    .Where(a => a.Observaciones != null &&
                a.Observaciones.Contains("Deducción planilla") &&
                a.Observaciones.Contains(periodo.Descripcion))
    .Select(a => a.Prestamo.EmpleadoId)
    .ToListAsync();

foreach (var empleado in empleados)
{
    var salarioOrdinario = Math.Round(
        empleado.ValorHora * empleado.HorasPorPeriodo, 2);

    var horasExtrasReg = await _context.HorasExtras
        .FirstOrDefaultAsync(h =>
            h.EmpleadoId == empleado.EmpleadoId &&
            h.PeriodoPagoId == periodoId);
    var montoHorasExtras = horasExtrasReg?.MontoTotal ?? 0m;
    var totalHorasExtras = horasExtrasReg?.TotalHoras ?? 0m;

    var montoComisiones = (await _context.Comisiones
        .Where(c =>
            c.EmpleadoId == empleado.EmpleadoId &&
            c.Fecha >= periodo.FechaInicio &&
            c.Fecha <= periodo.FechaFin)
        .Select(c => c.Monto)
        .ToListAsync()).Sum();

    var montoFeriados = (await _context.PagosFeriado
        .Where(pf =>
            pf.EmpleadoId == empleado.EmpleadoId &&
            pf.PeriodoPagoId == periodoId)
        .Select(pf => pf.MontoTotal)
        .ToListAsync()).Sum();

    var totalDevengado = Math.Round(
        salarioOrdinario + montoHorasExtras +
        montoComisiones + montoFeriados, 2);

    var deduccionCCSS = Math.Round(
        totalDevengado * (periodo.PorcentajeCCSS / 100m), 2);

    // Calculo de ISR sobre el devengado del periodo
    var deduccionRenta = CalcularImpuestoRenta(totalDevengado, periodo, empleado.NumHijos, empleado.TieneConyuge);

    decimal deduccionPrestamo = 0m;
    if (!abonosPeriodoExistentes.Contains(empleado.EmpleadoId))
    {
        var prestamo = await _context.Prestamos
            .FirstOrDefaultAsync(p =>
                p.EmpleadoId == empleado.EmpleadoId && p.Activo);
        if (prestamo != null && prestamo.Monto > 0)
        {
            var cuotaPeriodo = Math.Round(
                prestamo.CuotaMensual / empleado.FactorCuotaPrestamo, 2);
            deduccionPrestamo = Math.Round(
                Math.Min(cuotaPeriodo, prestamo.Monto), 2);

            // Validación Art. 172 CT: deducción no puede exceder 50% del salario
            var maxDeduccion = Math.Round(salarioOrdinario * 0.50m, 2);
            deduccionPrestamo = Math.Min(deduccionPrestamo, maxDeduccion);
        }
    }

    var creditosActivos = await _context.CreditosFerreteria
        .Where(c => c.EmpleadoId == empleado.EmpleadoId && c.Activo)
        .ToListAsync();
    var deduccionCredito = Math.Round(
        creditosActivos
            .Where(c => c.Saldo > 0)
            .Sum(c => Math.Min(c.CuotaQuincenal, c.Saldo)), 2);

    var diasIncapacidad = (await _context.Incapacidades
        .Where(i =>
            i.EmpleadoId == empleado.EmpleadoId &&
            i.FechaInicio <= periodo.FechaFin &&
            i.FechaFin >= periodo.FechaInicio)
        .ToListAsync())
        .Sum(i =>
        {
            var inicio = i.FechaInicio < periodo.FechaInicio
                ? periodo.FechaInicio : i.FechaInicio;
            var fin = i.FechaFin > periodo.FechaFin
                ? periodo.FechaFin : i.FechaFin;
            return Math.Max(0, (fin - inicio).Days + 1);
        });
    var salarioDiario = Math.Round(empleado.SalarioBase / 30m, 2);
    var deduccionIncapacidad = Math.Round(salarioDiario * diasIncapacidad, 2);

    var deduccionVacaciones = 0m;

    var totalDeducciones = Math.Round(
        deduccionCCSS + deduccionRenta +
        deduccionPrestamo + deduccionCredito +
        deduccionIncapacidad + deduccionVacaciones, 2);

    var netoAPagar = Math.Max(0,
        Math.Round(totalDevengado - totalDeducciones, 2));

    // Alerta si las deducciones exceden el salario
    if (netoAPagar == 0 && totalDeducciones > 0)
    {
        TempData["Warning"] = $"ADVERTENCIA: El empleado {empleado.Nombre} tiene deducciones que igualan o exceden su salario (Neto = ₡0). Revise las deducciones.";
    }

    var planillaExistente = await _context.PlanillasEmpleado
        .FirstOrDefaultAsync(p =>
            p.EmpleadoId == empleado.EmpleadoId &&
            p.PeriodoPagoId == periodoId);

    if (planillaExistente != null)
    {
        planillaExistente.HorasOrdinarias = empleado.HorasPorPeriodo;
        planillaExistente.HorasExtras = totalHorasExtras;
        planillaExistente.HorasNoLaboradas = 0m;
        planillaExistente.ValorHora = empleado.ValorHora;
        planillaExistente.ValorHoraExtra = empleado.ValorHoraExtra;
        planillaExistente.SalarioOrdinario = salarioOrdinario;
        planillaExistente.AumentoAplicado = montoComisiones;
        planillaExistente.MontoHorasExtras = montoHorasExtras;
        planillaExistente.MontoFeriados = montoFeriados;
        planillaExistente.TotalDevengado = totalDevengado;
        planillaExistente.DeduccionCCSS = deduccionCCSS;
        planillaExistente.DeduccionRenta = deduccionRenta;
        planillaExistente.DeduccionPrestamos = deduccionPrestamo;
        planillaExistente.DeduccionCreditoFerreteria = deduccionCredito;
        planillaExistente.DeduccionIncapacidad = deduccionIncapacidad;
        planillaExistente.DeduccionVacaciones = deduccionVacaciones;
        planillaExistente.DeduccionHorasNoLaboradas = 0m;
        planillaExistente.OtrasDeducciones = 0m;
        planillaExistente.TotalDeducciones = totalDeducciones;
        planillaExistente.NetoAPagar = netoAPagar;
    }
    else
    {
        _context.PlanillasEmpleado.Add(new PlanillaEmpleado
        {
            PeriodoPagoId              = periodoId,
            PorcentajeCCSS             = periodo.PorcentajeCCSS,
            EmpleadoId                 = empleado.EmpleadoId,
            HorasOrdinarias            = empleado.HorasPorPeriodo,
            HorasExtras                = totalHorasExtras,
            HorasNoLaboradas           = 0m,
            ValorHora                  = empleado.ValorHora,
            ValorHoraExtra             = empleado.ValorHoraExtra,
            SalarioOrdinario           = salarioOrdinario,
            AumentoAplicado            = montoComisiones,
            MontoHorasExtras           = montoHorasExtras,
            MontoFeriados              = montoFeriados,
            TotalDevengado             = totalDevengado,
            DeduccionCCSS              = deduccionCCSS,
            DeduccionRenta             = deduccionRenta,
            DeduccionPrestamos         = deduccionPrestamo,
            DeduccionCreditoFerreteria = deduccionCredito,
            DeduccionIncapacidad       = deduccionIncapacidad,
            DeduccionVacaciones        = deduccionVacaciones,
            DeduccionHorasNoLaboradas  = 0m,
            OtrasDeducciones           = 0m,
            TotalDeducciones           = totalDeducciones,
            NetoAPagar                 = netoAPagar
        });
    }
}

                await _context.SaveChangesAsync();

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

                registro.AumentoAplicado = Math.Round(model.AumentoAplicado, 2);
                registro.HorasNoLaboradas = Math.Round(model.HorasNoLaboradas, 2);
                registro.OtrasDeducciones = Math.Round(model.OtrasDeducciones, 2);
                registro.DescripcionOtrasDeducciones = model.DescripcionOtrasDeducciones?.Trim();

                registro.TotalDevengado = Math.Round(
                    registro.SalarioOrdinario + registro.AumentoAplicado +
                    registro.MontoHorasExtras + registro.MontoFeriados, 2);

                registro.DeduccionCCSS = Math.Round(
                    registro.TotalDevengado * (registro.PorcentajeCCSS / 100m), 2);

                // Calculo de ISR sobre el devengado del periodo
                registro.DeduccionRenta = CalcularImpuestoRenta(registro.TotalDevengado,
                    registro.PeriodoPago, registro.Empleado.NumHijos, registro.Empleado.TieneConyuge);

                registro.TotalDeducciones = Math.Round(
                    registro.DeduccionCCSS +
                    registro.DeduccionRenta +
                    registro.DeduccionPrestamos +
                    registro.DeduccionCreditoFerreteria +
                    registro.DeduccionIncapacidad +
                    registro.DeduccionVacaciones +
                    registro.DeduccionHorasNoLaboradas +
                    registro.OtrasDeducciones, 2);

                registro.NetoAPagar = Math.Max(0, registro.TotalDevengado - registro.TotalDeducciones);

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

                // Préstamos
                foreach (var planilla in planillas.Where(p => p.DeduccionPrestamos > 0))
                {
                    if (empleadosConAbonoPrestamo.Contains(planilla.EmpleadoId)) continue;
                    var prestamo = await _context.Prestamos
                        .FirstOrDefaultAsync(p => p.EmpleadoId == planilla.EmpleadoId && p.Activo);
                    if (prestamo == null) continue;

                    var saldoAnterior = prestamo.Monto;
                    var montoAbono = Math.Round(Math.Min(planilla.DeduccionPrestamos, prestamo.Monto), 2);
                    prestamo.Monto = Math.Max(0, Math.Round(prestamo.Monto - montoAbono, 2));
                    if (prestamo.Monto <= 0) prestamo.Activo = false;

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
                await _context.SaveChangesAsync();

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

                if (planilla == null) return NotFound();

                var pdfBytes = _servicioPDF.GenerarPDF(planilla);
                var nombre = $"Comprobante_{planilla.Empleado.PrimerApellido}_{planilla.PeriodoPago.Descripcion.Replace(" ", "_").Replace("—", "")}.pdf";

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Descargar PDF planilla", "Planilla",
                    $"{planilla.Empleado.PrimerApellido} {planilla.Empleado.Nombre}");

                return File(pdfBytes, "application/pdf", nombre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF planilla ID: {Id}", id);
                TempData["Error"] = "Error al generar el PDF. Intentá de nuevo.";
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
                ws.Range(1, 1, 1, 18).Merge().Style
                    .Font.SetBold(true).Font.SetFontSize(14)
                    .Alignment.SetHorizontal(
                        ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

                ws.Cell(2, 1).Value = "DEPARTAMENTO DE RECURSOS HUMANOS";
                ws.Range(2, 1, 2, 18).Merge().Style
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
                ws.Range(3, 1, 3, 18).Merge().Style
                    .Font.SetBold(true).Font.SetFontSize(11)
                    .Alignment.SetHorizontal(
                        ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

                ws.Cell(4, 1).Value =
                    $"PERÍODO: {periodo.FechaInicio:dd/MM/yyyy} " +
                    $"AL {periodo.FechaFin:dd/MM/yyyy}";
                ws.Range(4, 1, 4, 18).Merge().Style
                    .Font.SetItalic(true)
                    .Alignment.SetHorizontal(
                        ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

                var headers = new[]
                {
                    "Nombre y Apellidos", "Cédula", "Departamento", "Puesto",
                    "Sal. Ordinario", "Hrs. Extras", "Comisión", "Feriados",
                    "Total Devengado", $"CCSS {periodo.PorcentajeCCSS}%", "Renta (ISR)", "Préstamo",
                    "Cré. Ferretería", "Incapacidad", "Vac. Sin Pago",
                    "Hrs. No Lab.", "Total Deducciones", "Neto a Pagar"
                };

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
                    ws.Range(fila, 1, fila, 18).Merge().Style
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
                        ws.Cell(fila, 15).Value = p.DeduccionVacaciones;
                        ws.Cell(fila, 16).Value = p.DeduccionHorasNoLaboradas;
                        ws.Cell(fila, 17).Value = p.TotalDeducciones;
                        ws.Cell(fila, 18).Value = p.NetoAPagar;

                        for (int col = 5; col <= 18; col++)
                            ws.Cell(fila, col).Style
                                .NumberFormat.Format = "₡#,##0.00";

                        if (fila % 2 == 0)
                            ws.Range(fila, 1, fila, 18).Style
                                .Fill.SetBackgroundColor(
                                    ClosedXML.Excel.XLColor.FromHtml("#FFF3E0"));

                        ws.Range(fila, 1, fila, 18).Style
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
                    ws.Cell(fila, 15).Value = grupo.Sum(p => p.DeduccionVacaciones);
                    ws.Cell(fila, 16).Value = grupo.Sum(p => p.DeduccionHorasNoLaboradas);
                    ws.Cell(fila, 17).Value = grupo.Sum(p => p.TotalDeducciones);
                    ws.Cell(fila, 18).Value = grupo.Sum(p => p.NetoAPagar);

                    ws.Range(fila, 1, fila, 18).Style
                        .Font.SetBold(true)
                        .Fill.SetBackgroundColor(
                            ClosedXML.Excel.XLColor.FromHtml("#FFE0B2"))
                        .Border.SetOutsideBorder(
                            ClosedXML.Excel.XLBorderStyleValues.Medium);

                    for (int col = 5; col <= 18; col++)
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
                ws.Cell(fila, 15).Value = planillas.Sum(p => p.DeduccionVacaciones);
                ws.Cell(fila, 16).Value = planillas.Sum(p => p.DeduccionHorasNoLaboradas);
                ws.Cell(fila, 17).Value = planillas.Sum(p => p.TotalDeducciones);
                ws.Cell(fila, 18).Value = planillas.Sum(p => p.NetoAPagar);

                ws.Range(fila, 1, fila, 18).Style
                    .Font.SetBold(true).Font.SetFontSize(11)
                    .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                    .Fill.SetBackgroundColor(
                        ClosedXML.Excel.XLColor.FromHtml("#FF7A00"))
                    .Border.SetOutsideBorder(
                        ClosedXML.Excel.XLBorderStyleValues.Medium);

                for (int col = 5; col <= 18; col++)
                    ws.Cell(fila, col).Style.NumberFormat.Format = "₡#,##0.00";

                ws.Column(1).Width = 30;
                ws.Column(2).Width = 14;
                ws.Column(3).Width = 14;
                ws.Column(4).Width = 20;
                for (int col = 5; col <= 18; col++) ws.Column(col).Width = 16;
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

                var pdfBytes = _servicioPDF.GenerarPDFPlanillaGeneral(planillas, periodo);
                var nombre =
                    $"Planilla_{periodo.Descripcion.Replace(" ", "_").Replace("—", "")}.pdf";

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
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
                    : "Error al enviar el correo. Verificá la configuración SMTP.";

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
                int sinCorreo = 0;
                int errores = 0;
                var detalleErrores = new List<string>();

                foreach (var planilla in planillas)
                {
                    var correo = planilla.Empleado.CorreoElectronico;
                    if (string.IsNullOrWhiteSpace(correo))
                    {
                        sinCorreo++;
                        continue;
                    }

                    try
                    {
                        var pdfBytes = _servicioPDF.GenerarPDFSinFirmas(planilla);
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
                            $"{planilla.Empleado.PrimerApellido} {planilla.Empleado.Nombre}",
                            asunto, cuerpo, pdfBytes, nombreArchivo);

                        if (enviado)
                            enviados++;
                        else
                        {
                            errores++;
                            detalleErrores.Add(planilla.Empleado.PrimerApellido);
                        }
                    }
                    catch
                    {
                        errores++;
                        detalleErrores.Add(planilla.Empleado.PrimerApellido);
                    }
                }

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Enviar comprobantes masivo por email", "Planilla",
                    $"PeriodoId: {periodoId} — Enviados: {enviados}, Sin correo: {sinCorreo}, Errores: {errores}");

                var msg = $"{enviados} comprobante(s) enviado(s) correctamente.";
                if (sinCorreo > 0)
                    msg += $"\n{sinCorreo} empleado(s) sin correo registrado.";
                if (errores > 0)
                    msg += $"\n{errores} error(es): {string.Join(", ", detalleErrores)}";

                TempData[errores == 0 ? "Success" : "Warning"] = msg;
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