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

        private const decimal PorcentajeCCSS = 10.83m;

        public PlanillaController(
            ApplicationDbContext context,
            ILogger<PlanillaController> logger,
            ComprobantePlanillaService servicioPDF)
        {
            _context = context;
            _logger = logger;
            _servicioPDF = servicioPDF;
        }

        // ── INDEX ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(int? periodoId)
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
                ViewBag.PeriodoId = periodoId;

                if (periodoId == null)
                {
                    var activo = periodos.FirstOrDefault(p => p.Estado == EstadoPeriodo.Abierto)
                              ?? periodos.FirstOrDefault();
                    periodoId = activo?.PeriodoPagoId;
                    ViewBag.PeriodoId = periodoId;
                }

                if (periodoId == null)
                    return View(new List<PlanillaEmpleado>());

                var periodo = periodos.First(p => p.PeriodoPagoId == periodoId);
                ViewBag.Periodo = periodo;

                var planillas = await _context.PlanillasEmpleado
                    .Include(pe => pe.Empleado)
                    .Include(pe => pe.PeriodoPago)
                    .AsNoTracking()
                    .Where(pe => pe.PeriodoPagoId == periodoId)
                    .OrderBy(pe => pe.Empleado.PrimerApellido)
                    .ThenBy(pe => pe.Empleado.Nombre)
                    .ToListAsync();

                ViewBag.TotalEmpleados = planillas.Count;
                ViewBag.TotalDevengado = planillas.Sum(p => p.TotalDevengado);
                ViewBag.TotalDeducciones = planillas.Sum(p => p.TotalDeducciones);
                ViewBag.TotalNeto = planillas.Sum(p => p.NetoAPagar);

                return View(planillas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar planilla. PeriodoId: {P}", periodoId);
                TempData["Error"] = "Ocurrió un error al cargar la planilla. Intentá de nuevo.";
                return View(new List<PlanillaEmpleado>());
            }
        }

        // ── CALCULAR PLANILLA ─────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize("RRHH")]
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

                var existentes = await _context.PlanillasEmpleado
                    .Where(pe => pe.PeriodoPagoId == periodoId)
                    .ToListAsync();
                _context.PlanillasEmpleado.RemoveRange(existentes);
                await _context.SaveChangesAsync();

                var empleados = await _context.Empleados
                    .Where(e => e.Activo)
                    .ToListAsync();

                foreach (var empleado in empleados)
                {
                    // ── DEVENGADOS ─────────────────────────────────────────

                    var salarioOrdinario = Math.Round(
                        empleado.ValorHora * empleado.HorasQuincenales, 2);

                    var horasExtrasReg = await _context.HorasExtras
                        .FirstOrDefaultAsync(h =>
                            h.EmpleadoId == empleado.EmpleadoId &&
                            h.PeriodoPagoId == periodoId);
                    var montoHorasExtras = horasExtrasReg?.MontoTotal ?? 0m;
                    var totalHorasExtras = horasExtrasReg?.TotalHoras ?? 0m;

                    var montoComisiones = await _context.Comisiones
                        .Where(c =>
                            c.EmpleadoId == empleado.EmpleadoId &&
                            c.Fecha >= periodo.FechaInicio &&
                            c.Fecha <= periodo.FechaFin)
                        .SumAsync(c => c.Monto);

                    var montoFeriados = await _context.PagosFeriado
                        .Where(pf =>
                            pf.EmpleadoId == empleado.EmpleadoId &&
                            pf.PeriodoPagoId == periodoId)
                        .SumAsync(pf => pf.MontoTotal);

                    var totalDevengado = Math.Round(
                        salarioOrdinario +
                        montoHorasExtras +
                        montoComisiones +
                        montoFeriados, 2);

                    // ── DEDUCCIONES ────────────────────────────────────────

                    var deduccionCCSS = Math.Round(
                        totalDevengado * (PorcentajeCCSS / 100m), 2);

                    var prestamo = await _context.Prestamos
                        .FirstOrDefaultAsync(p =>
                            p.EmpleadoId == empleado.EmpleadoId &&
                            p.Activo);

                    decimal deduccionPrestamo = 0m;
                    if (prestamo != null)
                    {
                        var cuotaQuincenal = Math.Round(prestamo.CuotaMensual / 2m, 2);
                        deduccionPrestamo = Math.Min(cuotaQuincenal, prestamo.Monto);
                        deduccionPrestamo = Math.Round(deduccionPrestamo, 2);
                    }

                    var creditosActivos = await _context.CreditosFerreteria
                        .Where(c =>
                            c.EmpleadoId == empleado.EmpleadoId &&
                            c.Activo)
                        .ToListAsync();
                    var deduccionCredito = Math.Round(
                        creditosActivos.Sum(c => Math.Min(c.CuotaQuincenal, c.Saldo)), 2);

                    var deduccionIncapacidad = await _context.Incapacidades
                        .Where(i =>
                            i.EmpleadoId == empleado.EmpleadoId &&
                            i.ResponsablePago == ResponsablePago.Patrono &&
                            i.FechaInicio <= periodo.FechaFin &&
                            i.FechaFin >= periodo.FechaInicio)
                        .SumAsync(i => i.MontoTotal);
                    deduccionIncapacidad = Math.Round(deduccionIncapacidad, 2);

                    var totalDeducciones = Math.Round(
                        deduccionCCSS +
                        deduccionPrestamo +
                        deduccionCredito +
                        deduccionIncapacidad, 2);

                    var netoAPagar = Math.Max(0,
                        Math.Round(totalDevengado - totalDeducciones, 2));

                    var planilla = new PlanillaEmpleado
                    {
                        PeriodoPagoId = periodoId,
                        EmpleadoId = empleado.EmpleadoId,
                        HorasOrdinarias = empleado.HorasQuincenales,
                        HorasExtras = totalHorasExtras,
                        HorasNoLaboradas = 0m,
                        ValorHora = empleado.ValorHora,
                        ValorHoraExtra = empleado.ValorHoraExtra,
                        SalarioOrdinario = salarioOrdinario,
                        AumentoAplicado = montoComisiones,
                        MontoHorasExtras = montoHorasExtras,
                        MontoFeriados = montoFeriados,
                        TotalDevengado = totalDevengado,
                        PorcentajeCCSS = PorcentajeCCSS,
                        DeduccionCCSS = deduccionCCSS,
                        DeduccionPrestamos = deduccionPrestamo,
                        DeduccionCreditoFerreteria = deduccionCredito,
                        DeduccionIncapacidad = deduccionIncapacidad,
                        DeduccionHorasNoLaboradas = 0m,
                        OtrasDeducciones = 0m,
                        TotalDeducciones = totalDeducciones,
                        NetoAPagar = netoAPagar
                    };

                    _context.PlanillasEmpleado.Add(planilla);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Planilla calculada: PeriodoId {P} Empleados {E}",
                    periodoId, empleados.Count);
                TempData["Success"] = $"Planilla calculada para {empleados.Count} empleado(s).";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular planilla. PeriodoId: {P}", periodoId);
                TempData["Error"] = "Ocurrió un error al calcular la planilla. Intentá de nuevo.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
        }

        // ── EDITAR LÍNEA DE PLANILLA ──────────────────────────────────────────

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
                    return RedirectToAction(nameof(Index),
                        new { periodoId = planilla.PeriodoPagoId });
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize("RRHH")]
        public async Task<IActionResult> Editar(int id, PlanillaEmpleado model)
        {
            try
            {
                if (id != model.PlanillaEmpleadoId) return NotFound();

                var registro = await _context.PlanillasEmpleado
                    .Include(pe => pe.PeriodoPago)
                    .Include(pe => pe.Empleado)
                    .FirstOrDefaultAsync(pe => pe.PlanillaEmpleadoId == id);

                if (registro == null) return NotFound();

                if (registro.PeriodoPago.Estado == EstadoPeriodo.Cerrado)
                {
                    TempData["Error"] = "No se puede editar una planilla cerrada.";
                    return RedirectToAction(nameof(Index),
                        new { periodoId = registro.PeriodoPagoId });
                }

                registro.AumentoAplicado = Math.Round(model.AumentoAplicado, 2);
                registro.HorasNoLaboradas = Math.Round(model.HorasNoLaboradas, 2);
                registro.OtrasDeducciones = Math.Round(model.OtrasDeducciones, 2);

                registro.DeduccionHorasNoLaboradas = Math.Round(
                    registro.HorasNoLaboradas * registro.ValorHora, 2);

                registro.TotalDevengado = Math.Round(
                    registro.SalarioOrdinario +
                    registro.AumentoAplicado +
                    registro.MontoHorasExtras +
                    registro.MontoFeriados, 2);

                registro.DeduccionCCSS = Math.Round(
                    registro.TotalDevengado * (PorcentajeCCSS / 100m), 2);

                registro.TotalDeducciones = Math.Round(
                    registro.DeduccionCCSS +
                    registro.DeduccionPrestamos +
                    registro.DeduccionCreditoFerreteria +
                    registro.DeduccionIncapacidad +
                    registro.DeduccionHorasNoLaboradas +
                    registro.OtrasDeducciones, 2);

                registro.NetoAPagar = Math.Max(0,
                    Math.Round(registro.TotalDevengado - registro.TotalDeducciones, 2));

                _context.Update(registro);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Línea planilla editada: ID {Id}", id);
                TempData["Success"] = $"Planilla de {registro.Empleado.PrimerApellido} " +
                    $"{registro.Empleado.Nombre} actualizada. Neto: ₡{registro.NetoAPagar:N0}";
                return RedirectToAction(nameof(Index),
                    new { periodoId = registro.PeriodoPagoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar línea planilla ID: {Id}", id);
                TempData["Error"] = "Error al guardar los cambios.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── CERRAR PERÍODO ────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize("RRHH")]
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

                foreach (var planilla in planillas.Where(p => p.DeduccionPrestamos > 0))
                {
                    var prestamo = await _context.Prestamos
                        .FirstOrDefaultAsync(p =>
                            p.EmpleadoId == planilla.EmpleadoId && p.Activo);

                    if (prestamo != null)
                    {
                        prestamo.Monto = Math.Max(0,
                            Math.Round(prestamo.Monto - planilla.DeduccionPrestamos, 2));
                        if (prestamo.Monto <= 0)
                            prestamo.Activo = false;
                    }
                }

                foreach (var planilla in planillas.Where(p => p.DeduccionCreditoFerreteria > 0))
                {
                    var creditos = await _context.CreditosFerreteria
                        .Where(c => c.EmpleadoId == planilla.EmpleadoId && c.Activo)
                        .ToListAsync();

                    var montoRestante = planilla.DeduccionCreditoFerreteria;
                    foreach (var credito in creditos)
                    {
                        if (montoRestante <= 0) break;
                        var abono = Math.Min(credito.CuotaQuincenal, credito.Saldo);
                        abono = Math.Min(abono, montoRestante);
                        credito.Saldo = Math.Max(0, Math.Round(credito.Saldo - abono, 2));
                        montoRestante -= abono;
                        if (credito.Saldo <= 0)
                            credito.Activo = false;
                    }
                }

                periodo.Estado = EstadoPeriodo.Cerrado;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Período cerrado con planilla: ID {P}", periodoId);
                TempData["Success"] = $"Período {periodo.Descripcion} cerrado. " +
                    "Saldos de préstamos y créditos actualizados correctamente.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar período planilla: {P}", periodoId);
                TempData["Error"] = "Error al cerrar el período. Intentá de nuevo.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
        }

        // ── REABRIR PERÍODO ───────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize("RRHH")]
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

                periodo.Estado = EstadoPeriodo.Abierto;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Período reabierto desde planilla: ID {P}", periodoId);
                TempData["Success"] = $"Período {periodo.Descripcion} reabierto. " +
                    "Podés recalcular la planilla.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reabrir período desde planilla: {P}", periodoId);
                TempData["Error"] = "Error al reabrir el período. Intentá de nuevo.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
        }

        // ── DESCARGAR PDF ─────────────────────────────────────────────────────

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
                var nombreArchivo = $"Comprobante_{planilla.Empleado.PrimerApellido}_" +
                    $"{planilla.PeriodoPago.Descripcion.Replace(" ", "_").Replace("—", "")}.pdf";

                return File(pdfBytes, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF planilla ID: {Id}", id);
                TempData["Error"] = "Error al generar el PDF. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── EXPORTAR EXCEL ────────────────────────────────────────────────────

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

                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var ws = workbook.Worksheets.Add("Planilla");

                // ── Título ────────────────────────────────────────────────────
                ws.Cell(1, 1).Value = "FERRETERÍA EL PANA SRL";
                ws.Range(1, 1, 1, 16).Merge().Style
                    .Font.SetBold(true).Font.SetFontSize(14)
                    .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

                ws.Cell(2, 1).Value = "DEPARTAMENTO DE RECURSOS HUMANOS";
                ws.Range(2, 1, 2, 16).Merge().Style
                    .Font.SetBold(true).Font.SetFontSize(12)
                    .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

                ws.Cell(3, 1).Value = $"PLANILLA QUINCENAL — {periodo.Descripcion}";
                ws.Range(3, 1, 3, 16).Merge().Style
                    .Font.SetBold(true).Font.SetFontSize(11)
                    .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

                ws.Cell(4, 1).Value = $"PERÍODO: {periodo.FechaInicio:dd/MM/yyyy} AL {periodo.FechaFin:dd/MM/yyyy}";
                ws.Range(4, 1, 4, 16).Merge().Style
                    .Font.SetItalic(true)
                    .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);

                // ── Encabezados ───────────────────────────────────────────────
                var headers = new[]
                {
            "Nombre y Apellidos", "Cédula", "Departamento", "Puesto",
            "Sal. Ordinario", "Hrs. Extras", "Comisión", "Feriados",
            "Total Devengado", "CCSS 10.83%", "Préstamo",
            "Cré. Ferretería", "Incapacidad", "Hrs. No Lab.",
            "Total Deducciones", "Neto a Pagar"
        };

                int fila = 6;
                for (int col = 1; col <= headers.Length; col++)
                {
                    var cell = ws.Cell(fila, col);
                    cell.Value = headers[col - 1];
                    cell.Style
                        .Font.SetBold(true)
                        .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                        .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#FF7A00"))
                        .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(ClosedXML.Excel.XLBorderStyleValues.Thin);
                }

                // ── Datos agrupados por departamento ──────────────────────────
                fila = 7;
                var grupos = planillas.GroupBy(p => p.Empleado.Departamento);

                foreach (var grupo in grupos)
                {
                    // Fila de departamento
                    ws.Cell(fila, 1).Value = grupo.Key.ToUpper();
                    ws.Range(fila, 1, fila, 16).Merge().Style
                        .Font.SetBold(true)
                        .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                        .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#333333"))
                        .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Left);
                    fila++;

                    foreach (var p in grupo)
                    {
                        ws.Cell(fila, 1).Value = $"{p.Empleado.PrimerApellido} {p.Empleado.SegundoApellido} {p.Empleado.Nombre}";
                        ws.Cell(fila, 2).Value = p.Empleado.Cedula;
                        ws.Cell(fila, 3).Value = p.Empleado.Departamento;
                        ws.Cell(fila, 4).Value = p.Empleado.Puesto;
                        ws.Cell(fila, 5).Value = p.SalarioOrdinario;
                        ws.Cell(fila, 6).Value = p.MontoHorasExtras;
                        ws.Cell(fila, 7).Value = p.AumentoAplicado;
                        ws.Cell(fila, 8).Value = p.MontoFeriados;
                        ws.Cell(fila, 9).Value = p.TotalDevengado;
                        ws.Cell(fila, 10).Value = p.DeduccionCCSS;
                        ws.Cell(fila, 11).Value = p.DeduccionPrestamos;
                        ws.Cell(fila, 12).Value = p.DeduccionCreditoFerreteria;
                        ws.Cell(fila, 13).Value = p.DeduccionIncapacidad;
                        ws.Cell(fila, 14).Value = p.DeduccionHorasNoLaboradas;
                        ws.Cell(fila, 15).Value = p.TotalDeducciones;
                        ws.Cell(fila, 16).Value = p.NetoAPagar;

                        // Formato moneda columnas 5-16
                        for (int col = 5; col <= 16; col++)
                            ws.Cell(fila, col).Style.NumberFormat.Format = "₡#,##0.00";

                        // Color alterno
                        if (fila % 2 == 0)
                            ws.Range(fila, 1, fila, 16).Style
                                .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#FFF3E0"));

                        // Bordes
                        ws.Range(fila, 1, fila, 16).Style
                            .Border.SetOutsideBorder(ClosedXML.Excel.XLBorderStyleValues.Thin)
                            .Border.SetInsideBorder(ClosedXML.Excel.XLBorderStyleValues.Hair);

                        fila++;
                    }

                    // Subtotales por departamento
                    var subtotalFila = fila;
                    ws.Cell(subtotalFila, 1).Value = $"Subtotal {grupo.Key}";
                    ws.Range(subtotalFila, 1, subtotalFila, 4).Merge();
                    ws.Cell(subtotalFila, 5).Value = grupo.Sum(p => p.SalarioOrdinario);
                    ws.Cell(subtotalFila, 6).Value = grupo.Sum(p => p.MontoHorasExtras);
                    ws.Cell(subtotalFila, 7).Value = grupo.Sum(p => p.AumentoAplicado);
                    ws.Cell(subtotalFila, 8).Value = grupo.Sum(p => p.MontoFeriados);
                    ws.Cell(subtotalFila, 9).Value = grupo.Sum(p => p.TotalDevengado);
                    ws.Cell(subtotalFila, 10).Value = grupo.Sum(p => p.DeduccionCCSS);
                    ws.Cell(subtotalFila, 11).Value = grupo.Sum(p => p.DeduccionPrestamos);
                    ws.Cell(subtotalFila, 12).Value = grupo.Sum(p => p.DeduccionCreditoFerreteria);
                    ws.Cell(subtotalFila, 13).Value = grupo.Sum(p => p.DeduccionIncapacidad);
                    ws.Cell(subtotalFila, 14).Value = grupo.Sum(p => p.DeduccionHorasNoLaboradas);
                    ws.Cell(subtotalFila, 15).Value = grupo.Sum(p => p.TotalDeducciones);
                    ws.Cell(subtotalFila, 16).Value = grupo.Sum(p => p.NetoAPagar);

                    ws.Range(subtotalFila, 1, subtotalFila, 16).Style
                        .Font.SetBold(true)
                        .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#FFE0B2"))
                        .Border.SetOutsideBorder(ClosedXML.Excel.XLBorderStyleValues.Medium);

                    for (int col = 5; col <= 16; col++)
                        ws.Cell(subtotalFila, col).Style.NumberFormat.Format = "₡#,##0.00";

                    fila++;
                }

                // ── Totales generales ─────────────────────────────────────────
                ws.Cell(fila, 1).Value = "TOTAL GENERAL";
                ws.Range(fila, 1, fila, 4).Merge();
                ws.Cell(fila, 5).Value = planillas.Sum(p => p.SalarioOrdinario);
                ws.Cell(fila, 6).Value = planillas.Sum(p => p.MontoHorasExtras);
                ws.Cell(fila, 7).Value = planillas.Sum(p => p.AumentoAplicado);
                ws.Cell(fila, 8).Value = planillas.Sum(p => p.MontoFeriados);
                ws.Cell(fila, 9).Value = planillas.Sum(p => p.TotalDevengado);
                ws.Cell(fila, 10).Value = planillas.Sum(p => p.DeduccionCCSS);
                ws.Cell(fila, 11).Value = planillas.Sum(p => p.DeduccionPrestamos);
                ws.Cell(fila, 12).Value = planillas.Sum(p => p.DeduccionCreditoFerreteria);
                ws.Cell(fila, 13).Value = planillas.Sum(p => p.DeduccionIncapacidad);
                ws.Cell(fila, 14).Value = planillas.Sum(p => p.DeduccionHorasNoLaboradas);
                ws.Cell(fila, 15).Value = planillas.Sum(p => p.TotalDeducciones);
                ws.Cell(fila, 16).Value = planillas.Sum(p => p.NetoAPagar);

                ws.Range(fila, 1, fila, 16).Style
                    .Font.SetBold(true).Font.SetFontSize(11)
                    .Font.SetFontColor(ClosedXML.Excel.XLColor.White)
                    .Fill.SetBackgroundColor(ClosedXML.Excel.XLColor.FromHtml("#FF7A00"))
                    .Border.SetOutsideBorder(ClosedXML.Excel.XLBorderStyleValues.Medium);

                for (int col = 5; col <= 16; col++)
                    ws.Cell(fila, col).Style.NumberFormat.Format = "₡#,##0.00";

                // ── Ajustar columnas ──────────────────────────────────────────
                ws.Column(1).Width = 30; // Nombre
                ws.Column(2).Width = 14; // Cédula
                ws.Column(3).Width = 14; // Departamento
                ws.Column(4).Width = 20; // Puesto
                for (int col = 5; col <= 16; col++)
                    ws.Column(col).Width = 16;

                // Congelar encabezados
                ws.SheetView.FreezeRows(6);

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var nombreArchivo = $"Planilla_{periodo.Descripcion.Replace(" ", "_").Replace("—", "")}.xlsx";
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar Excel planilla. PeriodoId: {P}", periodoId);
                TempData["Error"] = "Error al generar el Excel. Intentá de nuevo.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
        }

        // ── EXPORTAR PDF GENERAL ──────────────────────────────────────────────

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

                var pdfBytes = _servicioPDF.GenerarPDFPlanillaGeneral(planillas, periodo);
                var nombreArchivo = $"Planilla_{periodo.Descripcion.Replace(" ", "_").Replace("—", "")}.pdf";

                return File(pdfBytes, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar PDF planilla. PeriodoId: {P}", periodoId);
                TempData["Error"] = "Error al generar el PDF. Intentá de nuevo.";
                return RedirectToAction(nameof(Index), new { periodoId });
            }
        }
    }
}