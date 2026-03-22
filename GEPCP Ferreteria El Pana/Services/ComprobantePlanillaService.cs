using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using GEPCP_Ferreteria_El_Pana.Models;

namespace GEPCP_Ferreteria_El_Pana.Services
{
    public class ComprobantePlanillaService
    {
        private readonly ILogger<ComprobantePlanillaService> _logger;
        private readonly IWebHostEnvironment _env;

        public ComprobantePlanillaService(
            ILogger<ComprobantePlanillaService> logger,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        // ── HELPERS PRIVADOS ──────────────────────────────────────────────────

        private string LogoPath =>
            Path.Combine(_env.WebRootPath, "images", "logo-el-pana.jpg");

        private byte[]? ObtenerLogoBytes()
        {
            try
            {
                return File.Exists(LogoPath) ? File.ReadAllBytes(LogoPath) : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo leer el logo desde {Path}", LogoPath);
                return null;
            }
        }

        // ── GENERAR PDF ───────────────────────────────────────────────────────

        public byte[] GenerarPDF(PlanillaEmpleado planilla)
        {
            var naranja = Color.FromHex("FF7A00");
            var grisClaro = Color.FromHex("CCCCCC");
            var grisTexto = Color.FromHex("999999");
            var fondoNaranja = Color.FromHex("FFF3E0");
            var fondoRojo = Color.FromHex("FFEBEE");
            var fondoGris = Color.FromHex("F5F5F5");
            var logoBytes = ObtenerLogoBytes();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // ── Encabezado con logo ───────────────────────────────
                        col.Item().Row(row =>
                        {
                            if (logoBytes != null)
                                row.ConstantItem(80).Padding(4).Image(logoBytes).FitArea();
                            else
                                row.ConstantItem(80).Text("");

                            row.RelativeItem().AlignMiddle().Column(c =>
                            {
                                c.Item().AlignCenter().Text("FERRETERÍA EL PANA")
                                    .Bold().FontSize(16).FontColor(naranja);
                                c.Item().AlignCenter().Text("DEPARTAMENTO DE RECURSOS HUMANOS")
                                    .Bold().FontSize(12);
                                c.Item().AlignCenter().Text("COMPROBANTE DE PAGO")
                                    .Bold().FontSize(12);
                            });

                            row.ConstantItem(80).Text("");
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

                        // ── Datos del empleado ────────────────────────────────
                        // ── Datos del empleado ────────────────────────────────
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(90);  // Label izquierda
                                c.RelativeColumn(3);   // Valor izquierda
                                c.ConstantColumn(90);  // Label derecha
                                c.RelativeColumn(2);   // Valor derecha
                            });

                            // Fila 1: Nombre + Cédula
                            table.Cell().Padding(3).Text("NOMBRE:").Bold();
                            table.Cell().Padding(3).Text(
                                $"{planilla.Empleado.Nombre} " +
                                $"{planilla.Empleado.PrimerApellido} " +
                                $"{planilla.Empleado.SegundoApellido}");
                            table.Cell().Padding(3).Text("CÉDULA:").Bold();
                            table.Cell().Padding(3).Text(planilla.Empleado.Cedula);

                            // Fila 2: Departamento + Puesto
                            table.Cell().Padding(3).Text("DEPARTAMENTO").Bold();
                            table.Cell().Padding(3).Text(planilla.Empleado.Departamento);
                            table.Cell().Padding(3).Text("PUESTO:").Bold();
                            table.Cell().Padding(3).Text(planilla.Empleado.Puesto);

                            // Fila 3: Período (ocupa todo el ancho)
                            table.Cell().Padding(3).Text("PERÍODO DE PAGO:").Bold();
                            table.Cell().ColumnSpan(3).Padding(3).Text(
                                $"{planilla.PeriodoPago.FechaInicio:dd/MM/yyyy} " +
                                $"AL {planilla.PeriodoPago.FechaFin:dd/MM/yyyy}");
                        });
                        // ── TOTAL DEVENGADO ───────────────────────────────────
                        col.Item().Text("TOTAL DEVENGADO:").Bold().Underline();
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(4);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Background(fondoGris).Padding(3).Text("CONCEPTO").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignRight().Text("VALOR HORA").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignRight().Text("HORAS").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignRight().Text("MONTO TOTAL").Bold();

                            table.Cell().Padding(3).Text("SALARIO BASE:");
                            table.Cell().Padding(3).AlignRight().Text("");
                            table.Cell().Padding(3).AlignRight().Text("");
                            table.Cell().Padding(3).AlignRight().Text(
                                $"₡{planilla.Empleado.SalarioBase:N2}");

                            table.Cell().Padding(3).Text("HORAS JORNADA ORDINARIA DIURNA");
                            table.Cell().Padding(3).AlignRight().Text($"₡{planilla.ValorHora:N2}");
                            table.Cell().Padding(3).AlignRight().Text($"{planilla.HorasOrdinarias:N2}");
                            table.Cell().Padding(3).AlignRight().Text($"₡{planilla.SalarioOrdinario:N2}");

