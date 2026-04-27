using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using GEPCP_Ferreteria_El_Pana.Models;

namespace GEPCP_Ferreteria_El_Pana.Services
{
    /// <summary>
    /// Extensiones para generar PDFs sin firmas (versiones digitales para email)
    /// </summary>
    public static class ComprobantePlanillaServiceExtensions
    {
        private static readonly Color Naranja = Color.FromHex("FF7A00");
        private static readonly Color NaranjaOsc = Color.FromHex("E56E00");
        private static readonly Color GrisClaro = Color.FromHex("DDDDDD");
        private static readonly Color GrisTexto = Color.FromHex("888888");
        private static readonly Color GrisFondo = Color.FromHex("F7F7F7");
        private static readonly Color Verde = Color.FromHex("1B5E20");
        private static readonly Color VerdeFondo = Color.FromHex("E8F5E9");
        private static readonly Color Rojo = Color.FromHex("B71C1C");
        private static readonly Color RojoFondo = Color.FromHex("FFEBEE");
        private static readonly Color NaranjaFondo = Color.FromHex("FFF3E0");
        private static readonly Color Oscuro = Color.FromHex("1A1A2E");
        private static readonly Color Blanco = Color.FromHex("FFFFFF");

        // HORAS EXTRAS SIN FIRMAS
        public static byte[] GenerarPDFHorasExtrasSinFirmas(this ComprobantePlanillaService service, HorasExtras hx, string usuario = "")
        {
            var logo = ObtenerLogoBytes(service);
            var emp = hx.Empleado;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        EncabezadoDigital(col, logo, "COMPROBANTE DE HORAS EXTRAS — COPIA DIGITAL",
                            $"Período: {hx.PeriodoPago.Descripcion}");

                        col.Item().Padding(20).Column(inner =>
                        {
                            SeccionLabel(inner, "Datos del Empleado");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(90); c.RelativeColumn(3);
                                    c.ConstantColumn(90); c.RelativeColumn(2);
                                });
                                FilaDatos(t, "NOMBRE:",
                                    $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim(),
                                    "CÉDULA:", emp.Cedula);
                                FilaDatos(t, "DEPARTAMENTO:", emp.Departamento,
                                    "PUESTO:", emp.Puesto);
                                FilaDatos(t, "TOTAL HORAS:", $"{hx.TotalHoras:N2} hrs",
                                    "PORCENTAJE:", $"{hx.Porcentaje:N1}%");
                            });

