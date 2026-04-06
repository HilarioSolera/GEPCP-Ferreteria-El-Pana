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

        // ── GENERAR PDF COMPROBANTE PLANILLA ──────────────────────────────────

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
                                $"{planilla.Empleado.Nombre} " +
                                $"{planilla.Empleado.PrimerApellido} " +
                                $"{planilla.Empleado.SegundoApellido}");
                            table.Cell().Padding(3).Text("CÉDULA:").Bold();
                            table.Cell().Padding(3).Text(planilla.Empleado.Cedula);
                            table.Cell().Padding(3).Text("DEPARTAMENTO").Bold();
                            table.Cell().Padding(3).Text(planilla.Empleado.Departamento);
                            table.Cell().Padding(3).Text("PUESTO:").Bold();
                            table.Cell().Padding(3).Text(planilla.Empleado.Puesto);
                            table.Cell().Padding(3).Text("PERÍODO DE PAGO:").Bold();
                            table.Cell().ColumnSpan(3).Padding(3).Text(
                                $"{planilla.PeriodoPago.FechaInicio:dd/MM/yyyy} " +
                                $"AL {planilla.PeriodoPago.FechaFin:dd/MM/yyyy}");
                        });

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
                            table.Cell().Padding(3).AlignRight().Text($"₡{planilla.Empleado.SalarioBase:N2}");

                            table.Cell().Padding(3).Text("HORAS JORNADA ORDINARIA DIURNA");
                            table.Cell().Padding(3).AlignRight().Text($"₡{planilla.ValorHora:N2}");
                            table.Cell().Padding(3).AlignRight().Text($"{planilla.HorasOrdinarias:N2}");
                            table.Cell().Padding(3).AlignRight().Text($"₡{planilla.SalarioOrdinario:N2}");

                            table.Cell().Padding(3).Text("HORAS JORNADA EXTRAORDINARIA");
                            table.Cell().Padding(3).AlignRight().Text(
                                planilla.HorasExtras > 0 ? $"₡{planilla.ValorHoraExtra:N2}" : "");
                            table.Cell().Padding(3).AlignRight().Text(
                                planilla.HorasExtras > 0 ? $"{planilla.HorasExtras:N2}" : "0.00");
                            table.Cell().Padding(3).AlignRight().Text($"₡{planilla.MontoHorasExtras:N2}");

                            if (planilla.AumentoAplicado > 0)
                            {
                                table.Cell().Padding(3).Text("COMISIÓN / AUMENTO");
                                table.Cell().Padding(3).AlignRight().Text("");
                                table.Cell().Padding(3).AlignRight().Text("");
                                table.Cell().Padding(3).AlignRight().Text($"₡{planilla.AumentoAplicado:N2}");
                            }
                            if (planilla.MontoFeriados > 0)
                            {
                                table.Cell().Padding(3).Text("FERIADOS");
                                table.Cell().Padding(3).AlignRight().Text("");
                                table.Cell().Padding(3).AlignRight().Text("");
                                table.Cell().Padding(3).AlignRight().Text($"₡{planilla.MontoFeriados:N2}");
                            }

                            table.Cell().Background(fondoNaranja).Padding(3).Text("TOTAL (BRUTO)").Bold();
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight().Text("");
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight().Text("");
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight()
                                .Text($"₡{planilla.TotalDevengado:N2}").Bold();
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);
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
                            table.Cell().Padding(3).AlignCenter().Text($"{planilla.PorcentajeCCSS:N2}%");
                            table.Cell().Padding(3).AlignRight().Text($"₡{planilla.DeduccionCCSS:N2}");

                            if (planilla.DeduccionCreditoFerreteria > 0)
                            {
                                table.Cell().Padding(3).Text("FACTURAS DE CRÉDITO FERRETERÍA");
                                table.Cell().Padding(3).AlignCenter().Text("");
                                table.Cell().Padding(3).AlignRight().Text($"₡{planilla.DeduccionCreditoFerreteria:N2}");
                            }
                            if (planilla.DeduccionPrestamos > 0)
                            {
                                table.Cell().Padding(3).Text("PRÉSTAMO PERSONAL");
                                table.Cell().Padding(3).AlignCenter().Text("");
                                table.Cell().Padding(3).AlignRight().Text($"₡{planilla.DeduccionPrestamos:N2}");
                            }
                            if (planilla.DeduccionHorasNoLaboradas > 0)
                            {
                                table.Cell().Padding(3).Text("HORAS NO LABORADAS");
                                table.Cell().Padding(3).AlignCenter().Text("");
                                table.Cell().Padding(3).AlignRight().Text($"₡{planilla.DeduccionHorasNoLaboradas:N2}");
                            }
                            if (planilla.DeduccionIncapacidad > 0)
                            {
                                table.Cell().Padding(3).Text("INCAPACIDAD");
                                table.Cell().Padding(3).AlignCenter().Text("");
                                table.Cell().Padding(3).AlignRight().Text($"₡{planilla.DeduccionIncapacidad:N2}");
                            }
                            if (planilla.DeduccionVacaciones > 0)
                            {
                                table.Cell().Padding(3).Text("VACACIONES SIN PAGO");
                                table.Cell().Padding(3).AlignCenter().Text("");
                                table.Cell().Padding(3).AlignRight().Text($"₡{planilla.DeduccionVacaciones:N2}");
                            }
                            if (planilla.OtrasDeducciones > 0)
                            {
                                table.Cell().Padding(3).Text("OTRAS DEDUCCIONES");
                                table.Cell().Padding(3).AlignCenter().Text("");
                                table.Cell().Padding(3).AlignRight().Text($"₡{planilla.OtrasDeducciones:N2}");
                            }

                            table.Cell().Background(fondoRojo).Padding(3).Text("TOTAL DEDUCCIONES").Bold();
                            table.Cell().Background(fondoRojo).Padding(3).AlignCenter().Text("");
                            table.Cell().Background(fondoRojo).Padding(3).AlignRight()
                                .Text($"₡{planilla.TotalDeducciones:N2}").Bold();
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

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
                                    $"{planilla.Empleado.PrimerApellido} " +
                                    $"{planilla.Empleado.SegundoApellido} " +
                                    $"{planilla.Empleado.Nombre}")
                                    .FontSize(9).Italic();
                                c.Item().AlignCenter().Text(planilla.Empleado.Cedula)
                                    .FontSize(8).FontColor(Color.FromHex("666666"));
                            });
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
                            table.Cell().Background(fondoGris).Padding(3).Text("CONCEPTO").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignCenter().Text("% APLICADO").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignRight().Text("VALOR HORA").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignRight().Text("TOTAL").Bold();

                            table.Cell().Padding(3).Text("PAGO HRS EXTRA");
                            table.Cell().Padding(3).AlignCenter().Text($"{horasExtras.Porcentaje:N1}");
                            table.Cell().Padding(3).AlignRight().Text($"₡{horasExtras.ValorHora:N2}");
                            table.Cell().Padding(3).AlignRight().Text($"₡{horasExtras.MontoTotal:N2}");

                            table.Cell().Background(fondoNaranja).Padding(3).Text("TOTAL").Bold();
                            table.Cell().Background(fondoNaranja).Padding(3).AlignCenter().Text("");
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight().Text("");
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight()
                                .Text($"₡{horasExtras.MontoTotal:N2}").Bold();
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

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
                            if (!string.IsNullOrEmpty(incapacidad.TiqueteCCSS))
                            {
                                table.Cell().Padding(3).Text("TIQUETE CCSS:").Bold();
                                table.Cell().ColumnSpan(3).Padding(3).Text(incapacidad.TiqueteCCSS);
                            }
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);
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
                            table.Cell().Background(fondoGris).Padding(3).Text("CONCEPTO").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignCenter().Text("% APLICADO").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignRight().Text("MONTO POR DÍA").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignRight().Text("TOTAL").Bold();

                            var concepto = incapacidad.ResponsablePago switch
                            {
                                ResponsablePago.Patrono => "Pago Patrono",
                                ResponsablePago.CCSS => "Pago CCSS",
                                ResponsablePago.INS => "Pago INS",
                                _ => "Pago Incapacidad"
                            };
                            table.Cell().Padding(3).Text(concepto);
                            table.Cell().Padding(3).AlignCenter().Text($"{incapacidad.PorcentajePago:N0}%");
                            table.Cell().Padding(3).AlignRight().Text($"₡{incapacidad.MontoPorDia:N2}");
                            table.Cell().Padding(3).AlignRight().Text($"₡{incapacidad.MontoTotal:N2}");

                            table.Cell().Padding(3).Text($"Entidad: {incapacidad.Entidad}");
                            table.Cell().Padding(3).AlignCenter().Text("");
                            table.Cell().Padding(3).AlignRight().Text("");
                            table.Cell().Padding(3).AlignRight().Text("");

                            table.Cell().Background(fondoNaranja).Padding(3).Text("TOTAL").Bold();
                            table.Cell().Background(fondoNaranja).Padding(3).AlignCenter().Text("");
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight().Text("");
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight()
                                .Text($"₡{incapacidad.MontoTotal:N2}").Bold();
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(6);
                                c.RelativeColumn(4);
                            });
                            table.Cell().Padding(5).Text("MONTO TOTAL INCAPACIDAD").Bold().FontSize(14);
                            table.Cell().Padding(5).AlignRight()
                                .Text($"₡{incapacidad.MontoTotal:N2}")
                                .Bold().FontSize(14).FontColor(naranja);
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        if (!string.IsNullOrEmpty(incapacidad.Observaciones))
                        {
                            col.Item().PaddingBottom(8).Column(c =>
                            {
                                c.Item().Text("OBSERVACIONES:").Bold().FontSize(9);
                                c.Item().PaddingTop(2).Text(incapacidad.Observaciones)
                                    .FontSize(9).FontColor(Color.FromHex("444444")).Italic();
                            });
                        }

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
                        col.Item().PaddingBottom(4).Text("DETALLE:").Bold().Underline();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(6);
                                c.RelativeColumn(4);
                            });
                            table.Cell().Background(fondoGris).Padding(3).Text("CONCEPTO").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignRight().Text("MONTO TOTAL").Bold();

                            table.Cell().Padding(3).Text($"AGUINALDO {aguinaldo.Anio}");
                            table.Cell().Padding(3).AlignRight().Text($"₡{aguinaldo.MontoTotal:N2}");

                            table.Cell().Background(fondoNaranja).Padding(3).Text("TOTAL").Bold();
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight()
                                .Text($"₡{aguinaldo.MontoTotal:N2}").Bold();
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

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

                        if (!string.IsNullOrEmpty(aguinaldo.Observaciones))
                        {
                            col.Item().PaddingBottom(8).Column(c =>
                            {
                                c.Item().Text("OBSERVACIONES:").Bold().FontSize(9);
                                c.Item().PaddingTop(2).Text(aguinaldo.Observaciones)
                                    .FontSize(9).FontColor(Color.FromHex("444444")).Italic();
                            });
                        }

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
            var grisTexto = Color.FromHex("999999");
            var fondoGris = Color.FromHex("F5F5F5");
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

                        var grupos = planillas.GroupBy(p => p.Empleado.Departamento);
                        foreach (var grupo in grupos)
                        {
                            col.Item().PaddingTop(6).Background(Color.FromHex("333333"))
                                .Padding(4).Text(grupo.Key.ToUpper())
                                .Bold().FontSize(9).FontColor(Color.FromHex("FFFFFF"));

                            col.Item().Table(table =>
                            {
                                DefinirColumnasPlanilla(table);
                                AgregarHeaderPlanilla(table, naranja);
                                foreach (var p in grupo)
                                    AgregarFilaPlanilla(table, p, fondoGris);

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

        // ── GENERAR PDF BOLETA VACACIONES ─────────────────────────────────────

        public byte[] GenerarBoletaVacaciones(
            Vacacion vacacion,
            decimal diasBase,
            decimal diasTomados,
            decimal disponibles,
            string emisor)
        {
            var naranja = Color.FromHex("FF7A00");
            var grisClaro = Color.FromHex("CCCCCC");
            var grisTexto = Color.FromHex("999999");
            var fondoGris = Color.FromHex("F5F5F5");
            var fondoVerde = Color.FromHex("E8F5E9");
            var fondoRojo = Color.FromHex("FFEBEE");
            var fondoAmari = Color.FromHex("FFF8E1");
            var logoBytes = ObtenerLogoBytes();

            var emp = vacacion.Empleado;
            var antiguedad = Math.Round((DateTime.Today - emp.FechaIngreso).TotalDays / 365, 1);
            var saldoTrasVacacion = disponibles - vacacion.DiasHabiles;

            var colorEstado = vacacion.Estado switch
            {
                EstadoVacacion.Aprobada => Color.FromHex("1B5E20"),
                EstadoVacacion.Rechazada => Color.FromHex("B71C1C"),
                _ => Color.FromHex("E65100")
            };
            var textoEstado = vacacion.Estado switch
            {
                EstadoVacacion.Aprobada => "APROBADA",
                EstadoVacacion.Rechazada => "RECHAZADA",
                _ => "PENDIENTE"
            };

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));
                    page.Content().Column(col =>
                    {
                        // ── Encabezado ────────────────────────────────────────
                        col.Item().Row(row =>
                        {
                            if (logoBytes != null)
                                row.ConstantItem(80).Padding(4).Image(logoBytes).FitArea();
                            else
                                row.ConstantItem(80).Text("");

                            row.RelativeItem().AlignMiddle().Column(c =>
                            {
                                c.Item().AlignCenter().Text("FERRETERÍA EL PANA SRL")
                                    .Bold().FontSize(16).FontColor(naranja);
                                c.Item().AlignCenter().Text("DEPARTAMENTO DE RECURSOS HUMANOS")
                                    .Bold().FontSize(12);
                                c.Item().AlignCenter().Text("BOLETA DE VACACIONES")
                                    .Bold().FontSize(12);
                                c.Item().AlignCenter().Text($"N.° {vacacion.VacacionId:D6}")
                                    .FontSize(9).FontColor(grisTexto).Italic();
                            });

                            row.ConstantItem(90).AlignMiddle().AlignRight().Column(c =>
                            {
                                c.Item().Background(colorEstado).Padding(6)
                                    .Text(textoEstado)
                                    .Bold().FontSize(9).FontColor(Color.FromHex("FFFFFF"));
                            });
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

                        // ── Datos del empleado ────────────────────────────────
                        col.Item().PaddingBottom(4).Text("DATOS DEL EMPLEADO")
                            .Bold().FontSize(9).FontColor(grisTexto);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(100);
                                c.RelativeColumn(3);
                                c.ConstantColumn(100);
                                c.RelativeColumn(2);
                            });
                            table.Cell().Padding(3).Text("NOMBRE:").Bold();
                            table.Cell().Padding(3).Text(
                                $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim());
                            table.Cell().Padding(3).Text("CÉDULA:").Bold();
                            table.Cell().Padding(3).Text(emp.Cedula);

                            table.Cell().Padding(3).Text("DEPARTAMENTO:").Bold();
                            table.Cell().Padding(3).Text(emp.Departamento);
                            table.Cell().Padding(3).Text("PUESTO:").Bold();
                            table.Cell().Padding(3).Text(emp.Puesto);

                            table.Cell().Padding(3).Text("FECHA INGRESO:").Bold();
                            table.Cell().Padding(3).Text(
                                $"{emp.FechaIngreso:dd/MM/yyyy} ({antiguedad} año(s))");
                            table.Cell().Padding(3).Text("SALARIO DIARIO:").Bold();
                            table.Cell().Padding(3).Text($"₡{vacacion.SalarioDiario:N2}");
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Período ───────────────────────────────────────────
                        col.Item().PaddingBottom(4).Text("PERÍODO DE VACACIONES")
                            .Bold().FontSize(9).FontColor(grisTexto);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(100);
                                c.RelativeColumn(2);
                                c.ConstantColumn(100);
                                c.RelativeColumn(2);
                            });
                            table.Cell().Padding(3).Text("FECHA INICIO:").Bold();
                            table.Cell().Padding(3).Text($"{vacacion.FechaInicio:dd/MM/yyyy}");
                            table.Cell().Padding(3).Text("FECHA FIN:").Bold();
                            table.Cell().Padding(3).Text($"{vacacion.FechaFin:dd/MM/yyyy}");

                            table.Cell().Padding(3).Text("DÍAS SOLICITADOS:").Bold();
                            table.Cell().Padding(3).Text($"{vacacion.DiasHabiles:N1} día(s)");
                            table.Cell().Padding(3).Text("TIPO:").Bold();
                            table.Cell().Padding(3).Text(
                                vacacion.Tipo == TipoVacacion.ConPago ? "Con Pago" : "Sin Pago");
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Resumen de días ───────────────────────────────────
                        col.Item().PaddingBottom(6)
                            .Text("RESUMEN DE DÍAS (Art. 153 Código de Trabajo CR)")
                            .Bold().FontSize(9).FontColor(grisTexto);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            table.Cell().Background(fondoGris).Padding(5).AlignCenter()
                                .Text("ACUMULADOS POR LEY").Bold().FontSize(8);
                            table.Cell().Background(fondoGris).Padding(5).AlignCenter()
                                .Text("TOMADOS ANTERIORES").Bold().FontSize(8);
                            table.Cell().Background(fondoAmari).Padding(5).AlignCenter()
                                .Text("DÍAS SOLICITADOS").Bold().FontSize(8);
                            table.Cell().Background(fondoVerde).Padding(5).AlignCenter()
                                .Text("SALDO RESTANTE").Bold().FontSize(8);

                            table.Cell().Background(fondoGris).Padding(8).AlignCenter()
                                .Text($"{diasBase}").FontSize(16).Bold();
                            table.Cell().Background(fondoRojo).Padding(8).AlignCenter()
                                .Text($"{diasTomados}").FontSize(16).Bold()
                                .FontColor(Color.FromHex("B71C1C"));
                            table.Cell().Background(fondoAmari).Padding(8).AlignCenter()
                                .Text($"{vacacion.DiasHabiles}").FontSize(16).Bold()
                                .FontColor(Color.FromHex("E65100"));
                            table.Cell().Background(fondoVerde).Padding(8).AlignCenter()
                                .Text($"{saldoTrasVacacion:N1}").FontSize(16).Bold()
                                .FontColor(Color.FromHex("1B5E20"));
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Deducción / pago ──────────────────────────────────
                        if (vacacion.Tipo == TipoVacacion.SinPago && vacacion.MontoDeducido > 0)
                        {
                            col.Item().PaddingBottom(4).Text("DEDUCCIÓN EN PLANILLA")
                                .Bold().FontSize(9).FontColor(grisTexto);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(6);
                                    c.RelativeColumn(4);
                                });
                                table.Cell().Background(fondoGris).Padding(3).Text("CONCEPTO").Bold();
                                table.Cell().Background(fondoGris).Padding(3).AlignRight().Text("MONTO").Bold();

                                table.Cell().Padding(3).Text(
                                    $"Vacaciones sin pago — {vacacion.DiasHabiles} días " +
                                    $"x ₡{vacacion.SalarioDiario:N2}/día");
                                table.Cell().Padding(3).AlignRight().Text($"₡{vacacion.MontoDeducido:N2}");

                                table.Cell().Background(fondoRojo).Padding(3).Text("TOTAL A DEDUCIR").Bold();
                                table.Cell().Background(fondoRojo).Padding(3).AlignRight()
                                    .Text($"₡{vacacion.MontoDeducido:N2}").Bold()
                                    .FontColor(Color.FromHex("B71C1C"));
                            });
                        }
                        else if (vacacion.Tipo == TipoVacacion.ConPago)
                        {
                            col.Item().Background(fondoVerde).Padding(8).Row(row =>
                            {
                                row.RelativeItem()
                                    .Text("Vacaciones con pago — El empleado recibirá su salario " +
                                          "ordinario durante el período de vacaciones.")
                                    .FontColor(Color.FromHex("1B5E20")).Bold().FontSize(9);
                            });
                        }

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Observaciones ─────────────────────────────────────
                        col.Item().PaddingBottom(4).Text("OBSERVACIONES")
                            .Bold().FontSize(9).FontColor(grisTexto);
                        col.Item().Background(fondoGris).Padding(8).Text(
                            string.IsNullOrWhiteSpace(vacacion.Observaciones)
                                ? "Sin observaciones."
                                : vacacion.Observaciones)
                            .FontSize(9).Italic().FontColor(Color.FromHex("444444"));

                        col.Item().PaddingVertical(16).LineHorizontal(1).LineColor(naranja);

                        // ── Firmas ────────────────────────────────────────────
                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(5);
                                c.RelativeColumn(1);
                                c.RelativeColumn(4);
                            });

                            table.Cell().Padding(3).Column(c =>
                            {
                                c.Item().Text("FIRMA DEL EMPLEADO:").Bold().FontSize(9);
                                c.Item().PaddingTop(40)
                                    .BorderBottom(1).BorderColor(Color.FromHex("333333")).Text("");
                                c.Item().PaddingTop(5).AlignCenter()
                                    .Text($"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim())
                                    .FontSize(8).Italic();
                                c.Item().AlignCenter().Text(emp.Cedula)
                                    .FontSize(7).FontColor(grisTexto);
                            });

                            table.Cell().Text("");

                            table.Cell().Padding(3).Column(c =>
                            {
                                c.Item().Text("AUTORIZADO POR:").Bold().FontSize(9);
                                c.Item().PaddingTop(40)
                                    .BorderBottom(1).BorderColor(Color.FromHex("333333")).Text("");
                                c.Item().PaddingTop(5).AlignCenter()
                                    .Text("Recursos Humanos / Jefatura")
                                    .FontSize(8).Italic();
                                c.Item().AlignCenter().Text($"Emitido por: {emisor}")
                                    .FontSize(7).FontColor(grisTexto);
                            });
                        });

                        col.Item().PaddingTop(16).AlignCenter()
                            .Text($"Emitido el {DateTime.Now:dd/MM/yyyy HH:mm} — " +
                                  "Art. 153-161 Código de Trabajo CR — " +
                                  "Sistema GEPCP Ferretería El Pana")
                            .FontSize(7).FontColor(grisTexto).Italic();
                    });
                });
            });

            return doc.GeneratePdf();
        }

        // ── HELPERS PRIVADOS TABLA PLANILLA GENERAL ───────────────────────────

        private static void DefinirColumnasPlanilla(TableDescriptor table)
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(3);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
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
            table.Cell().Padding(2)
                .Text($"{p.Empleado.PrimerApellido} {p.Empleado.Nombre}");
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

        // ── GENERAR PDF COMISIÓN ──────────────────────────────────────────────────

        public byte[] GenerarPDFComision(Comision comision)
        {
            var naranja = Color.FromHex("FF7A00");
            var grisClaro = Color.FromHex("CCCCCC");
            var grisTexto = Color.FromHex("999999");
            var fondoGris = Color.FromHex("F5F5F5");
            var fondoNaranja = Color.FromHex("FFF3E0");
            var fondoVerde = Color.FromHex("E8F5E9");
            var logoBytes = ObtenerLogoBytes();

            var emp = comision.Empleado;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // ── Encabezado ────────────────────────────────────────────
                        col.Item().Row(row =>
                        {
                            if (logoBytes != null)
                                row.ConstantItem(80).Padding(4).Image(logoBytes).FitArea();
                            else
                                row.ConstantItem(80).Text("");

                            row.RelativeItem().AlignMiddle().Column(c =>
                            {
                                c.Item().AlignCenter().Text("FERRETERÍA EL PANA SRL")
                                    .Bold().FontSize(16).FontColor(naranja);
                                c.Item().AlignCenter().Text("DEPARTAMENTO DE RECURSOS HUMANOS")
                                    .Bold().FontSize(12);
                                c.Item().AlignCenter().Text("COMPROBANTE DE COMISIÓN")
                                    .Bold().FontSize(12);
                                c.Item().AlignCenter()
                                    .Text($"N.° {comision.ComisionId:D6}")
                                    .FontSize(9).FontColor(grisTexto).Italic();
                            });

                            row.ConstantItem(80).Text("");
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

                        // ── Datos del empleado ────────────────────────────────────
                        col.Item().PaddingBottom(4)
                            .Text("DATOS DEL EMPLEADO")
                            .Bold().FontSize(9).FontColor(grisTexto);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(100);
                                c.RelativeColumn(3);
                                c.ConstantColumn(100);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Padding(3).Text("NOMBRE:").Bold();
                            table.Cell().Padding(3).Text(
                                $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim());
                            table.Cell().Padding(3).Text("CÉDULA:").Bold();
                            table.Cell().Padding(3).Text(emp.Cedula);

                            table.Cell().Padding(3).Text("DEPARTAMENTO:").Bold();
                            table.Cell().Padding(3).Text(emp.Departamento);
                            table.Cell().Padding(3).Text("PUESTO:").Bold();
                            table.Cell().Padding(3).Text(emp.Puesto);
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Datos de la comisión ──────────────────────────────────
                        col.Item().PaddingBottom(4)
                            .Text("DETALLE DE LA COMISIÓN")
                            .Bold().FontSize(9).FontColor(grisTexto);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(100);
                                c.RelativeColumn(3);
                                c.ConstantColumn(100);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Padding(3).Text("FECHA:").Bold();
                            table.Cell().Padding(3).Text($"{comision.Fecha:dd/MM/yyyy}");
                            table.Cell().Padding(3).Text("PERÍODO:").Bold();
                            table.Cell().Padding(3).Text(
                                comision.PeriodoPago?.Descripcion ?? "—");

                            table.Cell().Padding(3).Text("DESCRIPCIÓN:").Bold();
                            table.Cell().ColumnSpan(3).Padding(3).Text(comision.Descripcion);
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Tabla monto ───────────────────────────────────────────
                        col.Item().PaddingBottom(4)
                            .Text("MONTO")
                            .Bold().FontSize(9).FontColor(grisTexto);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(6);
                                c.RelativeColumn(4);
                            });

                            table.Cell().Background(fondoGris).Padding(3)
                                .Text("CONCEPTO").Bold();
                            table.Cell().Background(fondoGris).Padding(3).AlignRight()
                                .Text("MONTO TOTAL").Bold();

                            table.Cell().Padding(3).Text("COMISIÓN");
                            table.Cell().Padding(3).AlignRight()
                                .Text($"₡{comision.Monto:N2}");

                            table.Cell().Background(fondoNaranja).Padding(3)
                                .Text("TOTAL").Bold();
                            table.Cell().Background(fondoNaranja).Padding(3).AlignRight()
                                .Text($"₡{comision.Monto:N2}").Bold();
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

                        // ── Monto destacado ───────────────────────────────────────
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(6);
                                c.RelativeColumn(4);
                            });
                            table.Cell().Padding(5).Text("MONTO A PAGAR")
                                .Bold().FontSize(14);
                            table.Cell().Padding(5).AlignRight()
                                .Text($"₡{comision.Monto:N2}")
                                .Bold().FontSize(14).FontColor(naranja);
                        });

                        col.Item().PaddingVertical(16).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Firmas ────────────────────────────────────────────────
                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(5);
                                c.RelativeColumn(1);
                                c.RelativeColumn(4);
                            });

                            table.Cell().Padding(3).Column(c =>
                            {
                                c.Item().Text("RECIBO CONFORME:").Bold().FontSize(9);
                                c.Item().PaddingTop(40)
                                    .BorderBottom(1).BorderColor(Color.FromHex("333333")).Text("");
                                c.Item().PaddingTop(5).AlignCenter()
                                    .Text($"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim())
                                    .FontSize(8).Italic();
                                c.Item().AlignCenter().Text(emp.Cedula)
                                    .FontSize(7).FontColor(grisTexto);
                            });

                            table.Cell().Text("");

                            table.Cell().Padding(3).Column(c =>
                            {
                                c.Item().Text("AUTORIZADO POR:").Bold().FontSize(9);
                                c.Item().PaddingTop(40)
                                    .BorderBottom(1).BorderColor(Color.FromHex("333333")).Text("");
                                c.Item().PaddingTop(5).AlignCenter()
                                    .Text("Recursos Humanos / Jefatura")
                                    .FontSize(8).Italic();
                            });
                        });

                        col.Item().PaddingTop(16).AlignCenter()
                            .Text($"Emitido el {DateTime.Now:dd/MM/yyyy HH:mm} — " +
                                  "Sistema GEPCP Ferretería El Pana")
                            .FontSize(7).FontColor(grisTexto).Italic();
                    });
                });
            });

            return doc.GeneratePdf();
        }
        // ── GENERAR PDF FINIQUITO PRÉSTAMO ────────────────────────────────────────

        public byte[] GenerarFiniquitoPrestamo(Prestamo prestamo)
        {
            var naranja = Color.FromHex("FF7A00");
            var grisClaro = Color.FromHex("CCCCCC");
            var grisTexto = Color.FromHex("999999");
            var fondoGris = Color.FromHex("F5F5F5");
            var fondoVerde = Color.FromHex("E8F5E9");
            var fondoRojo = Color.FromHex("FFEBEE");
            var fondoNaranja = Color.FromHex("FFF3E0");
            var logoBytes = ObtenerLogoBytes();

            var emp = prestamo.Empleado;
            var abonos = prestamo.AbonosPrestamo
                .OrderBy(a => a.FechaAbono)
                .ToList();
            var montoOriginal = prestamo.MontoOriginal > 0
                ? prestamo.MontoOriginal
                : prestamo.CuotaMensual * prestamo.Cuotas;
            var totalPagado = abonos.Sum(a => a.Monto);
            var fechaInicio = prestamo.FechaPrestamo;
            var fechaFin = abonos.Any()
                ? abonos.Last().FechaAbono
                : DateTime.Now;
            var duracionDias = (int)(fechaFin - fechaInicio).TotalDays;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        // ── Encabezado ────────────────────────────────────────────
                        col.Item().Row(row =>
                        {
                            if (logoBytes != null)
                                row.ConstantItem(80).Padding(4).Image(logoBytes).FitArea();
                            else
                                row.ConstantItem(80).Text("");

                            row.RelativeItem().AlignMiddle().Column(c =>
                            {
                                c.Item().AlignCenter().Text("FERRETERÍA EL PANA SRL")
                                    .Bold().FontSize(16).FontColor(naranja);
                                c.Item().AlignCenter().Text("DEPARTAMENTO DE RECURSOS HUMANOS")
                                    .Bold().FontSize(12);
                                c.Item().AlignCenter().Text("FINIQUITO DE PRÉSTAMO")
                                    .Bold().FontSize(12);
                                c.Item().AlignCenter()
                                    .Text($"N.° {prestamo.PrestamoId:D6}")
                                    .FontSize(9).FontColor(grisTexto).Italic();
                            });

                            row.ConstantItem(90).AlignMiddle().AlignRight().Column(c =>
                            {
                                c.Item().Background(Color.FromHex("1B5E20"))
                                    .Padding(6)
                                    .Text("✓ SALDADO")
                                    .Bold().FontSize(10)
                                    .FontColor(Color.FromHex("FFFFFF"));
                            });
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

                        // ── Datos del empleado ────────────────────────────────────
                        col.Item().PaddingBottom(4)
                            .Text("DATOS DEL EMPLEADO")
                            .Bold().FontSize(9).FontColor(grisTexto);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(100);
                                c.RelativeColumn(3);
                                c.ConstantColumn(100);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Padding(3).Text("NOMBRE:").Bold();
                            table.Cell().Padding(3).Text(
                                $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim());
                            table.Cell().Padding(3).Text("CÉDULA:").Bold();
                            table.Cell().Padding(3).Text(emp.Cedula);

                            table.Cell().Padding(3).Text("DEPARTAMENTO:").Bold();
                            table.Cell().Padding(3).Text(emp.Departamento);
                            table.Cell().Padding(3).Text("PUESTO:").Bold();
                            table.Cell().Padding(3).Text(emp.Puesto);
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Resumen del préstamo ──────────────────────────────────
                        col.Item().PaddingBottom(4)
                            .Text("RESUMEN DEL PRÉSTAMO")
                            .Bold().FontSize(9).FontColor(grisTexto);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(120);
                                c.RelativeColumn(2);
                                c.ConstantColumn(120);
                                c.RelativeColumn(2);
                            });

                            table.Cell().Padding(3).Text("MONTO ORIGINAL:").Bold();
                            table.Cell().Padding(3).Text($"₡{montoOriginal:N2}");
                            table.Cell().Padding(3).Text("CUOTA QUINCENAL:").Bold();
                            table.Cell().Padding(3).Text($"₡{prestamo.CuotaMensual:N2}");

                            table.Cell().Padding(3).Text("FECHA OTORGADO:").Bold();
                            table.Cell().Padding(3).Text($"{fechaInicio:dd/MM/yyyy}");
                            table.Cell().Padding(3).Text("FECHA SALDADO:").Bold();
                            table.Cell().Padding(3).Text($"{fechaFin:dd/MM/yyyy}");

                            table.Cell().Padding(3).Text("CUOTAS PACTADAS:").Bold();
                            table.Cell().Padding(3).Text($"{prestamo.Cuotas}");
                            table.Cell().Padding(3).Text("DURACIÓN REAL:").Bold();
                            table.Cell().Padding(3).Text($"{duracionDias} días");

                            table.Cell().Padding(3).Text("ABONOS REALIZADOS:").Bold();
                            table.Cell().Padding(3).Text($"{abonos.Count} abono(s)");
                            table.Cell().Padding(3).Text("TOTAL PAGADO:").Bold();
                            table.Cell().Padding(3).Text($"₡{totalPagado:N2}");
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Historial de abonos ───────────────────────────────────
                        col.Item().PaddingBottom(6)
                            .Text("HISTORIAL COMPLETO DE ABONOS")
                            .Bold().FontSize(9).FontColor(grisTexto);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(25);  // #
                                c.RelativeColumn(3);   // Fecha
                                c.RelativeColumn(2);   // Monto
                                c.RelativeColumn(2);   // Saldo anterior
                                c.RelativeColumn(2);   // Saldo después
                                c.RelativeColumn(4);   // Observaciones
                            });

                            // Headers
                            table.Cell().Background(Color.FromHex("222222"))
                                .Padding(4).AlignCenter()
                                .Text("#").Bold().FontSize(8)
                                .FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(Color.FromHex("222222"))
                                .Padding(4)
                                .Text("FECHA Y HORA").Bold().FontSize(8)
                                .FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(Color.FromHex("222222"))
                                .Padding(4).AlignRight()
                                .Text("MONTO ABONO").Bold().FontSize(8)
                                .FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(Color.FromHex("222222"))
                                .Padding(4).AlignRight()
                                .Text("SALDO ANTERIOR").Bold().FontSize(8)
                                .FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(Color.FromHex("222222"))
                                .Padding(4).AlignRight()
                                .Text("SALDO DESPUÉS").Bold().FontSize(8)
                                .FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(Color.FromHex("222222"))
                                .Padding(4)
                                .Text("OBSERVACIONES").Bold().FontSize(8)
                                .FontColor(Color.FromHex("FFFFFF"));

                            // Filas
                            var saldoCalc = montoOriginal;
                            int num = 1;

                            foreach (var a in abonos)
                            {
                                var saldoAnt = saldoCalc;
                                saldoCalc = Math.Round(saldoCalc - a.Monto, 2);
                                if (saldoCalc < 0) saldoCalc = 0;

                                var bg = num % 2 == 0
                                    ? Color.FromHex("F5F5F5")
                                    : Color.FromHex("FFFFFF");

                                table.Cell().Background(bg).Padding(3).AlignCenter()
                                    .Text(num.ToString()).FontSize(8)
                                    .FontColor(grisTexto);
                                table.Cell().Background(bg).Padding(3)
                                    .Text(a.FechaAbono.ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                                table.Cell().Background(bg).Padding(3).AlignRight()
                                    .Text($"₡{a.Monto:N2}").Bold().FontSize(8)
                                    .FontColor(Color.FromHex("1B5E20"));
                                table.Cell().Background(bg).Padding(3).AlignRight()
                                    .Text($"₡{saldoAnt:N2}").FontSize(8)
                                    .FontColor(grisTexto);
                                table.Cell().Background(bg).Padding(3).AlignRight()
                                    .Text($"₡{saldoCalc:N2}").FontSize(8)
                                    .FontColor(saldoCalc == 0
                                        ? Color.FromHex("1B5E20")
                                        : Color.FromHex("C55000"));
                                table.Cell().Background(bg).Padding(3)
                                    .Text(a.Observaciones ?? "—").FontSize(7)
                                    .FontColor(grisTexto);

                                num++;
                            }

                            // Fila total
                            table.Cell().Background(naranja).Padding(4).AlignCenter()
                                .Text("✓").Bold().FontSize(9)
                                .FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(naranja).Padding(4)
                                .Text("TOTAL PAGADO").Bold().FontSize(8)
                                .FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(naranja).Padding(4).AlignRight()
                                .Text($"₡{totalPagado:N2}").Bold().FontSize(9)
                                .FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(naranja).Padding(4).AlignRight()
                                .Text($"₡{montoOriginal:N2}").Bold().FontSize(8)
                                .FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(naranja).Padding(4).AlignRight()
                                .Text("₡0.00").Bold().FontSize(9)
                                .FontColor(Color.FromHex("FFFFFF"));
                            table.Cell().Background(naranja).Padding(4)
                                .Text($"{abonos.Count} abono(s)").FontSize(8)
                                .FontColor(Color.FromHex("FFFFFF"));
                        });

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(naranja);

                        // ── Recuadro de finiquito ─────────────────────────────────
                        col.Item().Background(fondoVerde).Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("PRÉSTAMO COMPLETAMENTE SALDADO")
                                    .Bold().FontSize(13)
                                    .FontColor(Color.FromHex("1B5E20"));
                                c.Item().PaddingTop(4)
                                    .Text($"El empleado {emp.PrimerApellido} {emp.Nombre} ha " +
                                          $"cancelado en su totalidad el préstamo N.° " +
                                          $"{prestamo.PrestamoId:D6} por un monto original de " +
                                          $"₡{montoOriginal:N2}, mediante {abonos.Count} abono(s) " +
                                          $"realizados entre el {fechaInicio:dd/MM/yyyy} " +
                                          $"y el {fechaFin:dd/MM/yyyy}.")
                                    .FontSize(9).FontColor(Color.FromHex("1B5E20"));
                            });
                        });

                        col.Item().PaddingVertical(16).LineHorizontal(0.5f).LineColor(grisClaro);

                        // ── Firmas ────────────────────────────────────────────────
                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(5);
                                c.RelativeColumn(1);
                                c.RelativeColumn(4);
                            });

                            table.Cell().Padding(3).Column(c =>
                            {
                                c.Item().Text("FIRMA DEL EMPLEADO:").Bold().FontSize(9);
                                c.Item().PaddingTop(40)
                                    .BorderBottom(1)
                                    .BorderColor(Color.FromHex("333333")).Text("");
                                c.Item().PaddingTop(5).AlignCenter()
                                    .Text($"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim())
                                    .FontSize(8).Italic();
                                c.Item().AlignCenter().Text(emp.Cedula)
                                    .FontSize(7).FontColor(grisTexto);
                            });

                            table.Cell().Text("");

                            table.Cell().Padding(3).Column(c =>
                            {
                                c.Item().Text("AUTORIZADO POR:").Bold().FontSize(9);
                                c.Item().PaddingTop(40)
                                    .BorderBottom(1)
                                    .BorderColor(Color.FromHex("333333")).Text("");
                                c.Item().PaddingTop(5).AlignCenter()
                                    .Text("Recursos Humanos / Jefatura")
                                    .FontSize(8).Italic();
                                c.Item().AlignCenter()
                                    .Text("Ferretería El Pana SRL")
                                    .FontSize(7).FontColor(grisTexto);
                            });
                        });

                        col.Item().PaddingTop(16).AlignCenter()
                            .Text($"Documento generado el {DateTime.Now:dd/MM/yyyy HH:mm} — " +
                                  "Sistema GEPCP Ferretería El Pana — " +
                                  "Cédula Jurídica: 3-102-745359")
                            .FontSize(7).FontColor(grisTexto).Italic();
                    });
                });
            });

            return doc.GeneratePdf();
        }
    }

}