                            table.Cell().Padding(3).Text("HORAS JORNADA EXTRAORDINARIA");
                            table.Cell().Padding(3).AlignRight().Text(
                                planilla.HorasExtras > 0 ? $"₡{planilla.ValorHoraExtra:N2}" : "");
                            table.Cell().Padding(3).AlignRight().Text(
                                planilla.HorasExtras > 0 ? $"{planilla.HorasExtras:N2}" : "0.00");
                            table.Cell().Padding(3).AlignRight().Text(
                                $"₡{planilla.MontoHorasExtras:N2}");

                            if (planilla.AumentoAplicado > 0)
                            {
                                table.Cell().Padding(3).Text("COMISIÓN / AUMENTO");
                                table.Cell().Padding(3).AlignRight().Text("");
                                table.Cell().Padding(3).AlignRight().Text("");
                                table.Cell().Padding(3).AlignRight().Text(
                                    $"₡{planilla.AumentoAplicado:N2}");
                            }

                            if (planilla.MontoFeriados > 0)
                            {
                                table.Cell().Padding(3).Text("FERIADOS");
                                table.Cell().Padding(3).AlignRight().Text("");
                                table.Cell().Padding(3).AlignRight().Text("");
                                table.Cell().Padding(3).AlignRight().Text(
                                    $"₡{planilla.MontoFeriados:N2}");
                            }

                            table.Cell().Background(fondoNaranja).Padding(3)
                                .Text("TOTAL (BRUTO)").Bold();
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight().Text("");
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight().Text("");
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight()
                                .Text($"₡{planilla.TotalDevengado:N2}").Bold();
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── DEDUCCIONES ───────────────────────────────────────
                        col.Item().Text("DEDUCCIONES:").Bold().Underline();
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(6);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Padding(3).Text("SEGURO SOCIAL");
                            table.Cell().Padding(3).AlignCenter().Text(
                                $"{planilla.PorcentajeCCSS:N2}%");
                            table.Cell().Padding(3).AlignRight().Text(
                                $"₡{planilla.DeduccionCCSS:N2}");

                            if (planilla.DeduccionCreditoFerreteria > 0)
                            {
                                table.Cell().Padding(3).Text("FACTURAS DE CRÉDITO FERRETERÍA");
                                table.Cell().Padding(3).AlignCenter().Text("");
                                table.Cell().Padding(3).AlignRight().Text(
                                    $"₡{planilla.DeduccionCreditoFerreteria:N2}");
                            }

                            if (planilla.DeduccionPrestamos > 0)
                            {
                                table.Cell().Padding(3).Text("PRÉSTAMO PERSONAL");
                                table.Cell().Padding(3).AlignCenter().Text("");
                                table.Cell().Padding(3).AlignRight().Text(
                                    $"₡{planilla.DeduccionPrestamos:N2}");
                            }

                            if (planilla.DeduccionHorasNoLaboradas > 0)
                            {
                                table.Cell().Padding(3).Text("HORAS NO LABORADAS");
                                table.Cell().Padding(3).AlignCenter().Text("");
                                table.Cell().Padding(3).AlignRight().Text(
                                    $"₡{planilla.DeduccionHorasNoLaboradas:N2}");
                            }

                            if (planilla.DeduccionIncapacidad > 0)
                            {
                                table.Cell().Padding(3).Text("INCAPACIDAD");
                                table.Cell().Padding(3).AlignCenter().Text("");
                                table.Cell().Padding(3).AlignRight().Text(
                                    $"₡{planilla.DeduccionIncapacidad:N2}");
                            }

                            if (planilla.OtrasDeducciones > 0)
                            {
                                table.Cell().Padding(3).Text("OTRAS DEDUCCIONES");
                                table.Cell().Padding(3).AlignCenter().Text("");
                                table.Cell().Padding(3).AlignRight().Text(
                                    $"₡{planilla.OtrasDeducciones:N2}");
                            }

                            table.Cell().Background(fondoRojo).Padding(3)
                                .Text("TOTAL DEDUCCIONES").Bold();
                            table.Cell().Background(fondoRojo).Padding(3).AlignCenter().Text("");
                            table.Cell().Background(fondoRojo).Padding(3).AlignRight()
                                .Text($"₡{planilla.TotalDeducciones:N2}").Bold();
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