                            SeccionLabel(inner, "Detalle");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(4); c.RelativeColumn(2);
                                    c.RelativeColumn(2); c.RelativeColumn(2);
                                });
                                foreach (var h in new[] { "CONCEPTO", "% APLICADO", "VALOR HORA", "TOTAL" })
                                    t.Cell().Background(Naranja).Padding(4)
                                        .Text(h).Bold().FontSize(8).FontColor(Blanco);

                                t.Cell().Background(GrisFondo).Padding(3).Text("Pago Horas Extra").FontSize(8);
                                t.Cell().Padding(3).AlignCenter().Text($"{hx.Porcentaje:N1}%").FontSize(8);
                                t.Cell().Padding(3).AlignRight().Text($"₡{hx.ValorHora:N2}").FontSize(8);
                                t.Cell().Padding(3).AlignRight().Text($"₡{hx.MontoTotal:N2}").FontSize(8);

                                t.Cell().Background(NaranjaFondo).Padding(3).Text("TOTAL").Bold().FontSize(8);
                                t.Cell().Background(NaranjaFondo).Padding(3).Text("").FontSize(8);
                                t.Cell().Background(NaranjaFondo).Padding(3).Text("").FontSize(8);
                                t.Cell().Background(NaranjaFondo).Padding(3).AlignRight()
                                    .Text($"₡{hx.MontoTotal:N2}").Bold().FontSize(8).FontColor(NaranjaOsc);
                            });

                            inner.Item().PaddingTop(10).Background(Oscuro).Padding(12).Row(row =>
                            {
                                row.RelativeItem().Text("MONTO A PAGAR")
                                    .Bold().FontSize(14).FontColor(Blanco);
                                row.ConstantItem(180).AlignRight()
                                    .Text($"₡{hx.MontoTotal:N2}")
                                    .Bold().FontSize(18).FontColor(Naranja);
                            });

                            PiePaginaDigital(inner, usuario);
                        });
                    });
                });
            }).GeneratePdf();
        }

        // INCAPACIDAD SIN FIRMAS
        public static byte[] GenerarPDFIncapacidadSinFirmas(this ComprobantePlanillaService service, Incapacidad inc, string usuario = "")
        {
            var logo = ObtenerLogoBytes(service);
            var emp = inc.Empleado;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        EncabezadoDigital(col, logo, "COMPROBANTE DE INCAPACIDAD — COPIA DIGITAL",
                            $"{inc.FechaInicio:dd/MM/yyyy} — {inc.FechaFin:dd/MM/yyyy}");

                        col.Item().Padding(20).Column(inner =>
                        {
                            SeccionLabel(inner, "Datos del Empleado");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(90); c.RelativeColumn(3);
                                    c.ConstantColumn(90); c.RelativeColumn(2);
                                });
                                FilaDatos(t, "NOMBRE:",
                                    $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim(),
                                    "CÉDULA:", emp.Cedula);
                                FilaDatos(t, "DEPARTAMENTO:", emp.Departamento, "PUESTO:", emp.Puesto);
                                FilaDatos(t, "TOTAL DÍAS:", $"{inc.TotalDias} día(s)",
                                    "ENTIDAD:", inc.Entidad.ToString());
                            });

                            SeccionLabel(inner, "Detalle del Pago");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(4); c.RelativeColumn(2);
                                    c.RelativeColumn(2); c.RelativeColumn(2);
                                });
                                foreach (var h in new[] { "CONCEPTO", "% APLICADO", "MONTO/DÍA", "TOTAL" })
                                    t.Cell().Background(Naranja).Padding(4)
                                        .Text(h).Bold().FontSize(8).FontColor(Blanco);

                                var concepto = inc.ResponsablePago switch
                                {
                                    ResponsablePago.Patrono => "Pago Patrono (primeros 3 días)",
                                    ResponsablePago.CCSS => "Pago CCSS",
                                    ResponsablePago.INS => "Pago INS",
                                    _ => "Pago Incapacidad"
                                };
                                t.Cell().Background(GrisFondo).Padding(3).Text(concepto).FontSize(8);
                                t.Cell().Padding(3).AlignCenter().Text($"{inc.PorcentajePago:N0}%").FontSize(8);
                                t.Cell().Padding(3).AlignRight().Text($"₡{inc.MontoPorDia:N2}").FontSize(8);
                                t.Cell().Padding(3).AlignRight().Text($"₡{inc.MontoTotal:N2}").FontSize(8);

                                t.Cell().Background(NaranjaFondo).Padding(3).Text("TOTAL").Bold().FontSize(8);
                                t.Cell().Background(NaranjaFondo).Padding(3).Text("").FontSize(8);
                                t.Cell().Background(NaranjaFondo).Padding(3).Text("").FontSize(8);
                                t.Cell().Background(NaranjaFondo).Padding(3).AlignRight()
                                    .Text($"₡{inc.MontoTotal:N2}").Bold().FontSize(8).FontColor(NaranjaOsc);
                            });

                            if (!string.IsNullOrEmpty(inc.Observaciones))
                            {
                                SeccionLabel(inner, "Observaciones");
                                inner.Item().Background(GrisFondo).Padding(8)
                                    .Text(inc.Observaciones).FontSize(9).Italic()
                                    .FontColor(Color.FromHex("444444"));
                            }

                            inner.Item().PaddingTop(10).Background(Oscuro).Padding(12).Row(row =>
                            {
                                row.RelativeItem().Text("MONTO TOTAL INCAPACIDAD")
                                    .Bold().FontSize(14).FontColor(Blanco);
                                row.ConstantItem(180).AlignRight()
                                    .Text($"₡{inc.MontoTotal:N2}")
                                    .Bold().FontSize(18).FontColor(Naranja);
                            });

                            PiePaginaDigital(inner, usuario);
                        });
                    });
                });
            }).GeneratePdf();
        }

        // AGUINALDO SIN FIRMAS
        public static byte[] GenerarPDFAguinaldoSinFirmas(this ComprobantePlanillaService service, Aguinaldo ag, string usuario = "")
        {
            var logo = ObtenerLogoBytes(service);
            var emp = ag.Empleado;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        EncabezadoDigital(col, logo, "COMPROBANTE DE AGUINALDO — COPIA DIGITAL",
                            $"Período: {ag.FechaInicio:dd/MM/yyyy} — {ag.FechaFin:dd/MM/yyyy}");

                        col.Item().Padding(20).Column(inner =>
                        {
                            SeccionLabel(inner, "Datos del Empleado");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(90); c.RelativeColumn(3);
                                    c.ConstantColumn(90); c.RelativeColumn(2);
                                });
                                FilaDatos(t, "NOMBRE:",
                                    $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim(),
                                    "CÉDULA:", emp.Cedula);
                                FilaDatos(t, "DEPARTAMENTO:", emp.Departamento, "PUESTO:", emp.Puesto);
                                FilaDatos(t, "AÑO:", ag.Anio.ToString(),
                                    "FECHA PAGO:", ag.FechaPago.ToString("dd/MM/yyyy"));
                            });

                            SeccionLabel(inner, "Detalle");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c => { c.RelativeColumn(6); c.RelativeColumn(4); });
                                t.Cell().Background(Naranja).Padding(4)
                                    .Text("CONCEPTO").Bold().FontSize(8).FontColor(Blanco);
                                t.Cell().Background(Naranja).Padding(4).AlignRight()
                                    .Text("MONTO").Bold().FontSize(8).FontColor(Blanco);

                                t.Cell().Background(GrisFondo).Padding(3)
                                    .Text($"Aguinaldo {ag.Anio}").FontSize(8);
                                t.Cell().Padding(3).AlignRight()
                                    .Text($"₡{ag.MontoTotal:N2}").FontSize(8);

                                t.Cell().Background(NaranjaFondo).Padding(3)
                                    .Text("TOTAL").Bold().FontSize(8);
                                t.Cell().Background(NaranjaFondo).Padding(3).AlignRight()
                                    .Text($"₡{ag.MontoTotal:N2}").Bold().FontSize(8).FontColor(NaranjaOsc);
                            });

                            if (!string.IsNullOrEmpty(ag.Observaciones))
                            {
                                SeccionLabel(inner, "Observaciones");
                                inner.Item().Background(GrisFondo).Padding(8)
                                    .Text(ag.Observaciones).FontSize(9).Italic()
                                    .FontColor(Color.FromHex("444444"));
                            }

                            inner.Item().PaddingTop(10).Background(Oscuro).Padding(12).Row(row =>
                            {
                                row.RelativeItem().Text("MONTO A PAGAR")
                                    .Bold().FontSize(14).FontColor(Blanco);
                                row.ConstantItem(180).AlignRight()
                                    .Text($"₡{ag.MontoTotal:N2}")
                                    .Bold().FontSize(18).FontColor(Naranja);
                            });

                            PiePaginaDigital(inner, usuario);
                        });
                    });
                });
            }).GeneratePdf();
        }

        // COMISIÓN SIN FIRMAS
        public static byte[] GenerarPDFComisionSinFirmas(this ComprobantePlanillaService service, Comision comision, string usuario = "")
        {
            var logo = ObtenerLogoBytes(service);
            var emp = comision.Empleado;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        EncabezadoDigital(col, logo, "COMPROBANTE DE COMISIÓN — COPIA DIGITAL",
                            $"N.° {comision.ComisionId:D6}");

                        col.Item().Padding(20).Column(inner =>
                        {
                            SeccionLabel(inner, "Datos del Empleado");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(90); c.RelativeColumn(3);
                                    c.ConstantColumn(90); c.RelativeColumn(2);
                                });
                                FilaDatos(t, "DEPARTAMENTO:", emp.Departamento,
                                    "PUESTO:", emp.Puesto);
                                FilaDatos(t, "TIPO DE PAGO:", emp.DescripcionTipoPago,
                                    "PERÍODO:", comision.PeriodoPago?.Descripcion ?? "—");
                            });

                            SeccionLabel(inner, "Detalle de la Comisión");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(90); c.RelativeColumn(3);
                                    c.ConstantColumn(90); c.RelativeColumn(2);
                                });
                                FilaDatos(t, "FECHA:", $"{comision.Fecha:dd/MM/yyyy}",
                                    "PERÍODO:", comision.PeriodoPago?.Descripcion ?? "—");
                                t.Cell().Background(GrisFondo).Padding(4)
                                    .Text("DESCRIPCIÓN:").Bold().FontSize(9);
                                t.Cell().ColumnSpan(3).Padding(4)
                                    .Text(comision.Descripcion).FontSize(9);
                            });

                            inner.Item().PaddingTop(10).Background(Oscuro).Padding(12).Row(row =>
                            {
                                row.RelativeItem().Text("MONTO A PAGAR")
                                    .Bold().FontSize(14).FontColor(Blanco);
                                row.ConstantItem(180).AlignRight()
                                    .Text($"₡{comision.Monto:N2}")
                                    .Bold().FontSize(18).FontColor(Naranja);
                            });

                            PiePaginaDigital(inner, usuario);
                        });
                    });
                });
            }).GeneratePdf();
        }

        // BOLETA VACACIONES SIN FIRMAS
        public static byte[] GenerarBoletaVacacionesSinFirmas(this ComprobantePlanillaService service,
            Vacacion vacacion, decimal diasBase, decimal diasTomados, decimal disponibles, string emisor = "", string usuario = "")
        {
            var logo = ObtenerLogoBytes(service);
            var emp = vacacion.Empleado;
            var antiguedad = Math.Round((DateTime.Today - emp.FechaIngreso).TotalDays / 365, 1);
            var saldoTras = disponibles - vacacion.DiasHabiles;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        EncabezadoDigital(col, logo, "BOLETA DE VACACIONES — COPIA DIGITAL",
                            $"N.° {vacacion.VacacionId:D6}");

                        col.Item().Padding(20).Column(inner =>
                        {
                            SeccionLabel(inner, "Datos del Empleado");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(100); c.RelativeColumn(3);
                                    c.ConstantColumn(100); c.RelativeColumn(2);
                                });
                                FilaDatos(t, "NOMBRE:",
                                    $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim(),
                                    "CÉDULA:", emp.Cedula);
                                FilaDatos(t, "DEPARTAMENTO:", emp.Departamento, "PUESTO:", emp.Puesto);
                                FilaDatos(t, "FECHA INGRESO:",
                                    $"{emp.FechaIngreso:dd/MM/yyyy} ({antiguedad} año(s))",
                                    "SALARIO DIARIO:", $"₡{vacacion.SalarioDiario:N2}");
                            });

                            SeccionLabel(inner, "Período de Vacaciones");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(100); c.RelativeColumn(2);
                                    c.ConstantColumn(100); c.RelativeColumn(2);
                                });
                                FilaDatos(t, "FECHA INICIO:", $"{vacacion.FechaInicio:dd/MM/yyyy}",
                                    "FECHA FIN:", $"{vacacion.FechaFin:dd/MM/yyyy}");
                                FilaDatos(t, "DÍAS SOLICITADOS:", $"{vacacion.DiasHabiles:N1} día(s)",
                                    "TIPO:", "Con Pago");
                            });

                            SeccionLabel(inner, "Resumen de Días (Art. 153 Código de Trabajo CR)");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(); c.RelativeColumn();
                                    c.RelativeColumn(); c.RelativeColumn();
                                });
                                foreach (var (h, bg) in new[]
                                {
                                    ("ACUMULADOS POR LEY",  GrisFondo),
                                    ("TOMADOS ANTERIORES",  RojoFondo),
                                    ("DÍAS SOLICITADOS",    Color.FromHex("FFF8E1")),
                                    ("SALDO RESTANTE",      VerdeFondo),
                                })
                                    t.Cell().Background(bg).Padding(5).AlignCenter()
                                        .Text(h).Bold().FontSize(8);

                                t.Cell().Background(GrisFondo).Padding(10).AlignCenter()
                                    .Text($"{diasBase}").FontSize(18).Bold();
                                t.Cell().Background(RojoFondo).Padding(10).AlignCenter()
                                    .Text($"{diasTomados}").FontSize(18).Bold().FontColor(Rojo);
                                t.Cell().Background(Color.FromHex("FFF8E1")).Padding(10).AlignCenter()
                                    .Text($"{vacacion.DiasHabiles}").FontSize(18).Bold()
                                    .FontColor(Color.FromHex("E65100"));
                                t.Cell().Background(VerdeFondo).Padding(10).AlignCenter()
                                    .Text($"{saldoTras:N1}").FontSize(18).Bold().FontColor(Verde);
                            });

                            SeccionLabel(inner, "Observaciones");
                            inner.Item().Background(GrisFondo).Padding(8)
                                .Text(string.IsNullOrWhiteSpace(vacacion.Observaciones)
                                    ? "Sin observaciones." : vacacion.Observaciones)
                                .FontSize(9).Italic().FontColor(Color.FromHex("444444"));

                            PiePaginaDigital(inner, usuario, "Art. 153-161 Código de Trabajo CR");
                        });
                    });
                });
            }).GeneratePdf();
        }

        // PRÉSTAMO SIN FIRMAS
        public static byte[] GenerarFiniquitoPrestamoSinFirmas(this ComprobantePlanillaService service, Prestamo prestamo, string usuario = "")
        {
            var logo = ObtenerLogoBytes(service);
            var emp = prestamo.Empleado;
            var abonos = prestamo.AbonosPrestamo.OrderBy(a => a.FechaAbono).ToList();

            var montoOriginal = prestamo.MontoOriginal > 0
                ? prestamo.MontoOriginal
                : prestamo.CuotaMensual * prestamo.Cuotas;

            var totalPagado = abonos.Sum(a => a.Monto);
            var saldoPendiente = Math.Max(0, Math.Round(montoOriginal - totalPagado, 2));
            var esSaldado = !prestamo.Activo || saldoPendiente == 0;
            var fechaFin = abonos.Any() ? abonos.Last().FechaAbono : DateTime.Now;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        EncabezadoDigital(col, logo,
                            esSaldado ? "FINIQUITO DE PRÉSTAMO — COPIA DIGITAL" : "ESTADO DE PRÉSTAMO — COPIA DIGITAL",
                            $"N.° {prestamo.PrestamoId:D6}");

                        col.Item().Padding(20).Column(inner =>
                        {
                            SeccionLabel(inner, "Datos del Empleado");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(100); c.RelativeColumn(3);
                                    c.ConstantColumn(100); c.RelativeColumn(2);
                                });
                                FilaDatos(t, "NOMBRE:",
                                    $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim(),
                                    "CÉDULA:", emp.Cedula);
                                FilaDatos(t, "DEPARTAMENTO:", emp.Departamento, "PUESTO:", emp.Puesto);
                            });

                            SeccionLabel(inner, "Resumen del Préstamo");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(120); c.RelativeColumn(2);
                                    c.ConstantColumn(120); c.RelativeColumn(2);
                                });
                                FilaDatos(t, "MONTO ORIGINAL:", $"₡{montoOriginal:N2}",
                                    "CUOTA QUINCENAL:", $"₡{prestamo.CuotaMensual:N2}");
                                FilaDatos(t, "FECHA OTORGADO:", $"{prestamo.FechaPrestamo:dd/MM/yyyy}",
                                    "CUOTAS PACTADAS:", $"{prestamo.Cuotas}");
                                FilaDatos(t, "TOTAL PAGADO:", $"₡{totalPagado:N2}",
                                    "ABONOS REALIZADOS:", $"{abonos.Count}");
                                FilaDatos(t, "SALDO PENDIENTE:", $"₡{saldoPendiente:N2}",
                                    "ESTADO:", esSaldado ? "✓ SALDADO" : "EN CURSO");
                            });

                            if (esSaldado)
                            {
                                inner.Item().PaddingTop(8).Background(VerdeFondo).Padding(12).Row(r =>
                                {
                                    r.ConstantItem(4).Background(Verde).Text("");
                                    r.RelativeItem().PaddingLeft(8)
                                        .Text($"Préstamo completamente saldado el {fechaFin:dd/MM/yyyy}")
                                        .Bold().FontSize(10).FontColor(Verde);
                                });
                            }
                            else
                            {
                                inner.Item().PaddingTop(8).Background(Color.FromHex("FFF3E0")).Padding(12).Row(r =>
                                {
                                    r.ConstantItem(4).Background(Color.FromHex("C55000")).Text("");
                                    r.RelativeItem().PaddingLeft(8)
                                        .Text($"Saldo pendiente: ₡{saldoPendiente:N2}")
                                        .Bold().FontSize(10).FontColor(Color.FromHex("C55000"));
                                });
                            }

                            inner.Item().PaddingTop(10).Background(Oscuro).Padding(12).Row(row =>
                            {
                                row.RelativeItem().Text(esSaldado ? "MONTO SALDADO" : "SALDO PENDIENTE")
                                    .Bold().FontSize(14).FontColor(Blanco);
                                row.ConstantItem(180).AlignRight()
                                    .Text($"₡{(esSaldado ? totalPagado : saldoPendiente):N2}")
                                    .Bold().FontSize(18).FontColor(Naranja);
                            });

                            PiePaginaDigital(inner, usuario);
                        });
                    });
                });
            }).GeneratePdf();
        }

        // HELPERS PRIVADOS

        private static byte[]? ObtenerLogoBytes(ComprobantePlanillaService service)
        {
            var tipo = service.GetType();
            var campo = tipo.GetField("_env", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var env = (IWebHostEnvironment?)campo?.GetValue(service);
            if (env == null) return null;

            var logoPath = Path.Combine(env.WebRootPath, "images", "logo-el-pana.jpg");
            try { return File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null; }
            catch { return null; }
        }

        private static void EncabezadoDigital(ColumnDescriptor col, byte[]? logo, string titulo, string subtitulo)
        {
            col.Item().Background(Oscuro).Padding(0).Row(row =>
            {
                row.ConstantItem(8).Background(Naranja).Text("");
                row.RelativeItem().Padding(14).Row(inner =>
                {
                    if (logo != null)
                        inner.ConstantItem(64).AlignMiddle().Image(logo).FitArea();
                    else
                        inner.ConstantItem(64).Text("");

                    inner.RelativeItem().PaddingLeft(14).AlignMiddle().Column(c =>
                    {
                        c.Item().Text("FERRETERÍA EL PANA SRL")
                            .Bold().FontSize(15).FontColor(Naranja);
                        c.Item().Text("DEPARTAMENTO DE RECURSOS HUMANOS")
                            .FontSize(8).FontColor(GrisTexto);
                        c.Item().PaddingTop(4).Text(titulo)
                            .Bold().FontSize(12).FontColor(Blanco);
                        if (!string.IsNullOrEmpty(subtitulo))
                            c.Item().Text(subtitulo).FontSize(9).FontColor(GrisTexto);
                    });
                });
            });
            col.Item().Height(3).Background(Naranja);
        }

        private static void SeccionLabel(ColumnDescriptor col, string texto)
        {
            col.Item().PaddingTop(10).PaddingBottom(3).Row(row =>
            {
                row.ConstantItem(3).Background(Naranja).Text("");
                row.RelativeItem().PaddingLeft(6)
                    .Text(texto.ToUpper())
                    .Bold().FontSize(8).FontColor(Color.FromHex("555555"))
                    .LetterSpacing(0.5f);
            });
        }

        private static void FilaDatos(TableDescriptor t, string l1, string v1, string l2, string v2)
        {
            t.Cell().Background(GrisFondo).Padding(4).Text(l1).Bold().FontSize(9);
            t.Cell().Padding(4).Text(v1).FontSize(9);
            t.Cell().Background(GrisFondo).Padding(4).Text(l2).Bold().FontSize(9);
            t.Cell().Padding(4).Text(v2).FontSize(9);
        }

        private static void PiePaginaDigital(ColumnDescriptor col, string usuario = "", string extra = "")
        {
            col.Item().PaddingTop(20).Height(1).Background(GrisClaro);
            col.Item().PaddingTop(6).Row(row =>
            {
                var textoUsuario = string.IsNullOrEmpty(usuario)
                    ? ""
                    : $"Generado por: {usuario}  ·  ";
                row.RelativeItem().Text(
                    $"Ferretería El Pana SRL  ·  Cédula Jurídica: 3-102-745359  ·  " +
                    textoUsuario +
                    $"{DateTime.Now:dd/MM/yyyy HH:mm}  ·  " +
                    $"Copia Digital - Válida sin firma" +
                    (string.IsNullOrEmpty(extra) ? "" : $"  ·  {extra}"))
                    .FontSize(7).FontColor(GrisTexto).Italic();
            });
        }
    }
}
