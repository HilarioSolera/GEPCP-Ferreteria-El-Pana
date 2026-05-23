using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Text;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class HistorialSalariosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HistorialSalariosController> _logger;

        public HistorialSalariosController(
            ApplicationDbContext context,
            ILogger<HistorialSalariosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // INDEX: historial de todos los empleados

        public async Task<IActionResult> Index(string? busqueda)
        {
            try
            {
                ViewBag.Busqueda = busqueda;

                // Sin búsqueda → página vacía
                if (string.IsNullOrWhiteSpace(busqueda))
                    return View(new List<HistorialSalario>());

                var termino = busqueda.Trim().ToLower();
                var historial = await _context.HistorialSalarios
                    .Include(h => h.Empleado)
                    .AsNoTracking()
                    .Where(h =>
                        h.Empleado.Nombre.ToLower().Contains(termino) ||
                        h.Empleado.PrimerApellido.ToLower().Contains(termino) ||
                        h.Empleado.Cedula.Contains(termino))
                    .OrderByDescending(h => h.FechaCambio)
                    .ToListAsync();

                return View(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar historial de salarios");
                TempData["Error"] = "Error al cargar el historial.";
                return View(new List<HistorialSalario>());
            }
        }

        // HISTORIAL POR EMPLEADO

        public async Task<IActionResult> Empleado(int? id)
        {
            try
            {
                if (id == null || id <= 0) return NotFound();

                var empleado = await _context.Empleados
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpleadoId == id);

                if (empleado == null) return NotFound();

                var historial = await _context.HistorialSalarios
                    .AsNoTracking()
                    .Where(h => h.EmpleadoId == id)
                    .OrderByDescending(h => h.FechaCambio)
                    .ToListAsync();

                ViewBag.Empleado = empleado;
                return View(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar historial del empleado ID: {Id}", id);
                TempData["Error"] = "Error al cargar el historial.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ENVIAR HISTORIAL POR EMAIL

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarPorEmail(int? empleadoId)
        {
            try
            {
                if (empleadoId == null || empleadoId <= 0)
                {
                    TempData["Error"] = "Empleado no especificado.";
                    return RedirectToAction(nameof(Index));
                }

                var empleado = await _context.Empleados
                    .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId);

                if (empleado == null)
                {
                    TempData["Error"] = "Empleado no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                var correo = empleado.CorreoElectronico;
                if (string.IsNullOrWhiteSpace(correo))
                {
                    TempData["Error"] = $"{empleado.PrimerApellido} {empleado.Nombre} no tiene correo registrado.";
                    return RedirectToAction(nameof(Empleado), new { id = empleadoId });
                }

                var historial = await _context.HistorialSalarios
                    .AsNoTracking()
                    .Where(h => h.EmpleadoId == empleadoId)
                    .OrderByDescending(h => h.FechaCambio)
                    .ToListAsync();

                var emailSvc = HttpContext.RequestServices
                    .GetRequiredService<EmailService>();

                var asunto = $"Historial de Salarios — {empleado.PrimerApellido} {empleado.Nombre}";

                var filasHistorial = historial.Select(h =>
                {
                    var diferencia = h.SalarioNuevo - h.SalarioAnterior;
                    var estilo = diferencia >= 0 ? "color:green;" : "color:red;";
                    return $@"<tr>
                        <td style='padding:8px;border-bottom:1px solid #eee;'>{h.FechaCambio:dd/MM/yyyy HH:mm}</td>
                        <td style='padding:8px;border-bottom:1px solid #eee;text-align:right;'>₡{h.SalarioAnterior:N0}</td>
                        <td style='padding:8px;border-bottom:1px solid #eee;text-align:right;'>₡{h.SalarioNuevo:N0}</td>
                        <td style='padding:8px;border-bottom:1px solid #eee;text-align:right;{estilo}font-weight:bold;'>
                            {(diferencia >= 0 ? "+" : "")}₡{diferencia:N0}
                        </td>
                        <td style='padding:8px;border-bottom:1px solid #eee;'>{h.ModificadoPor ?? "—"}</td>
                    </tr>";
                }).Aggregate("", (a, b) => a + b);

                var cuerpo = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;'>
    <div style='background:#1A1A2E;padding:20px;'>
        <h2 style='color:#FF7A00;margin:0;'>Ferretería El Pana SRL</h2>
        <p style='color:#888;margin:4px 0 0;font-size:13px;'>
            Departamento de Recursos Humanos
        </p>
    </div>
    <div style='padding:20px;border:1px solid #eee;'>
        <p>Estimado(a) <strong>{empleado.PrimerApellido} {empleado.Nombre}</strong>,</p>
        <p>Adjunto encontrará su historial de cambios de salario registrados en el sistema.</p>

        <table style='width:100%;border-collapse:collapse;margin:20px 0;'>
            <thead>
                <tr style='background:#FF7A00;color:white;'>
                    <th style='padding:8px;text-align:left;font-weight:bold;'>Fecha</th>
                    <th style='padding:8px;text-align:right;font-weight:bold;'>Salario Anterior</th>
                    <th style='padding:8px;text-align:right;font-weight:bold;'>Salario Nuevo</th>
                    <th style='padding:8px;text-align:right;font-weight:bold;'>Diferencia</th>
                    <th style='padding:8px;text-align:left;font-weight:bold;'>Modificado por</th>
                </tr>
            </thead>
            <tbody>
                {filasHistorial}
            </tbody>
        </table>

        <p style='color:#666;font-size:0.9rem;margin-top:20px;'>
            <i class='bi bi-info-circle'></i>
            Este es un documento informativo generado automáticamente por el sistema GEPCP.
        </p>
    </div>
    <div style='background:#f5f5f5;padding:15px;border-top:1px solid #ddd;font-size:0.85rem;color:#666;'>
        <p style='margin:0;'>
            © 2024 Ferretería El Pana SRL - Todos los derechos reservados<br/>
            Este correo fue enviado automáticamente. Por favor no responder.
        </p>
    </div>
</div>";

                await emailSvc.EnviarAsync(correo, asunto, cuerpo);

                TempData["Exito"] = $"Historial enviado exitosamente a {correo}.";
                return RedirectToAction(nameof(Empleado), new { id = empleadoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar historial por email");
                TempData["Error"] = "Error al enviar el correo. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // DESCARGAR HISTORIAL COMO PDF

        public async Task<IActionResult> DescargarPDF(int? empleadoId)
        {
            try
            {
                if (empleadoId == null || empleadoId <= 0)
                    return NotFound();

                var empleado = await _context.Empleados
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId);

                if (empleado == null) return NotFound();

                var historial = await _context.HistorialSalarios
                    .AsNoTracking()
                    .Where(h => h.EmpleadoId == empleadoId)
                    .OrderByDescending(h => h.FechaCambio)
                    .ToListAsync();

                var htmlContent = GenerarHTMLHistorial(empleado, historial);
                var bytes = Encoding.UTF8.GetBytes(htmlContent);
                var nombre = $"HistorialSalarios_{empleado.PrimerApellido}_{empleado.Nombre}_{DateTime.Now:yyyyMMdd}.html";

                return File(bytes, "text/html", nombre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar PDF de historial");
                TempData["Error"] = "Error al generar el PDF.";
                return RedirectToAction(nameof(Empleado), new { id = empleadoId });
            }
        }

        // DESCARGAR HISTORIAL COMO EXCEL

        public async Task<IActionResult> DescargarExcel(int? empleadoId)
        {
            try
            {
                if (empleadoId == null || empleadoId <= 0)
                    return NotFound();

                var empleado = await _context.Empleados
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId);

                if (empleado == null) return NotFound();

                var historial = await _context.HistorialSalarios
                    .AsNoTracking()
                    .Where(h => h.EmpleadoId == empleadoId)
                    .OrderByDescending(h => h.FechaCambio)
                    .ToListAsync();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Historial");

                    // Título
                    worksheet.Cell(1, 1).Value = "Historial de Salarios";
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Font.FontSize = 14;
                    worksheet.Range(1, 1, 1, 5).Merge();

                    // Información del empleado
                    worksheet.Cell(2, 1).Value = $"Empleado: {empleado.PrimerApellido} {empleado.Nombre}";
                    worksheet.Cell(3, 1).Value = $"Cédula: {empleado.Cedula}";
                    worksheet.Cell(4, 1).Value = $"Salario Actual: ₡{empleado.SalarioBase:N0}";

                    // Encabezados
                    var headerRow = 6;
                    worksheet.Cell(headerRow, 1).Value = "Fecha";
                    worksheet.Cell(headerRow, 2).Value = "Salario Anterior";
                    worksheet.Cell(headerRow, 3).Value = "Salario Nuevo";
                    worksheet.Cell(headerRow, 4).Value = "Diferencia";
                    worksheet.Cell(headerRow, 5).Value = "Modificado por";

                    // Estilos encabezado
                    for (int col = 1; col <= 5; col++)
                    {
                        var cell = worksheet.Cell(headerRow, col);
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FF7A00");
                        cell.Style.Font.FontColor = XLColor.White;
                        cell.Style.Font.Bold = true;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    // Datos
                    int row = headerRow + 1;
                    foreach (var h in historial)
                    {
                        var diferencia = h.SalarioNuevo - h.SalarioAnterior;

                        worksheet.Cell(row, 1).Value = h.FechaCambio.ToString("dd/MM/yyyy HH:mm");
                        worksheet.Cell(row, 2).Value = h.SalarioAnterior;
                        worksheet.Cell(row, 3).Value = h.SalarioNuevo;
                        worksheet.Cell(row, 4).Value = diferencia;
                        worksheet.Cell(row, 5).Value = h.ModificadoPor ?? "—";

                        // Colorear números
                        worksheet.Cell(row, 2).Style.NumberFormat.Format = "₡#,##0";
                        worksheet.Cell(row, 3).Style.NumberFormat.Format = "₡#,##0";
                        worksheet.Cell(row, 4).Style.NumberFormat.Format = "₡#,##0";

                        if (diferencia >= 0)
                            worksheet.Cell(row, 4).Style.Font.FontColor = XLColor.Green;
                        else
                            worksheet.Cell(row, 4).Style.Font.FontColor = XLColor.Red;

                        row++;
                    }

                    // Ajustar ancho
                    worksheet.Column(1).Width = 20;
                    worksheet.Column(2).Width = 18;
                    worksheet.Column(3).Width = 18;
                    worksheet.Column(4).Width = 18;
                    worksheet.Column(5).Width = 20;

                    // Guardar en memoria
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var bytes = stream.ToArray();
                        var nombre = $"HistorialSalarios_{empleado.PrimerApellido}_{empleado.Nombre}_{DateTime.Now:yyyyMMdd}.xlsx";
                        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombre);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descargar Excel de historial");
                TempData["Error"] = "Error al generar el Excel.";
                return RedirectToAction(nameof(Empleado), new { id = empleadoId });
            }
        }

        // MÉTODO AUXILIAR: Generar HTML del historial

        private string GenerarHTMLHistorial(Empleado empleado, List<HistorialSalario> historial)
        {
            var filasHistorial = historial.Select(h =>
            {
                var diferencia = h.SalarioNuevo - h.SalarioAnterior;
                var estilo = diferencia >= 0 ? "color:green;" : "color:red;";
                return $@"<tr>
                    <td style='padding:8px;border-bottom:1px solid #eee;'>{h.FechaCambio:dd/MM/yyyy HH:mm}</td>
                    <td style='padding:8px;border-bottom:1px solid #eee;text-align:right;'>₡{h.SalarioAnterior:N0}</td>
                    <td style='padding:8px;border-bottom:1px solid #eee;text-align:right;'>₡{h.SalarioNuevo:N0}</td>
                    <td style='padding:8px;border-bottom:1px solid #eee;text-align:right;{estilo}font-weight:bold;'>
                        {(diferencia >= 0 ? "+" : "")}₡{diferencia:N0}
                    </td>
                    <td style='padding:8px;border-bottom:1px solid #eee;'>{h.ModificadoPor ?? "—"}</td>
                </tr>";
            }).Aggregate("", (a, b) => a + b);

            return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <title>Historial de Salarios</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background: #1A1A2E; padding: 20px; color: white; }}
        .header h2 {{ color: #FF7A00; margin: 0; }}
        .info {{ padding: 20px; border: 1px solid #eee; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th {{ background: #FF7A00; color: white; padding: 8px; text-align: left; font-weight: bold; }}
        td {{ padding: 8px; border-bottom: 1px solid #eee; }}
        .footer {{ margin-top: 20px; font-size: 0.9rem; color: #666; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>Ferretería El Pana SRL</h2>
        <p>Historial de Salarios</p>
    </div>
    <div class='info'>
        <p><strong>Empleado:</strong> {empleado.PrimerApellido} {empleado.Nombre}</p>
        <p><strong>Cédula:</strong> {empleado.Cedula}</p>
        <p><strong>Salario Actual:</strong> ₡{empleado.SalarioBase:N0}</p>
    </div>
    <table>
        <thead>
            <tr>
                <th>Fecha</th>
                <th style='text-align:right;'>Salario Anterior</th>
                <th style='text-align:right;'>Salario Nuevo</th>
                <th style='text-align:right;'>Diferencia</th>
                <th>Modificado por</th>
            </tr>
        </thead>
        <tbody>
            {filasHistorial}
        </tbody>
    </table>
    <div class='footer'>
        <p>Documento generado automáticamente el {DateTime.Now:dd/MM/yyyy HH:mm}</p>
    </div>
</body>
</html>";
        }
    }
}