                        // ── NETO A PAGAR ──────────────────────────────────────
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(6);
                                c.RelativeColumn(4);
                            });

                            table.Cell().Padding(5).Text("NETO A PAGAR").Bold().FontSize(14);
                            table.Cell().Padding(5).AlignRight()
                                .Text($"₡{planilla.NetoAPagar:N2}")
                                .Bold().FontSize(14).FontColor(naranja);
                        });

                        col.Item().PaddingVertical(16).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Firma ─────────────────────────────────────────────
                        col.Item().PaddingTop(30).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(5);
                                c.RelativeColumn(2);
                            });

                            // Columna izquierda: firma empleado
                            table.Cell().Padding(3).Column(c =>
                            {
                                c.Item().Text("RECIBO CONFORME:").Bold().FontSize(10);
                                c.Item().PaddingTop(40).BorderBottom(1).BorderColor(Color.FromHex("333333")).Text("");
                                c.Item().PaddingTop(5).AlignCenter().Text(
                                    $"{planilla.Empleado.PrimerApellido} " +
                                    $"{planilla.Empleado.SegundoApellido} " +
                                    $"{planilla.Empleado.Nombre}")
                                    .FontSize(9).Italic();
                                c.Item().AlignCenter().Text(planilla.Empleado.Cedula)
                                    .FontSize(8).FontColor(Color.FromHex("666666"));
                            });

                            // Columna derecha: fecha
                            table.Cell().Padding(3).AlignRight().AlignBottom().Column(c =>
                            {
                                c.Item().Text("Fecha de pago:").Bold().FontSize(9);
                                c.Item().PaddingTop(4).Text($"{DateTime.Today:dd/MM/yyyy}").FontSize(10);
                            });
                        });

                        col.Item().PaddingTop(12).AlignCenter()
                            .Text("Ferreteria El Pana SRL")
                            .FontSize(8).FontColor(grisTexto).Italic();
                    });
                });
            });

            return doc.GeneratePdf();
        }
        // ── GENERAR PDF HORAS EXTRAS ──────────────────────────────────────────

        public byte[] GenerarPDFHorasExtras(HorasExtras horasExtras)
        {
            var naranja = Color.FromHex("FF7A00");
            var grisClaro = Color.FromHex("CCCCCC");
            var grisTexto = Color.FromHex("999999");
            var fondoGris = Color.FromHex("F5F5F5");
            var fondoNaranja = Color.FromHex("FFF3E0");
            var logoBytes = ObtenerLogoBytes();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // ── Encabezado con logo ───────────────────────────────
                        col.Item().Row(row =>
                        {
                            if (logoBytes != null)
                                row.ConstantItem(80).Padding(4).Image(logoBytes).FitArea();
                            else
                                row.ConstantItem(80).Text("");

                            row.RelativeItem().AlignMiddle().Column(c =>
                            {
                                c.Item().AlignCenter().Text("FERRETERÍA EL PANA")
                                    .Bold().FontSize(16).FontColor(naranja);
                                c.Item().AlignCenter().Text("DEPARTAMENTO RECURSOS HUMANOS")
                                    .Bold().FontSize(12);
                                c.Item().AlignCenter().Text("COMPROBANTE PAGO HORAS EXTRAS")
                                    .Bold().FontSize(12);
                            });

                            row.ConstantItem(80).Text("");
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

                        // ── Datos del empleado ────────────────────────────────
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(90);
                                c.RelativeColumn(3);
                                c.ConstantColumn(90);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Padding(3).Text("NOMBRE:").Bold();
                            table.Cell().Padding(3).Text(
                                $"{horasExtras.Empleado.PrimerApellido} " +
                                $"{horasExtras.Empleado.SegundoApellido} " +
                                $"{horasExtras.Empleado.Nombre}");
                            table.Cell().Padding(3).Text("CÉDULA:").Bold();
                            table.Cell().Padding(3).Text(horasExtras.Empleado.Cedula);

                            table.Cell().Padding(3).Text("DEPARTAMENTO:").Bold();
                            table.Cell().Padding(3).Text(horasExtras.Empleado.Departamento);
                            table.Cell().Padding(3).Text("PUESTO:").Bold();
                            table.Cell().Padding(3).Text(horasExtras.Empleado.Puesto);

                            table.Cell().Padding(3).Text("PERÍODO:").Bold();
                            table.Cell().Padding(3).Text(horasExtras.PeriodoPago.Descripcion);
                            table.Cell().Padding(3).Text("TOTAL HORAS:").Bold();
                            table.Cell().Padding(3).Text($"{horasExtras.TotalHoras:N2} hrs");
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Detalle ───────────────────────────────────────────
                        col.Item().PaddingBottom(4).Text("DETALLE:").Bold().Underline();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(4);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            // Header
                            table.Cell().Background(fondoGris).Padding(3).Text("CONCEPTO").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignCenter().Text("% APLICADO").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignRight().Text("VALOR HORA").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignRight().Text("TOTAL").Bold();

                            // Fila de datos
                            table.Cell().Padding(3).Text("PAGO HRS EXTRA");
                            table.Cell().Padding(3).AlignCenter().Text($"{horasExtras.Porcentaje:N1}");
                            table.Cell().Padding(3).AlignRight().Text($"₡{horasExtras.ValorHora:N2}");
                            table.Cell().Padding(3).AlignRight().Text($"₡{horasExtras.MontoTotal:N2}");

                            // Total
                            table.Cell().Background(fondoNaranja).Padding(3).Text("TOTAL").Bold();
                            table.Cell().Background(fondoNaranja).Padding(3).AlignCenter().Text("");
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight().Text("");
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight()
                                .Text($"₡{horasExtras.MontoTotal:N2}").Bold();
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

                        // ── Monto destacado ───────────────────────────────────
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(6);
                                c.RelativeColumn(4);
                            });

                            table.Cell().Padding(5).Text("MONTO A PAGAR").Bold().FontSize(14);
                            table.Cell().Padding(5).AlignRight()
                                .Text($"₡{horasExtras.MontoTotal:N2}")
                                .Bold().FontSize(14).FontColor(naranja);
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Firma ─────────────────────────────────────────────
                        col.Item().PaddingTop(30).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(5);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Padding(3).Column(c =>
                            {
                                c.Item().Text("RECIBO CONFORME:").Bold().FontSize(10);
                                c.Item().PaddingTop(40).BorderBottom(1)
                                    .BorderColor(Color.FromHex("333333")).Text("");
                                c.Item().PaddingTop(5).AlignCenter().Text(
                                    $"{horasExtras.Empleado.PrimerApellido} " +
                                    $"{horasExtras.Empleado.SegundoApellido} " +
                                    $"{horasExtras.Empleado.Nombre}")
                                    .FontSize(9).Italic();
                                c.Item().AlignCenter().Text(horasExtras.Empleado.Cedula)
                                    .FontSize(8).FontColor(Color.FromHex("666666"));
                            });

                            table.Cell().Padding(3).AlignRight().AlignBottom().Column(c =>
                            {
                                c.Item().Text("Fecha de pago:").Bold().FontSize(9);
                                c.Item().PaddingTop(4).Text($"{DateTime.Today:dd/MM/yyyy}").FontSize(10);
                            });
                        });

                        col.Item().PaddingTop(12).AlignCenter()
                            .Text("Este comprobante es generado automáticamente por el sistema GEPCP")
                            .FontSize(8).FontColor(grisTexto).Italic();
                    });
                });
            });

            return doc.GeneratePdf();
        }
        // ── GENERAR PDF INCAPACIDAD ───────────────────────────────────────────

        public byte[] GenerarPDFIncapacidad(Incapacidad incapacidad)
        {
            var naranja = Color.FromHex("FF7A00");
            var grisClaro = Color.FromHex("CCCCCC");
            var grisTexto = Color.FromHex("999999");
            var fondoGris = Color.FromHex("F5F5F5");
            var fondoNaranja = Color.FromHex("FFF3E0");
            var logoBytes = ObtenerLogoBytes();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // ── Encabezado con logo ───────────────────────────────
                        col.Item().Row(row =>
                        {
                            if (logoBytes != null)
                                row.ConstantItem(80).Padding(4).Image(logoBytes).FitArea();
                            else
                                row.ConstantItem(80).Text("");

                            row.RelativeItem().AlignMiddle().Column(c =>
                            {
                                c.Item().AlignCenter().Text("FERRETERÍA EL PANA")
                                    .Bold().FontSize(16).FontColor(naranja);
                                c.Item().AlignCenter().Text("DEPARTAMENTO DE RECURSOS HUMANOS")
                                    .Bold().FontSize(12);
                                c.Item().AlignCenter().Text("COMPROBANTE DE PAGO DE INCAPACIDAD")
                                    .Bold().FontSize(12);
                            });

                            row.ConstantItem(80).Text("");
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

                        // ── Datos del empleado ────────────────────────────────
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(90);
                                c.RelativeColumn(3);
                                c.ConstantColumn(90);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Padding(3).Text("NOMBRE:").Bold();
                            table.Cell().Padding(3).Text(
                                $"{incapacidad.Empleado.PrimerApellido} " +
                                $"{incapacidad.Empleado.SegundoApellido} " +
                                $"{incapacidad.Empleado.Nombre}");
                            table.Cell().Padding(3).Text("CÉDULA:").Bold();
                            table.Cell().Padding(3).Text(incapacidad.Empleado.Cedula);

                            table.Cell().Padding(3).Text("DEPARTAMENTO:").Bold();
                            table.Cell().Padding(3).Text(incapacidad.Empleado.Departamento);
                            table.Cell().Padding(3).Text("PUESTO:").Bold();
                            table.Cell().Padding(3).Text(incapacidad.Empleado.Puesto);

                            table.Cell().Padding(3).Text("PERÍODO:").Bold();
                            table.Cell().Padding(3).Text(
                                $"{incapacidad.FechaInicio:dd/MM/yyyy} AL {incapacidad.FechaFin:dd/MM/yyyy}");
                            table.Cell().Padding(3).Text("TOTAL DÍAS:").Bold();
                            table.Cell().Padding(3).Text($"{incapacidad.TotalDias} días");

                            // Tiquete solo si es CCSS
                            if (!string.IsNullOrEmpty(incapacidad.TiqueteCCSS))
                            {
                                table.Cell().Padding(3).Text("TIQUETE CCSS:").Bold();
                                table.Cell().ColumnSpan(3).Padding(3).Text(incapacidad.TiqueteCCSS);
                            }
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Detalle ───────────────────────────────────────────
                        col.Item().PaddingBottom(4).Text("DETALLE:").Bold().Underline();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(4);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            // Header
                            table.Cell().Background(fondoGris).Padding(3).Text("CONCEPTO").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignCenter().Text("% APLICADO").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignRight().Text("MONTO POR DÍA").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignRight().Text("TOTAL").Bold();

                            // Concepto según responsable
                            var concepto = incapacidad.ResponsablePago switch
                            {
                                ResponsablePago.Patrono => "Pago Patrono",
                                ResponsablePago.CCSS => "Pago CCSS",
                                ResponsablePago.INS => "Pago INS",
                                _ => "Pago Incapacidad"
                            };

                            table.Cell().Padding(3).Text(concepto);
                            table.Cell().Padding(3).AlignCenter().Text(
                                $"{incapacidad.PorcentajePago:N0}%");
                            table.Cell().Padding(3).AlignRight().Text(
                                $"₡{incapacidad.MontoPorDia:N2}");
                            table.Cell().Padding(3).AlignRight().Text(
                                $"₡{incapacidad.MontoTotal:N2}");

                            // Entidad emisora
                            table.Cell().Padding(3).Text($"Entidad: {incapacidad.Entidad}");
                            table.Cell().Padding(3).AlignCenter().Text("");
                            table.Cell().Padding(3).AlignRight().Text("");
                            table.Cell().Padding(3).AlignRight().Text("");

                            // Total
                            table.Cell().Background(fondoNaranja).Padding(3).Text("TOTAL").Bold();
                            table.Cell().Background(fondoNaranja).Padding(3).AlignCenter().Text("");
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight().Text("");
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight()
                                .Text($"₡{incapacidad.MontoTotal:N2}").Bold();
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

                        // ── Monto destacado ───────────────────────────────────
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(6);
                                c.RelativeColumn(4);
                            });

                            table.Cell().Padding(5).Text("MONTO TOTAL INCAPACIDAD")
                                .Bold().FontSize(14);
                            table.Cell().Padding(5).AlignRight()
                                .Text($"₡{incapacidad.MontoTotal:N2}")
                                .Bold().FontSize(14).FontColor(naranja);
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Observaciones si las hay ──────────────────────────
                        if (!string.IsNullOrEmpty(incapacidad.Observaciones))
                        {
                            col.Item().PaddingBottom(8).Table(table =>
                            {
                                table.ColumnsDefinition(c => c.RelativeColumn());
                                table.Cell().Padding(3).Column(c =>
                                {
                                    c.Item().Text("OBSERVACIONES:").Bold().FontSize(9);
                                    c.Item().PaddingTop(2).Text(incapacidad.Observaciones)
                                        .FontSize(9).FontColor(Color.FromHex("444444")).Italic();
                                });
                            });
                        }

                        // ── Firma ─────────────────────────────────────────────
                        col.Item().PaddingTop(30).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(5);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Padding(3).Column(c =>
                            {
                                c.Item().Text("RECIBO CONFORME:").Bold().FontSize(10);
                                c.Item().PaddingTop(40).BorderBottom(1)
                                    .BorderColor(Color.FromHex("333333")).Text("");
                                c.Item().PaddingTop(5).AlignCenter().Text(
                                    $"{incapacidad.Empleado.PrimerApellido} " +
                                    $"{incapacidad.Empleado.SegundoApellido} " +
                                    $"{incapacidad.Empleado.Nombre}")
                                    .FontSize(9).Italic();
                                c.Item().AlignCenter().Text(incapacidad.Empleado.Cedula)
                                    .FontSize(8).FontColor(Color.FromHex("666666"));
                            });

                            table.Cell().Padding(3).AlignRight().AlignBottom().Column(c =>
                            {
                                c.Item().Text("Fecha de pago:").Bold().FontSize(9);
                                c.Item().PaddingTop(4).Text($"{DateTime.Today:dd/MM/yyyy}").FontSize(10);
                            });
                        });

                        col.Item().PaddingTop(12).AlignCenter()
                            .Text("Este comprobante es generado automáticamente por el sistema GEPCP")
                            .FontSize(8).FontColor(grisTexto).Italic();
                    });
                });
            });

            return doc.GeneratePdf();
        }

        // ── GENERAR PDF AGUINALDO ─────────────────────────────────────────────

        public byte[] GenerarPDFAguinaldo(Aguinaldo aguinaldo)
        {
            var naranja = Color.FromHex("FF7A00");
            var grisClaro = Color.FromHex("CCCCCC");
            var grisTexto = Color.FromHex("999999");
            var fondoGris = Color.FromHex("F5F5F5");
            var fondoNaranja = Color.FromHex("FFF3E0");
            var logoBytes = ObtenerLogoBytes();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // ── Encabezado con logo ───────────────────────────────
                        col.Item().Row(row =>
                        {
                            if (logoBytes != null)
                                row.ConstantItem(80).Padding(4).Image(logoBytes).FitArea();
                            else
                                row.ConstantItem(80).Text("");

                            row.RelativeItem().AlignMiddle().Column(c =>
                            {
                                c.Item().AlignCenter().Text("FERRETERÍA EL PANA")
                                    .Bold().FontSize(16).FontColor(naranja);
                                c.Item().AlignCenter().Text("DEPARTAMENTO DE RECURSOS HUMANOS")
                                    .Bold().FontSize(12);
                                c.Item().AlignCenter().Text("COMPROBANTE DE PAGO DE AGUINALDO")
                                    .Bold().FontSize(12);
                            });

                            row.ConstantItem(80).Text("");
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

                        // ── Datos del empleado ────────────────────────────────
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(90);
                                c.RelativeColumn(3);
                                c.ConstantColumn(90);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Padding(3).Text("NOMBRE:").Bold();
                            table.Cell().Padding(3).Text(
                                $"{aguinaldo.Empleado.PrimerApellido} " +
                                $"{aguinaldo.Empleado.SegundoApellido} " +
                                $"{aguinaldo.Empleado.Nombre}");
                            table.Cell().Padding(3).Text("CÉDULA:").Bold();
                            table.Cell().Padding(3).Text(aguinaldo.Empleado.Cedula);

                            table.Cell().Padding(3).Text("DEPARTAMENTO:").Bold();
                            table.Cell().Padding(3).Text(aguinaldo.Empleado.Departamento);
                            table.Cell().Padding(3).Text("PUESTO:").Bold();
                            table.Cell().Padding(3).Text(aguinaldo.Empleado.Puesto);

                            table.Cell().Padding(3).Text("PERÍODO:").Bold();
                            table.Cell().Padding(3).Text(
                                $"{aguinaldo.FechaInicio:dd/MM/yyyy} AL {aguinaldo.FechaFin:dd/MM/yyyy}");
                            table.Cell().Padding(3).Text("AÑO:").Bold();
                            table.Cell().Padding(3).Text(aguinaldo.Anio.ToString());

                            table.Cell().Padding(3).Text("FECHA A CANCELAR:").Bold();
                            table.Cell().ColumnSpan(3).Padding(3)
                                .Text(aguinaldo.FechaPago.ToString("dd/MM/yyyy"));
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Detalle ───────────────────────────────────────────
                        col.Item().PaddingBottom(4).Text("DETALLE:").Bold().Underline();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(6);
                                c.RelativeColumn(4);
                            });

                            // Header
                            table.Cell().Background(fondoGris).Padding(3).Text("CONCEPTO").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignRight().Text("MONTO TOTAL").Bold();

                            // Fila
                            table.Cell().Padding(3).Text($"AGUINALDO {aguinaldo.Anio}");
                            table.Cell().Padding(3).AlignRight().Text($"₡{aguinaldo.MontoTotal:N2}");

                            // Total
                            table.Cell().Background(fondoNaranja).Padding(3).Text("TOTAL").Bold();
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight()
                                .Text($"₡{aguinaldo.MontoTotal:N2}").Bold();
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

                        // ── Monto destacado ───────────────────────────────────
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(6);
                                c.RelativeColumn(4);
                            });

                            table.Cell().Padding(5).Text("MONTO A PAGAR").Bold().FontSize(14);
                            table.Cell().Padding(5).AlignRight()
                                .Text($"₡{aguinaldo.MontoTotal:N2}")
                                .Bold().FontSize(14).FontColor(naranja);
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Observaciones ─────────────────────────────────────
                        if (!string.IsNullOrEmpty(aguinaldo.Observaciones))
                        {
                            col.Item().PaddingBottom(8).Column(c =>
                            {
                                c.Item().Text("OBSERVACIONES:").Bold().FontSize(9);
                                c.Item().PaddingTop(2).Text(aguinaldo.Observaciones)
                                    .FontSize(9).FontColor(Color.FromHex("444444")).Italic();
                            });
                        }

                        // ── Firma ─────────────────────────────────────────────
                        col.Item().PaddingTop(30).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(5);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Padding(3).Column(c =>
                            {
                                c.Item().Text("RECIBO CONFORME:").Bold().FontSize(10);
                                c.Item().PaddingTop(40).BorderBottom(1)
                                    .BorderColor(Color.FromHex("333333")).Text("");
                                c.Item().PaddingTop(5).AlignCenter().Text(
                                    $"{aguinaldo.Empleado.PrimerApellido} " +
                                    $"{aguinaldo.Empleado.SegundoApellido} " +
                                    $"{aguinaldo.Empleado.Nombre}")
                                    .FontSize(9).Italic();
                                c.Item().AlignCenter().Text(aguinaldo.Empleado.Cedula)
                                    .FontSize(8).FontColor(Color.FromHex("666666"));
                            });

                            table.Cell().Padding(3).AlignRight().AlignBottom().Column(c =>
                            {
                                c.Item().Text("Fecha de pago:").Bold().FontSize(9);
                                c.Item().PaddingTop(4)
                                    .Text(aguinaldo.FechaPago.ToString("dd/MM/yyyy")).FontSize(10);
                            });
                        });

                        col.Item().PaddingTop(12).AlignCenter()
                            .Text("Este comprobante es generado automáticamente por el sistema GEPCP")
                            .FontSize(8).FontColor(grisTexto).Italic();
                    });
                });
            });

            return doc.GeneratePdf();
        }
        // ── GENERAR PDF PLANILLA GENERAL ──────────────────────────────────────

        public byte[] GenerarPDFPlanillaGeneral(
            List<PlanillaEmpleado> planillas,
            PeriodoPago periodo)
        {
            var naranja = Color.FromHex("FF7A00");
            var grisClaro = Color.FromHex("CCCCCC");
            var grisTexto = Color.FromHex("999999");
            var fondoGris = Color.FromHex("F5F5F5");
            var fondoNaranja = Color.FromHex("FFF3E0");
            var fondoRojo = Color.FromHex("FFEBEE");
            var logoBytes = ObtenerLogoBytes();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // ── Encabezado ────────────────────────────────────────
                        col.Item().Row(row =>
                        {
                            if (logoBytes != null)
                                row.ConstantItem(60).Padding(2).Image(logoBytes).FitArea();
                            else
                                row.ConstantItem(60).Text("");

                            row.RelativeItem().AlignMiddle().Column(c =>
                            {
                                c.Item().AlignCenter().Text("FERRETERÍA EL PANA SRL")
                                    .Bold().FontSize(14).FontColor(naranja);
                                c.Item().AlignCenter().Text("DEPARTAMENTO DE RECURSOS HUMANOS")
                                    .Bold().FontSize(10);
                                c.Item().AlignCenter().Text("CONTROL PLANILLA QUINCENAL")
                                    .Bold().FontSize(10);
                                c.Item().AlignCenter()
                                    .Text($"PERÍODO: {periodo.FechaInicio:dd/MM/yyyy} AL {periodo.FechaFin:dd/MM/yyyy}")
                                    .FontSize(9).Italic();
                            });

                            row.ConstantItem(60).Text("");
                        });

                        col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(naranja);

                        // ── Tabla por departamento ────────────────────────────
                        var grupos = planillas.GroupBy(p => p.Empleado.Departamento);

                        foreach (var grupo in grupos)
                        {
                            // Header de departamento
                            col.Item().PaddingTop(6).Background(Color.FromHex("333333"))
                                .Padding(4).Text(grupo.Key.ToUpper())
                                .Bold().FontSize(9).FontColor(Color.FromHex("FFFFFF"));

                            // Encabezados de columna
                            col.Item().Table(table =>
                            {
                                DefinirColumnasPlanilla(table);

                                // Header row
                                var headerStyle = fondoNaranja;
                                AgregarHeaderPlanilla(table, naranja);

                                // Filas de empleados
                                foreach (var p in grupo)
                                    AgregarFilaPlanilla(table, p, fondoGris);

                                // Subtotal del departamento
                                table.Cell().Background(Color.FromHex("FFE0B2")).Padding(3)
                                    .Text($"Subtotal {grupo.Key}").Bold();
                                table.Cell().Background(Color.FromHex("FFE0B2")).Padding(3).AlignRight()
                                    .Text($"₡{grupo.Sum(p => p.SalarioOrdinario):N0}").Bold();
                                table.Cell().Background(Color.FromHex("FFE0B2")).Padding(3).AlignRight()
                                    .Text($"₡{grupo.Sum(p => p.MontoHorasExtras):N0}").Bold();
                                table.Cell().Background(Color.FromHex("FFE0B2")).Padding(3).AlignRight()
                                    .Text($"₡{grupo.Sum(p => p.AumentoAplicado):N0}").Bold();
                                table.Cell().Background(Color.FromHex("FFE0B2")).Padding(3).AlignRight()
                                    .Text($"₡{grupo.Sum(p => p.TotalDevengado):N0}").Bold();
                                table.Cell().Background(Color.FromHex("FFE0B2")).Padding(3).AlignRight()
                                    .Text($"₡{grupo.Sum(p => p.DeduccionCCSS):N0}").Bold();
                                table.Cell().Background(Color.FromHex("FFE0B2")).Padding(3).AlignRight()
                                    .Text($"₡{grupo.Sum(p => p.DeduccionPrestamos):N0}").Bold();
                                table.Cell().Background(Color.FromHex("FFE0B2")).Padding(3).AlignRight()
                                    .Text($"₡{grupo.Sum(p => p.DeduccionCreditoFerreteria):N0}").Bold();
                                table.Cell().Background(Color.FromHex("FFE0B2")).Padding(3).AlignRight()
                                    .Text($"₡{grupo.Sum(p => p.TotalDeducciones):N0}").Bold();
                                table.Cell().Background(Color.FromHex("FFE0B2")).Padding(3).AlignRight()
                                    .Text($"₡{grupo.Sum(p => p.NetoAPagar):N0}").Bold();
                            });
                        }

                        col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(naranja);

                        // ── Total general ─────────────────────────────────────
                        col.Item().Table(table =>
                        {
                            DefinirColumnasPlanilla(table);

                            table.Cell().Background(naranja).Padding(3)
                                .Text("TOTAL GENERAL").Bold().FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(naranja).Padding(3).AlignRight()
                                .Text($"₡{planillas.Sum(p => p.SalarioOrdinario):N0}")
                                .Bold().FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(naranja).Padding(3).AlignRight()
                                .Text($"₡{planillas.Sum(p => p.MontoHorasExtras):N0}")
                                .Bold().FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(naranja).Padding(3).AlignRight()
                                .Text($"₡{planillas.Sum(p => p.AumentoAplicado):N0}")
                                .Bold().FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(naranja).Padding(3).AlignRight()
                                .Text($"₡{planillas.Sum(p => p.TotalDevengado):N0}")
                                .Bold().FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(naranja).Padding(3).AlignRight()
                                .Text($"₡{planillas.Sum(p => p.DeduccionCCSS):N0}")
                                .Bold().FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(naranja).Padding(3).AlignRight()
                                .Text($"₡{planillas.Sum(p => p.DeduccionPrestamos):N0}")
                                .Bold().FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(naranja).Padding(3).AlignRight()
                                .Text($"₡{planillas.Sum(p => p.DeduccionCreditoFerreteria):N0}")
                                .Bold().FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(naranja).Padding(3).AlignRight()
                                .Text($"₡{planillas.Sum(p => p.TotalDeducciones):N0}")
                                .Bold().FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(naranja).Padding(3).AlignRight()
                                .Text($"₡{planillas.Sum(p => p.NetoAPagar):N0}")
                                .Bold().FontColor(Color.FromHex("FFFFFF"));
                        });

                        col.Item().PaddingTop(10).AlignCenter()
                            .Text($"Generado el {DateTime.Today:dd/MM/yyyy} — Sistema GEPCP Ferretería El Pana")
                            .FontSize(7).FontColor(grisTexto).Italic();
                    });
                });
            });

            return doc.GeneratePdf();
        }

        // ── HELPERS PRIVADOS PARA TABLA PLANILLA GENERAL ──────────────────────

        private static void DefinirColumnasPlanilla(TableDescriptor table)
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(3);  // Nombre
                c.RelativeColumn(2);  // Sal. Ord.
                c.RelativeColumn(2);  // Hrs. Ext.
                c.RelativeColumn(2);  // Comisión
                c.RelativeColumn(2);  // Total Dev.
                c.RelativeColumn(2);  // CCSS
                c.RelativeColumn(2);  // Préstamo
                c.RelativeColumn(2);  // Cré. Ferre.
                c.RelativeColumn(2);  // Total Ded.
                c.RelativeColumn(2);  // Neto
            });
        }

        private static void AgregarHeaderPlanilla(TableDescriptor table, Color naranja)
        {
            var headers = new[]
            {
        "Empleado", "Sal. Ord.", "Hrs. Ext.", "Comisión",
        "Total Dev.", "CCSS", "Préstamo", "Cré. Ferre.",
        "Total Ded.", "Neto"
    };
            foreach (var h in headers)
                table.Cell().Background(naranja).Padding(3).AlignCenter()
                    .Text(h).Bold().FontSize(8).FontColor(Color.FromHex("FFFFFF"));
        }

        private static void AgregarFilaPlanilla(
            TableDescriptor table,
            PlanillaEmpleado p,
            Color fondoGris)
        {
            table.Cell().Padding(2).Text(
                $"{p.Empleado.PrimerApellido} {p.Empleado.Nombre}");
            table.Cell().Padding(2).AlignRight().Text($"₡{p.SalarioOrdinario:N0}");
            table.Cell().Padding(2).AlignRight()
                .Text(p.MontoHorasExtras > 0 ? $"₡{p.MontoHorasExtras:N0}" : "—");
            table.Cell().Padding(2).AlignRight()
                .Text(p.AumentoAplicado > 0 ? $"₡{p.AumentoAplicado:N0}" : "—");
            table.Cell().Padding(2).AlignRight().Text($"₡{p.TotalDevengado:N0}").Bold();
            table.Cell().Padding(2).AlignRight().Text($"₡{p.DeduccionCCSS:N0}");
            table.Cell().Padding(2).AlignRight()
                .Text(p.DeduccionPrestamos > 0 ? $"₡{p.DeduccionPrestamos:N0}" : "—");
            table.Cell().Padding(2).AlignRight()
                .Text(p.DeduccionCreditoFerreteria > 0 ? $"₡{p.DeduccionCreditoFerreteria:N0}" : "—");
            table.Cell().Padding(2).AlignRight().Text($"₡{p.TotalDeducciones:N0}").Bold();
            table.Cell().Padding(2).AlignRight()
                .Text($"₡{p.NetoAPagar:N0}").Bold().FontColor(Color.FromHex("1B5E20"));
        }

    }
}