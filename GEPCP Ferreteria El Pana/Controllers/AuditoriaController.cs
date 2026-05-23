using ClosedXML.Excel;
using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("Jefatura")]
    public class AuditoriaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditoriaController> _logger;

        public AuditoriaController(
            ApplicationDbContext context,
            ILogger<AuditoriaController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            string? usuario,
            string? modulo,
            string? desde,
            string? hasta,
            int pagina = 1)
        {
            try
            {
                const int porPagina = 50;

                ViewBag.Usuario = usuario;
                ViewBag.Modulo = modulo;
                ViewBag.Desde = desde;
                ViewBag.Hasta = hasta;
                ViewBag.Pagina = pagina;

                if (string.IsNullOrWhiteSpace(usuario) &&
                    string.IsNullOrWhiteSpace(modulo) &&
                    string.IsNullOrWhiteSpace(desde) &&
                    string.IsNullOrWhiteSpace(hasta))
                {
                    ViewBag.Modulos = await ObtenerModulos();
                    ViewBag.TotalRegistros = 0;
                    ViewBag.TotalPaginas = 0;
                    return View(new List<RegistroAuditoria>());
                }

                var query = ConstruirConsulta(usuario, modulo, desde, hasta);

                var total = await query.CountAsync();
                var totalPaginas = (int)Math.Ceiling(total / (double)porPagina);

                pagina = Math.Max(1, Math.Min(pagina, Math.Max(1, totalPaginas)));

                var registros = await query
                    .OrderByDescending(r => r.FechaHora)
                    .Skip((pagina - 1) * porPagina)
                    .Take(porPagina)
                    .ToListAsync();

                ViewBag.Modulos = await ObtenerModulos();
                ViewBag.TotalRegistros = total;
                ViewBag.TotalPaginas = totalPaginas;

                return View(registros);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar auditoría");
                TempData["Error"] = "Error al cargar la auditoría.";
                return View(new List<RegistroAuditoria>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportarExcel(string? usuario, string? modulo, string? desde, string? hasta)
        {
            try
            {
                var registros = await ConstruirConsulta(usuario, modulo, desde, hasta)
                    .OrderByDescending(r => r.FechaHora)
                    .ToListAsync();

                if (!registros.Any())
                {
                    TempData["Warning"] = "No hay registros para exportar en Excel con los filtros seleccionados.";
                    return RedirectToAction(nameof(Index), new { usuario, modulo, desde, hasta });
                }

                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Auditoria");

                ws.Cell(1, 1).Value = "FERRETERÍA EL PANA SRL";
                ws.Range(1, 1, 1, 6).Merge().Style
                    .Font.SetBold(true).Font.SetFontSize(14)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell(2, 1).Value = "REPORTE DE AUDITORÍA DEL SISTEMA";
                ws.Range(2, 1, 2, 6).Merge().Style
                    .Font.SetBold(true).Font.SetFontSize(12)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell(3, 1).Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                ws.Range(3, 1, 3, 6).Merge().Style
                    .Font.SetItalic(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

                var headers = new[] { "Fecha/Hora", "Usuario", "Módulo", "Acción", "Detalle", "IP" };
                for (int c = 1; c <= headers.Length; c++)
                {
                    ws.Cell(5, c).Value = headers[c - 1];
                    ws.Cell(5, c).Style
                        .Font.SetBold(true)
                        .Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#FF7A00"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                var fila = 6;
                foreach (var r in registros)
                {
                    ws.Cell(fila, 1).Value = r.FechaHora;
                    ws.Cell(fila, 1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                    ws.Cell(fila, 2).Value = r.Usuario;
                    ws.Cell(fila, 3).Value = r.Modulo;
                    ws.Cell(fila, 4).Value = r.Accion;
                    ws.Cell(fila, 5).Value = r.Detalle ?? "—";
                    ws.Cell(fila, 6).Value = r.IpAddress ?? "—";

                    ws.Range(fila, 1, fila, 6).Style
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Hair);

                    if (fila % 2 == 0)
                    {
                        ws.Range(fila, 1, fila, 6)
                            .Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FFF3E0"));
                    }

                    fila++;
                }

                ws.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var archivo = $"Auditoria_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    archivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar auditoría a Excel");
                TempData["Error"] = "Error al exportar auditoría en Excel.";
                return RedirectToAction(nameof(Index), new { usuario, modulo, desde, hasta });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportarPDF(string? usuario, string? modulo, string? desde, string? hasta)
        {
            try
            {
                var registros = await ConstruirConsulta(usuario, modulo, desde, hasta)
                    .OrderByDescending(r => r.FechaHora)
                    .ToListAsync();

                if (!registros.Any())
                {
                    TempData["Warning"] = "No hay registros para exportar en PDF con los filtros seleccionados.";
                    return RedirectToAction(nameof(Index), new { usuario, modulo, desde, hasta });
                }

                var pdfBytes = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                        page.Header().Column(c =>
                        {
                            c.Item().Text("FERRETERÍA EL PANA SRL").Bold().FontSize(14).FontColor(Colors.Orange.Darken2);
                            c.Item().Text("REPORTE DE AUDITORÍA DEL SISTEMA").Bold().FontSize(10);
                            c.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });

                        page.Content().PaddingTop(10).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(110);
                                c.ConstantColumn(95);
                                c.ConstantColumn(90);
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                                c.ConstantColumn(90);
                            });

                            foreach (var h in new[] { "FECHA/HORA", "USUARIO", "MÓDULO", "ACCIÓN", "DETALLE", "IP" })
                                t.Cell().Background(Colors.Orange.Medium).Padding(4)
                                    .Text(h).Bold().FontSize(8).FontColor(Colors.White);

                            foreach (var r in registros)
                            {
                                t.Cell().Padding(3).Text(r.FechaHora.ToString("dd/MM/yyyy HH:mm:ss")).FontSize(7);
                                t.Cell().Padding(3).Text(r.Usuario).FontSize(7);
                                t.Cell().Padding(3).Text(r.Modulo).FontSize(7);
                                t.Cell().Padding(3).Text(r.Accion).FontSize(7);
                                t.Cell().Padding(3).Text(r.Detalle ?? "—").FontSize(7);
                                t.Cell().Padding(3).Text(r.IpAddress ?? "—").FontSize(7);
                            }
                        });

                        page.Footer().AlignRight().Text($"Total registros: {registros.Count}").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                }).GeneratePdf();

                var archivo = $"Auditoria_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", archivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar auditoría a PDF");
                TempData["Error"] = "Error al exportar auditoría en PDF.";
                return RedirectToAction(nameof(Index), new { usuario, modulo, desde, hasta });
            }
        }

        private IQueryable<RegistroAuditoria> ConstruirConsulta(string? usuario, string? modulo, string? desde, string? hasta)
        {
            var query = _context.RegistrosAuditoria
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(usuario))
                query = query.Where(r => r.Usuario.ToLower().Contains(usuario.Trim().ToLower()));

            if (!string.IsNullOrWhiteSpace(modulo))
                query = query.Where(r => r.Modulo == modulo);

            if (DateTime.TryParse(desde, out var fechaDesde))
                query = query.Where(r => r.FechaHora >= fechaDesde.Date);

            if (DateTime.TryParse(hasta, out var fechaHasta))
                query = query.Where(r => r.FechaHora < fechaHasta.Date.AddDays(1));

            return query;
        }

        private async Task<List<string>> ObtenerModulos()
        {
            return await _context.RegistrosAuditoria
                .AsNoTracking()
                .Select(r => r.Modulo)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
        }
    }
}
