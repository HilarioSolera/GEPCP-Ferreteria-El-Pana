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

        // ── PALETA GLOBAL ─────────────────────────────────────────────────────
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

        // ── HELPERS ───────────────────────────────────────────────────────────
        private string LogoPath =>
            Path.Combine(_env.WebRootPath, "images", "logo-el-pana.jpg");

        private byte[]? ObtenerLogoBytes()
        {
            try { return File.Exists(LogoPath) ? File.ReadAllBytes(LogoPath) : null; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo leer el logo desde {Path}", LogoPath);
                return null;
            }
        }

        // Encabezado reutilizable
        private void Encabezado(ColumnDescriptor col, byte[]? logo,
            string titulo, string subtitulo, string? numerDoc = null)
        {
            col.Item().Background(Oscuro).Padding(0).Row(row =>
            {
                // Franja naranja izquierda
                row.ConstantItem(8).Background(Naranja).Text("");

                row.RelativeItem().Padding(14).Row(inner =>
                {
                    if (logo != null)
                        inner.ConstantItem(64).AlignMiddle()
                             .Image(logo).FitArea();
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

                    if (!string.IsNullOrEmpty(numerDoc))
                        inner.ConstantItem(80).AlignMiddle().AlignRight().Column(c =>
                        {
                            c.Item().Background(Naranja).Padding(6)
                                .AlignCenter().Text(numerDoc)
                                .Bold().FontSize(8).FontColor(Blanco);
                        });
                });
            });
            col.Item().Height(3).Background(Naranja);
        }

        // Sección label
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

        // Fila de datos 4 col
        private static void FilaDatos(TableDescriptor t,
            string l1, string v1, string l2, string v2)
        {
            t.Cell().Background(GrisFondo).Padding(4).Text(l1).Bold().FontSize(9);
            t.Cell().Padding(4).Text(v1).FontSize(9);
            t.Cell().Background(GrisFondo).Padding(4).Text(l2).Bold().FontSize(9);
            t.Cell().Padding(4).Text(v2).FontSize(9);
        }

        // Pie de página reutilizable
        private static void PiePagina(ColumnDescriptor col, string extra = "")
        {
            col.Item().PaddingTop(20).Height(1).Background(GrisClaro);
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(
                    $"Ferretería El Pana SRL  ·  Cédula Jurídica: 3-102-745359  ·  " +
                    $"Sistema GEPCP  ·  Generado: {DateTime.Now:dd/MM/yyyy HH:mm}" +
                    (string.IsNullOrEmpty(extra) ? "" : $"  ·  {extra}"))
                    .FontSize(7).FontColor(GrisTexto).Italic();
            });
        }

        // Bloque de firmas reutilizable
        private static void Firmas(ColumnDescriptor col,
            string nombreEmp, string cedula, string autorizado = "Recursos Humanos / Jefatura")
        {
            col.Item().PaddingTop(28).Table(t =>
            {
                t.ColumnsDefinition(c => { c.RelativeColumn(5); c.RelativeColumn(1); c.RelativeColumn(4); });

                t.Cell().Padding(4).Column(c =>
                {
                    c.Item().Text("RECIBO CONFORME").Bold().FontSize(9).FontColor(GrisTexto);
                    c.Item().PaddingTop(36).BorderBottom(0.5f).BorderColor(Color.FromHex("AAAAAA")).Text("");
                    c.Item().PaddingTop(4).AlignCenter().Text(nombreEmp).FontSize(8).Italic();
                    c.Item().AlignCenter().Text(cedula).FontSize(7).FontColor(GrisTexto);
                });
                t.Cell().Text("");
                t.Cell().Padding(4).Column(c =>
                {
                    c.Item().Text("AUTORIZADO POR").Bold().FontSize(9).FontColor(GrisTexto);
                    c.Item().PaddingTop(36).BorderBottom(0.5f).BorderColor(Color.FromHex("AAAAAA")).Text("");
                    c.Item().PaddingTop(4).AlignCenter().Text(autorizado).FontSize(8).Italic();
                    c.Item().AlignCenter().Text("Ferretería El Pana SRL").FontSize(7).FontColor(GrisTexto);
                });
            });
        }

        // ── COMPROBANTE PLANILLA ──────────────────────────────────────────────
        public byte[] GenerarPDF(PlanillaEmpleado planilla)
        {
            var logo = ObtenerLogoBytes();
            var emp = planilla.Empleado;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        Encabezado(col, logo, "COMPROBANTE DE PAGO",
                            $"Período: {planilla.PeriodoPago.FechaInicio:dd/MM/yyyy} — {planilla.PeriodoPago.FechaFin:dd/MM/yyyy}");

                        col.Item().Padding(20).Column(inner =>
                        {
                            // Datos empleado
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
                            });

                            // Devengados
                            SeccionLabel(inner, "Total Devengado");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(4); c.RelativeColumn(2);
                                    c.RelativeColumn(2); c.RelativeColumn(2);
                                });
                                // Header
                                foreach (var h in new[] { "CONCEPTO", "VALOR HORA", "HORAS", "MONTO" })
                                    t.Cell().Background(Naranja).Padding(4).AlignCenter()
                                        .Text(h).Bold().FontSize(8).FontColor(Blanco);

                                void FilaD(string c, string vh, string h, string m, bool bold = false)
                                {
                                    t.Cell().Background(GrisFondo).Padding(3).Text(c).FontSize(8);
                                    t.Cell().Padding(3).AlignRight().Text(vh).FontSize(8);
                                    t.Cell().Padding(3).AlignRight().Text(h).FontSize(8);
                                    if (bold) t.Cell().Background(NaranjaFondo).Padding(3).AlignRight()
                                        .Text(m).Bold().FontSize(8).FontColor(NaranjaOsc);
                                    else t.Cell().Padding(3).AlignRight().Text(m).FontSize(8);
                                }

                                FilaD("Salario Base", "", "", $"₡{emp.SalarioBase:N2}");
                                FilaD("Jornada Ordinaria Diurna",
                                    $"₡{planilla.ValorHora:N2}", $"{planilla.HorasOrdinarias:N2}",
                                    $"₡{planilla.SalarioOrdinario:N2}");
                                FilaD("Horas Extraordinarias",
                                    planilla.HorasExtras > 0 ? $"₡{planilla.ValorHoraExtra:N2}" : "—",
                                    $"{planilla.HorasExtras:N2}",
                                    $"₡{planilla.MontoHorasExtras:N2}");
                                if (planilla.AumentoAplicado > 0)
                                    FilaD("Comisión / Aumento", "", "", $"₡{planilla.AumentoAplicado:N2}");
                                if (planilla.MontoFeriados > 0)
                                    FilaD("Feriados", "", "", $"₡{planilla.MontoFeriados:N2}");
                                FilaD("TOTAL BRUTO DEVENGADO", "", "", $"₡{planilla.TotalDevengado:N2}", true);
                            });

                            // Deducciones
                            SeccionLabel(inner, "Deducciones");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                { c.RelativeColumn(6); c.RelativeColumn(2); c.RelativeColumn(2); });

                                void FilaDed(string c, string pct, string m, bool total = false)
                                {
                                    var bg = total ? RojoFondo : Blanco;
                                    if (total)
                                    {
                                        t.Cell().Background(bg).Padding(3).Text(c).FontSize(8).Bold();
                                        t.Cell().Background(bg).Padding(3).AlignCenter().Text(pct).FontSize(8);
                                        t.Cell().Background(bg).Padding(3).AlignRight()
                                            .Text(m).FontSize(8).Bold().FontColor(Rojo);
                                    }
                                    else
                                    {
                                        t.Cell().Background(bg).Padding(3).Text(c).FontSize(8);
                                        t.Cell().Background(bg).Padding(3).AlignCenter().Text(pct).FontSize(8);
                                        t.Cell().Background(bg).Padding(3).AlignRight()
                                            .Text(m).FontSize(8).FontColor(Color.FromHex("222222"));
                                    }
                                }

                                // CCSS desglosada (fallback a valores estándar si el período no los tiene)
                                var totalBruto = planilla.TotalDevengado;
                                var per = planilla.PeriodoPago;
                                var factorMes = per.TipoPeriodo switch
                                {
                                    TipoPeriodo.Semanal => 52m / 12m,
                                    TipoPeriodo.Mensual => 1m,
                                    _ => 2m
                                };
                                var brutoMensual = totalBruto * factorMes;
                                var pctSEM = per.PorcentajeSEM > 0 ? per.PorcentajeSEM : 5.50m;
                                var pctIVM = per.PorcentajeIVM > 0 ? per.PorcentajeIVM : 4.33m;
                                var pctBP  = per.PorcentajeBP  > 0 ? per.PorcentajeBP  : 1.00m;
                                var sem = Math.Round(totalBruto * (pctSEM / 100m), 2);
                                var ivm = Math.Round(totalBruto * (pctIVM / 100m), 2);
                                var bp = Math.Round(totalBruto * (pctBP / 100m), 2);

                                // Header deducciones
                                foreach (var h in new[] { "CONCEPTO", "%", "MONTO" })
                                    t.Cell().Background(Naranja).Padding(4)
                                        .Text(h).Bold().FontSize(8).FontColor(Blanco);

                                // Salario bruto mensual (referencia)
                                t.Cell().ColumnSpan(3).Padding(3)
                                    .Text($"Salario Bruto Mensual (referencia): ₡{brutoMensual:N2}")
                                    .Bold().FontSize(8).FontColor(Color.FromHex("333333"));

                                // Sub-header Cargas Sociales
                                t.Cell().ColumnSpan(3).Background(GrisFondo).Padding(3)
                                    .Text($"CARGAS SOCIALES (CCSS) — {planilla.PorcentajeCCSS:N2}%")
                                    .Bold().FontSize(7.5f).FontColor(Color.FromHex("555555"));

                                FilaDed("   Seguro Enfermedad y Maternidad (SEM)", $"{pctSEM:N2}%",
                                    $"₡{sem:N2}");
                                FilaDed("   Invalidez, Vejez y Muerte (IVM)", $"{pctIVM:N2}%",
                                    $"₡{ivm:N2}");
                                FilaDed("   Banco Popular (Aporte trabajador)", $"{pctBP:N2}%",
                                    $"₡{bp:N2}");
                                FilaDed("Subtotal CCSS",
                                    $"{planilla.PorcentajeCCSS:N2}%",
                                    $"₡{planilla.DeduccionCCSS:N2}");

                                // ISR (Impuesto sobre la Renta)
                                if (planilla.DeduccionRenta > 0)
                                {
                                    var baseImponibleMensual = brutoMensual;
                                    // Fallback ISR tramos (protección contra valores en 0)
                                    var t2D = per.ISR_Tramo2_Desde > 0 ? per.ISR_Tramo2_Desde : 918000m;
                                    var t2H = per.ISR_Tramo2_Hasta > 0 ? per.ISR_Tramo2_Hasta : 1347000m;
                                    var t2P = per.ISR_Tramo2_Porcentaje > 0 ? per.ISR_Tramo2_Porcentaje : 10m;
                                    var t3D = per.ISR_Tramo3_Desde > 0 ? per.ISR_Tramo3_Desde : 1347000m;
                                    var t3H = per.ISR_Tramo3_Hasta > 0 ? per.ISR_Tramo3_Hasta : 2364000m;
                                    var t3P = per.ISR_Tramo3_Porcentaje > 0 ? per.ISR_Tramo3_Porcentaje : 15m;
                                    var t4D = per.ISR_Tramo4_Desde > 0 ? per.ISR_Tramo4_Desde : 2364000m;
                                    var t4H = per.ISR_Tramo4_Hasta > 0 ? per.ISR_Tramo4_Hasta : 4727000m;
                                    var t4P = per.ISR_Tramo4_Porcentaje > 0 ? per.ISR_Tramo4_Porcentaje : 20m;
                                    var t5D = per.ISR_Tramo5_Desde > 0 ? per.ISR_Tramo5_Desde : 4727000m;
                                    var t5P = per.ISR_Tramo5_Porcentaje > 0 ? per.ISR_Tramo5_Porcentaje : 25m;

                                    t.Cell().ColumnSpan(3).Background(GrisFondo).Padding(3)
                                        .Text($"IMPUESTO SOBRE LA RENTA (ISR) — Base mensual: ₡{baseImponibleMensual:N2}")
                                        .Bold().FontSize(7.5f).FontColor(Color.FromHex("555555"));

                                    if (baseImponibleMensual > t2D)
                                        FilaDed($"   Excedente ₡{t2D:N0} – ₡{t2H:N0}",
                                            $"{t2P:N0}%",
                                            $"₡{Math.Round(Math.Min(Math.Max(baseImponibleMensual - t2D, 0), t2H - t2D) * (t2P / 100m) / factorMes, 2):N2}");
                                    if (baseImponibleMensual > t3D)
                                        FilaDed($"   Excedente ₡{t3D:N0} – ₡{t3H:N0}",
                                            $"{t3P:N0}%",
                                            $"₡{Math.Round(Math.Min(Math.Max(baseImponibleMensual - t3D, 0), t3H - t3D) * (t3P / 100m) / factorMes, 2):N2}");
                                    if (baseImponibleMensual > t4D)
                                        FilaDed($"   Excedente ₡{t4D:N0} – ₡{t4H:N0}",
                                            $"{t4P:N0}%",
                                            $"₡{Math.Round(Math.Min(Math.Max(baseImponibleMensual - t4D, 0), t4H - t4D) * (t4P / 100m) / factorMes, 2):N2}");
                                    if (baseImponibleMensual > t5D)
                                        FilaDed($"   Excedente sobre ₡{t5D:N0}",
                                            $"{t5P:N0}%",
                                            $"₡{Math.Round((baseImponibleMensual - t5D) * (t5P / 100m) / factorMes, 2):N2}");

                                    // Créditos fiscales
                                    var credHijos = emp.NumHijos * per.ISR_CreditoHijo;
                                    var credConyuge = emp.TieneConyuge ? per.ISR_CreditoConyuge : 0m;
                                    var totalCreditos = credHijos + credConyuge;
                                    if (totalCreditos > 0)
                                        FilaDed($"   Créditos fiscales ({emp.NumHijos} hijo(s), {(emp.TieneConyuge ? "cónyuge" : "sin cónyuge")})",
                                            "", $"-₡{Math.Round(totalCreditos / factorMes, 2):N2}");

                                    FilaDed("Subtotal ISR (retención por período)", "",
                                        $"₡{planilla.DeduccionRenta:N2}");
                                }
                                else
                                {
                                    FilaDed("Impuesto sobre la Renta (ISR)", "Exento",
                                        $"₡0.00");
                                }

                                // Otras deducciones
                                if (planilla.DeduccionCreditoFerreteria > 0)
                                    FilaDed("Crédito Ferretería", "",
                                        $"₡{planilla.DeduccionCreditoFerreteria:N2}");
                                if (planilla.DeduccionPrestamos > 0)
                                    FilaDed("Préstamo Personal", "",
                                        $"₡{planilla.DeduccionPrestamos:N2}");
                                if (planilla.DeduccionHorasNoLaboradas > 0)
                                    FilaDed("Horas No Laboradas", "",
                                        $"₡{planilla.DeduccionHorasNoLaboradas:N2}");
                                if (planilla.DeduccionIncapacidad > 0)
                                    FilaDed("Incapacidad", "",
                                        $"₡{planilla.DeduccionIncapacidad:N2}");
                                if (planilla.DeduccionVacaciones > 0)
                                    FilaDed("Vacaciones Sin Pago", "",
                                        $"₡{planilla.DeduccionVacaciones:N2}");
                                if (planilla.OtrasDeducciones > 0)
                                {
                                    var labelOtras = string.IsNullOrWhiteSpace(planilla.DescripcionOtrasDeducciones)
                                        ? "Otras Deducciones"
                                        : planilla.DescripcionOtrasDeducciones;
                                    FilaDed(labelOtras, "", $"₡{planilla.OtrasDeducciones:N2}");
                                }

                                // Total
                                FilaDed("TOTAL DEDUCCIONES", "",
                                    $"₡{planilla.TotalDeducciones:N2}", true);
                            });

                            // Neto
                            inner.Item().PaddingTop(10).Background(Oscuro).Padding(12).Row(row =>
                            {
                                row.RelativeItem().Text("NETO A PAGAR")
                                    .Bold().FontSize(14).FontColor(Blanco);
                                row.ConstantItem(180).AlignRight()
                                    .Text($"₡{planilla.NetoAPagar:N2}")
                                    .Bold().FontSize(18).FontColor(Naranja);
                            });

                            Firmas(inner,
                                $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim(),
                                emp.Cedula);
                            PiePagina(inner);
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ── HORAS EXTRAS ─────────────────────────────────────────────────────
        public byte[] GenerarPDFHorasExtras(HorasExtras hx)
        {
            var logo = ObtenerLogoBytes();
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
                        Encabezado(col, logo, "COMPROBANTE DE HORAS EXTRAS",
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

                                t.Cell().Background(NaranjaFondo).Padding(3)
                                    .Text("TOTAL").Bold().FontSize(8);
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

                            Firmas(inner,
                                $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim(),
                                emp.Cedula);
                            PiePagina(inner);
                        });
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GenerarPDFHorasExtrasSinFirmas(HorasExtras hx)
        {
            var logo = ObtenerLogoBytes();
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
                        Encabezado(col, logo, "COMPROBANTE DE HORAS EXTRAS — COPIA DIGITAL",
                            $"Período: {hx.PeriodoPago.Descripcion}");

                        col.Item().Padding(20).Column(inner =>
                        {
                            inner.Item().Background(Color.FromHex("E3F2FD")).Padding(8).Row(r =>
                            {
                                r.ConstantItem(4).Background(Color.FromHex("1565C0")).Text("");
                                r.RelativeItem().PaddingLeft(8)
                                    .Text("Documento generado digitalmente por el Sistema GEPCP. No requiere firma física.")
                                    .FontSize(8).FontColor(Color.FromHex("1565C0")).Italic();
                            });

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

                                t.Cell().Background(NaranjaFondo).Padding(3)
                                    .Text("TOTAL").Bold().FontSize(8);
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

                            PiePagina(inner);
                        });
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GenerarPDFComisionSinFirmas(Comision comision)
        {
            var logo = ObtenerLogoBytes();
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
                        Encabezado(col, logo, "COMPROBANTE DE COMISIÓN — COPIA DIGITAL",
                            $"N.° {comision.ComisionId:D6}");

                        col.Item().Padding(20).Column(inner =>
                        {
                            inner.Item().Background(Color.FromHex("E3F2FD")).Padding(8).Row(r =>
                            {
                                r.ConstantItem(4).Background(Color.FromHex("1565C0")).Text("");
                                r.RelativeItem().PaddingLeft(8)
                                    .Text("Documento generado digitalmente por el Sistema GEPCP. No requiere firma física.")
                                    .FontSize(8).FontColor(Color.FromHex("1565C0")).Italic();
                            });

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

                            PiePagina(inner);
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ── INCAPACIDAD ───────────────────────────────────────────────────────
        public byte[] GenerarPDFIncapacidad(Incapacidad inc)
        {
            var logo = ObtenerLogoBytes();
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
                        Encabezado(col, logo, "COMPROBANTE DE INCAPACIDAD",
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
                                if (!string.IsNullOrEmpty(inc.TiqueteCCSS))
                                    FilaDatos(t, "TIQUETE CCSS:", inc.TiqueteCCSS, "", "");
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

                                t.Cell().Background(NaranjaFondo).Padding(3)
                                    .Text("TOTAL").Bold().FontSize(8);
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

                            Firmas(inner,
                                $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim(),
                                emp.Cedula);
                            PiePagina(inner);
                        });
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GenerarPDFIncapacidadSinFirmas(Incapacidad inc)
        {
            var logo = ObtenerLogoBytes();
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
                        Encabezado(col, logo, "COMPROBANTE DE INCAPACIDAD — COPIA DIGITAL",
                            $"{inc.FechaInicio:dd/MM/yyyy} — {inc.FechaFin:dd/MM/yyyy}");

                        col.Item().Padding(20).Column(inner =>
                        {
                            inner.Item().Background(Color.FromHex("E3F2FD")).Padding(8).Row(r =>
                            {
                                r.ConstantItem(4).Background(Color.FromHex("1565C0")).Text("");
                                r.RelativeItem().PaddingLeft(8)
                                    .Text("Documento generado digitalmente por el Sistema GEPCP. No requiere firma física.")
                                    .FontSize(8).FontColor(Color.FromHex("1565C0")).Italic();
                            });

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
                                if (!string.IsNullOrEmpty(inc.TiqueteCCSS))
                                    FilaDatos(t, "TIQUETE CCSS:", inc.TiqueteCCSS, "", "");
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

                                t.Cell().Background(NaranjaFondo).Padding(3)
                                    .Text("TOTAL").Bold().FontSize(8);
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

                            PiePagina(inner);
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ── AGUINALDO ─────────────────────────────────────────────────────────
        public byte[] GenerarPDFAguinaldo(Aguinaldo ag)
        {
            var logo = ObtenerLogoBytes();
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
                        Encabezado(col, logo, "COMPROBANTE DE AGUINALDO",
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

                            Firmas(inner,
                                $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim(),
                                emp.Cedula);
                            PiePagina(inner);
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ── AGUINALDO SIN FIRMAS (para envío por email) ────────────────────────

        public byte[] GenerarPDFAguinaldoSinFirmas(Aguinaldo ag)
        {
            var logo = ObtenerLogoBytes();
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
                        Encabezado(col, logo, "COMPROBANTE DE AGUINALDO — COPIA DIGITAL",
                            $"Período: {ag.FechaInicio:dd/MM/yyyy} — {ag.FechaFin:dd/MM/yyyy}");

                        col.Item().Padding(20).Column(inner =>
                        {
                            inner.Item().Background(Color.FromHex("E3F2FD")).Padding(8).Row(r =>
                            {
                                r.ConstantItem(4).Background(Color.FromHex("1565C0")).Text("");
                                r.RelativeItem().PaddingLeft(8)
                                    .Text("Documento generado digitalmente por el Sistema GEPCP. No requiere firma física.")
                                    .FontSize(8).FontColor(Color.FromHex("1565C0")).Italic();
                            });

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

                            PiePagina(inner);
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ── PLANILLA GENERAL ──────────────────────────────────────────────────
        public byte[] GenerarPDFPlanillaGeneral(
            List<PlanillaEmpleado> planillas, PeriodoPago periodo)
        {
            var logo = ObtenerLogoBytes();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(7.5f).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        var tituloPlanilla = periodo.TipoPeriodo switch
                        {
                            TipoPeriodo.Semanal => "CONTROL PLANILLA SEMANAL",
                            TipoPeriodo.Mensual => "CONTROL PLANILLA MENSUAL",
                            _ => "CONTROL PLANILLA QUINCENAL"
                        };
                        Encabezado(col, logo, tituloPlanilla,
                            $"Período: {periodo.FechaInicio:dd/MM/yyyy} — {periodo.FechaFin:dd/MM/yyyy}");

                        col.Item().Padding(14).Column(inner =>
                        {
                            var grupos = planillas.GroupBy(p => p.Empleado.Departamento);
                            foreach (var grupo in grupos)
                            {
                                inner.Item().PaddingTop(8).Background(Oscuro).Padding(5)
                                    .Row(r =>
                                    {
                                        r.ConstantItem(4).Background(Naranja).Text("");
                                        r.RelativeItem().PaddingLeft(6)
                                            .Text(grupo.Key.ToUpper())
                                            .Bold().FontSize(8).FontColor(Blanco);
                                    });

                                inner.Item().Table(t =>
                                {
                                    DefinirColumnasPlanilla(t);
                                    AgregarHeaderPlanilla(t, Naranja);
                                    foreach (var p in grupo) AgregarFilaPlanilla(t, p, GrisFondo);

                                    // Subtotal
                                    var sbg = Color.FromHex("FFF0DC");
                                    t.Cell().Background(sbg).Padding(3)
                                        .Text($"Subtotal — {grupo.Key}").Bold().FontSize(7);
                                    foreach (var v in new[]
                                    {
                                        grupo.Sum(p => p.SalarioOrdinario),
                                        grupo.Sum(p => p.MontoHorasExtras),
                                        grupo.Sum(p => p.AumentoAplicado),
                                        grupo.Sum(p => p.TotalDevengado),
                                        grupo.Sum(p => p.DeduccionCCSS),
                                        grupo.Sum(p => p.DeduccionPrestamos),
                                        grupo.Sum(p => p.DeduccionCreditoFerreteria),
                                        grupo.Sum(p => p.TotalDeducciones),
                                        grupo.Sum(p => p.NetoAPagar),
                                    })
                                        t.Cell().Background(sbg).Padding(3).AlignRight()
                                            .Text($"₡{v:N0}").Bold().FontSize(7);
                                });
                            }

                            // Total general
                            inner.Item().PaddingTop(6).Table(t =>
                            {
                                DefinirColumnasPlanilla(t);
                                t.Cell().Background(Naranja).Padding(4)
                                    .Text("TOTAL GENERAL").Bold().FontSize(8).FontColor(Blanco);
                                foreach (var v in new[]
                                {
                                    planillas.Sum(p => p.SalarioOrdinario),
                                    planillas.Sum(p => p.MontoHorasExtras),
                                    planillas.Sum(p => p.AumentoAplicado),
                                    planillas.Sum(p => p.TotalDevengado),
                                    planillas.Sum(p => p.DeduccionCCSS),
                                    planillas.Sum(p => p.DeduccionPrestamos),
                                    planillas.Sum(p => p.DeduccionCreditoFerreteria),
                                    planillas.Sum(p => p.TotalDeducciones),
                                    planillas.Sum(p => p.NetoAPagar),
                                })
                                    t.Cell().Background(Naranja).Padding(4).AlignRight()
                                        .Text($"₡{v:N0}").Bold().FontSize(8).FontColor(Blanco);
                            });

                            PiePagina(inner);
                        });
                    });
                });
            }).GeneratePdf();
        }

        private static void DefinirColumnasPlanilla(TableDescriptor t)
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2);
                c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2);
                c.RelativeColumn(2); c.RelativeColumn(2);
            });
        }

        private static void AgregarHeaderPlanilla(TableDescriptor t, Color bg)
        {
            foreach (var h in new[]
                { "Empleado","Sal. Ord.","Hrs. Ext.","Comisión",
                  "Total Dev.","CCSS","Préstamo","Cré. Ferre.","Total Ded.","Neto" })
                t.Cell().Background(bg).Padding(3).AlignCenter()
                    .Text(h).Bold().FontSize(7).FontColor(Color.FromHex("FFFFFF"));
        }

        private static void AgregarFilaPlanilla(TableDescriptor t, PlanillaEmpleado p, Color fondo)
        {
            t.Cell().Padding(2).Text($"{p.Empleado.PrimerApellido} {p.Empleado.Nombre}").FontSize(7);
            t.Cell().Padding(2).AlignRight().Text($"₡{p.SalarioOrdinario:N0}").FontSize(7);
            t.Cell().Padding(2).AlignRight()
                .Text(p.MontoHorasExtras > 0 ? $"₡{p.MontoHorasExtras:N0}" : "—").FontSize(7);
            t.Cell().Padding(2).AlignRight()
                .Text(p.AumentoAplicado > 0 ? $"₡{p.AumentoAplicado:N0}" : "—").FontSize(7);
            t.Cell().Padding(2).AlignRight().Text($"₡{p.TotalDevengado:N0}").Bold().FontSize(7);
            t.Cell().Padding(2).AlignRight().Text($"₡{p.DeduccionCCSS:N0}").FontSize(7);
            t.Cell().Padding(2).AlignRight()
                .Text(p.DeduccionPrestamos > 0 ? $"₡{p.DeduccionPrestamos:N0}" : "—").FontSize(7);
            t.Cell().Padding(2).AlignRight()
                .Text(p.DeduccionCreditoFerreteria > 0 ? $"₡{p.DeduccionCreditoFerreteria:N0}" : "—").FontSize(7);
            t.Cell().Padding(2).AlignRight().Text($"₡{p.TotalDeducciones:N0}").Bold().FontSize(7);
            t.Cell().Padding(2).AlignRight().Text($"₡{p.NetoAPagar:N0}").Bold()
                .FontSize(7).FontColor(Verde);
        }
        // ── COMISIÓN ──────────────────────────────────────────────────────────
        public byte[] GenerarPDFComision(Comision comision)
        {
            var logo = ObtenerLogoBytes();
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
                        Encabezado(col, logo, "COMPROBANTE DE COMISIÓN",
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

                            Firmas(inner,
                                $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim(),
                                emp.Cedula);
                            PiePagina(inner);
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ── BOLETA VACACIONES ─────────────────────────────────────────────────
        public byte[] GenerarBoletaVacaciones(
            Vacacion vacacion, decimal diasBase, decimal diasTomados,
            decimal disponibles, string emisor)
        {
            var logo = ObtenerLogoBytes();
            var emp = vacacion.Empleado;
            var antiguedad = Math.Round((DateTime.Today - emp.FechaIngreso).TotalDays / 365, 1);
            var saldoTras = disponibles - vacacion.DiasHabiles;
            var (colorEstado, textoEstado) = vacacion.Estado switch
            {
                EstadoVacacion.Aprobada => (Verde, "✓ APROBADA"),
                EstadoVacacion.Rechazada => (Rojo, "✗ RECHAZADA"),
                _ => (Color.FromHex("E65100"), "⏳ PENDIENTE")
            };

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        Encabezado(col, logo, "BOLETA DE VACACIONES",
                            $"N.° {vacacion.VacacionId:D6}", textoEstado);

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
                                    "TIPO:", vacacion.Tipo == TipoVacacion.ConPago ? "Con Pago" : "Sin Pago");
                            });

                            SeccionLabel(inner, "Resumen de Días por Período (Art. 153 Código de Trabajo CR)");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(); c.RelativeColumn();
                                    c.RelativeColumn(); c.RelativeColumn();
                                });
                                foreach (var (h, bg) in new[]
                                {
                                    ("DÍAS POR PERÍODO",    GrisFondo),
                                    ("DISFRUTADOS",         RojoFondo),
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

                            inner.Item().PaddingTop(8).Background(VerdeFondo).Padding(10).Row(r =>
                            {
                                r.ConstantItem(4).Background(Verde).Text("");
                                r.RelativeItem().PaddingLeft(8)
                                    .Text("Vacaciones con pago — El empleado recibirá su salario ordinario durante el período de vacaciones.")
                                    .FontColor(Verde).Bold().FontSize(9);
                            });

                            SeccionLabel(inner, "Observaciones");
                            inner.Item().Background(GrisFondo).Padding(8)
                                .Text(string.IsNullOrWhiteSpace(vacacion.Observaciones)
                                    ? "Sin observaciones." : vacacion.Observaciones)
                                .FontSize(9).Italic().FontColor(Color.FromHex("444444"));

                            Firmas(inner,
                                $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim(),
                                emp.Cedula, $"Recursos Humanos — Emitido por: {emisor}");
                            PiePagina(inner, "Art. 153-161 Código de Trabajo CR");
                        });
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GenerarBoletaVacacionesSinFirmas(
            Vacacion vacacion, decimal diasBase, decimal diasTomados,
            decimal disponibles, string emisor)
        {
            var logo = ObtenerLogoBytes();
            var emp = vacacion.Empleado;
            var antiguedad = Math.Round((DateTime.Today - emp.FechaIngreso).TotalDays / 365, 1);
            var saldoTras = disponibles - vacacion.DiasHabiles;
            var (colorEstado, textoEstado) = vacacion.Estado switch
            {
                EstadoVacacion.Aprobada => (Verde, "✓ APROBADA"),
                EstadoVacacion.Rechazada => (Rojo, "✗ RECHAZADA"),
                _ => (Color.FromHex("E65100"), "⏳ PENDIENTE")
            };

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        Encabezado(col, logo, "BOLETA DE VACACIONES — COPIA DIGITAL",
                            $"N.° {vacacion.VacacionId:D6}", textoEstado);

                        col.Item().Padding(20).Column(inner =>
                        {
                            inner.Item().Background(Color.FromHex("E3F2FD")).Padding(8).Row(r =>
                            {
                                r.ConstantItem(4).Background(Color.FromHex("1565C0")).Text("");
                                r.RelativeItem().PaddingLeft(8)
                                    .Text("Documento generado digitalmente por el Sistema GEPCP. No requiere firma física.")
                                    .FontSize(8).FontColor(Color.FromHex("1565C0")).Italic();
                            });

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
                                    "TIPO:", vacacion.Tipo == TipoVacacion.ConPago ? "Con Pago" : "Sin Pago");
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

                            if (vacacion.Tipo == TipoVacacion.SinPago && vacacion.MontoDeducido > 0)
                            {
                                SeccionLabel(inner, "Deducción en Planilla");
                                inner.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(c => { c.RelativeColumn(6); c.RelativeColumn(4); });
                                    t.Cell().Background(GrisFondo).Padding(4)
                                        .Text("CONCEPTO").Bold().FontSize(8);
                                    t.Cell().Background(GrisFondo).Padding(4).AlignRight()
                                        .Text("MONTO").Bold().FontSize(8);
                                    t.Cell().Padding(3)
                                        .Text($"Vacaciones sin pago — {vacacion.DiasHabiles} días × ₡{vacacion.SalarioDiario:N2}/día")
                                        .FontSize(8);
                                    t.Cell().Padding(3).AlignRight()
                                        .Text($"₡{vacacion.MontoDeducido:N2}").FontSize(8);
                                    t.Cell().Background(RojoFondo).Padding(4)
                                        .Text("TOTAL A DEDUCIR").Bold().FontSize(8);
                                    t.Cell().Background(RojoFondo).Padding(4).AlignRight()
                                        .Text($"₡{vacacion.MontoDeducido:N2}").Bold().FontSize(8).FontColor(Rojo);
                                });
                            }
                            else if (vacacion.Tipo == TipoVacacion.ConPago)
                            {
                                inner.Item().PaddingTop(8).Background(VerdeFondo).Padding(10).Row(r =>
                                {
                                    r.ConstantItem(4).Background(Verde).Text("");
                                    r.RelativeItem().PaddingLeft(8)
                                        .Text("Vacaciones con pago — El empleado recibirá su salario ordinario durante el período de vacaciones.")
                                        .FontColor(Verde).Bold().FontSize(9);
                                });
                            }

                            SeccionLabel(inner, "Observaciones");
                            inner.Item().Background(GrisFondo).Padding(8)
                                .Text(string.IsNullOrWhiteSpace(vacacion.Observaciones)
                                    ? "Sin observaciones." : vacacion.Observaciones)
                                .FontSize(9).Italic().FontColor(Color.FromHex("444444"));

                            PiePagina(inner, "Art. 153-161 Código de Trabajo CR");
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ── PRÉSTAMO — activo o saldado ───────────────────────────────────────
        public byte[] GenerarFiniquitoPrestamo(Prestamo prestamo)
        {
            var logo = ObtenerLogoBytes();
            var emp = prestamo.Empleado;
            var abonos = prestamo.AbonosPrestamo
                .OrderBy(a => a.FechaAbono).ToList();

            var montoOriginal = prestamo.MontoOriginal > 0
                ? prestamo.MontoOriginal
                : prestamo.CuotaMensual * prestamo.Cuotas;

            var totalPagado = abonos.Sum(a => a.Monto);
            var saldoPendiente = Math.Max(0, Math.Round(montoOriginal - totalPagado, 2));
            var esSaldado = !prestamo.Activo || saldoPendiente == 0;

            var fechaFin = abonos.Any() ? abonos.Last().FechaAbono : DateTime.Now;
            var duracion = (int)(fechaFin - prestamo.FechaPrestamo).TotalDays;

            // Cuotas estimadas restantes
            var cuotasEstimadas = prestamo.CuotaMensual > 0
                ? (int)Math.Ceiling((double)(saldoPendiente / prestamo.CuotaMensual))
                : 0;

            var (colorEstado, textoEstado, descripEstado) = esSaldado
                ? (Verde, "✓ SALDADO",
                   $"Préstamo cancelado en su totalidad el {fechaFin:dd/MM/yyyy}.")
                : (Color.FromHex("C55000"), "EN CURSO",
                   $"Saldo pendiente: ₡{saldoPendiente:N2}  ·  " +
                   $"Cuotas estimadas restantes: {cuotasEstimadas}");

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        Encabezado(col, logo,
                            esSaldado ? "FINIQUITO DE PRÉSTAMO" : "ESTADO DE PRÉSTAMO",
                            $"N.° {prestamo.PrestamoId:D6}", textoEstado);

                        col.Item().Padding(20).Column(inner =>
                        {
                            // Datos empleado
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

                            // Resumen del préstamo
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
                                    esSaldado ? "DURACIÓN:" : "CUOTAS ESTIMADAS RESTANTES:",
                                    esSaldado ? $"{duracion} días" : $"{cuotasEstimadas}");
                            });

                            // Banner de estado
                            inner.Item().PaddingTop(8).Background(
                                esSaldado ? VerdeFondo : Color.FromHex("FFF3E0"))
                                .Padding(10).Row(r =>
                                {
                                    r.ConstantItem(4).Background(colorEstado).Text("");
                                    r.RelativeItem().PaddingLeft(8)
                                        .Text(descripEstado)
                                        .Bold().FontSize(9).FontColor(colorEstado);
                                });

                            // Historial de abonos
                            if (abonos.Any())
                            {
                                SeccionLabel(inner, "Historial Completo de Abonos");
                                inner.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(c =>
                                    {
                                        c.ConstantColumn(22); c.RelativeColumn(3);
                                        c.RelativeColumn(2); c.RelativeColumn(2);
                                        c.RelativeColumn(2); c.RelativeColumn(4);
                                    });

                                    // Header
                                    foreach (var h in new[]
                                    {
                                        "#", "FECHA Y HORA", "MONTO ABONO",
                                        "SALDO ANTERIOR", "SALDO DESPUÉS", "OBSERVACIONES"
                                    })
                                        t.Cell().Background(Oscuro).Padding(4)
                                            .AlignCenter().Text(h).Bold().FontSize(7)
                                            .FontColor(Blanco);

                                    var saldoCalc = montoOriginal;
                                    int num = 1;

                                    foreach (var a in abonos)
                                    {
                                        var saldoAnt = saldoCalc;
                                        saldoCalc = Math.Round(saldoCalc - a.Monto, 2);
                                        if (saldoCalc < 0) saldoCalc = 0;
                                        var esFinal = saldoCalc == 0;
                                        var bg = num % 2 == 0
                                            ? Color.FromHex("F5F5F5")
                                            : Blanco;

                                        t.Cell().Background(bg).Padding(3).AlignCenter()
                                            .Text(num.ToString()).FontSize(7).FontColor(GrisTexto);
                                        t.Cell().Background(bg).Padding(3)
                                            .Text(a.FechaAbono.ToString("dd/MM/yyyy HH:mm")).FontSize(7);
                                        t.Cell().Background(bg).Padding(3).AlignRight()
                                            .Text($"₡{a.Monto:N2}").Bold().FontSize(7).FontColor(Verde);
                                        t.Cell().Background(bg).Padding(3).AlignRight()
                                            .Text($"₡{saldoAnt:N2}").FontSize(7).FontColor(GrisTexto);
                                        t.Cell().Background(bg).Padding(3).AlignRight()
                                            .Text(esFinal ? "₡0.00 ✓" : $"₡{saldoCalc:N2}")
                                            .FontSize(7)
                                            .FontColor(esFinal ? Verde : Color.FromHex("C55000"));
                                            
                                        t.Cell().Background(bg).Padding(3)
                                            .Text(a.Observaciones ?? "—").FontSize(6.5f)
                                            .FontColor(GrisTexto);

                                        num++;
                                    }

                                    // Fila de totales
                                    t.Cell().Background(Naranja).Padding(4).AlignCenter()
                                        .Text("∑").Bold().FontSize(9).FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4)
                                        .Text("TOTAL PAGADO").Bold().FontSize(7).FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4).AlignRight()
                                        .Text($"₡{totalPagado:N2}").Bold().FontSize(7).FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4).AlignRight()
                                        .Text($"₡{montoOriginal:N2}").FontSize(7).FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4).AlignRight()
                                        .Text($"₡{saldoPendiente:N2}").Bold().FontSize(7).FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4)
                                        .Text($"{abonos.Count} abono(s)").FontSize(7).FontColor(Blanco);
                                });
                            }

                            Firmas(inner,
                                $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim(),
                                emp.Cedula);
                            PiePagina(inner);
                        });
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GenerarFiniquitoPrestamoSinFirmas(Prestamo prestamo)
        {
            var logo = ObtenerLogoBytes();
            var emp = prestamo.Empleado;
            var abonos = prestamo.AbonosPrestamo
                .OrderBy(a => a.FechaAbono).ToList();

            var montoOriginal = prestamo.MontoOriginal > 0
                ? prestamo.MontoOriginal
                : prestamo.CuotaMensual * prestamo.Cuotas;

            var totalPagado = abonos.Sum(a => a.Monto);
            var saldoPendiente = Math.Max(0, Math.Round(montoOriginal - totalPagado, 2));
            var esSaldado = !prestamo.Activo || saldoPendiente == 0;

            var fechaFin = abonos.Any() ? abonos.Last().FechaAbono : DateTime.Now;
            var duracion = (int)(fechaFin - prestamo.FechaPrestamo).TotalDays;

            // Cuotas estimadas restantes
            var cuotasEstimadas = prestamo.CuotaMensual > 0
                ? (int)Math.Ceiling((double)(saldoPendiente / prestamo.CuotaMensual))
                : 0;

            var (colorEstado, textoEstado, descripEstado) = esSaldado
                ? (Verde, "✓ SALDADO",
                   $"Préstamo cancelado en su totalidad el {fechaFin:dd/MM/yyyy}.")
                : (Color.FromHex("C55000"), "EN CURSO",
                   $"Saldo pendiente: ₡{saldoPendiente:N2}  ·  " +
                   $"Cuotas estimadas restantes: {cuotasEstimadas}");

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        Encabezado(col, logo,
                            esSaldado ? "FINIQUITO DE PRÉSTAMO — COPIA DIGITAL" : "ESTADO DE PRÉSTAMO — COPIA DIGITAL",
                            $"N.° {prestamo.PrestamoId:D6}", textoEstado);

                        col.Item().Padding(20).Column(inner =>
                        {
                            inner.Item().Background(Color.FromHex("E3F2FD")).Padding(8).Row(r =>
                            {
                                r.ConstantItem(4).Background(Color.FromHex("1565C0")).Text("");
                                r.RelativeItem().PaddingLeft(8)
                                    .Text("Documento generado digitalmente por el Sistema GEPCP. No requiere firma física.")
                                    .FontSize(8).FontColor(Color.FromHex("1565C0")).Italic();
                            });

                            // Datos empleado
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

                            // Resumen del préstamo
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
                                    esSaldado ? "DURACIÓN:" : "CUOTAS ESTIMADAS RESTANTES:",
                                    esSaldado ? $"{duracion} días" : $"{cuotasEstimadas}");
                            });

                            // Banner de estado
                            inner.Item().PaddingTop(8).Background(
                                esSaldado ? VerdeFondo : Color.FromHex("FFF3E0"))
                                .Padding(10).Row(r =>
                                {
                                    r.ConstantItem(4).Background(colorEstado).Text("");
                                    r.RelativeItem().PaddingLeft(8)
                                        .Text(descripEstado)
                                        .Bold().FontSize(9).FontColor(colorEstado);
                                });

                            // Historial de abonos
                            if (abonos.Any())
                            {
                                SeccionLabel(inner, "Historial Completo de Abonos");
                                inner.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(c =>
                                    {
                                        c.ConstantColumn(22); c.RelativeColumn(3);
                                        c.RelativeColumn(2); c.RelativeColumn(2);
                                        c.RelativeColumn(2); c.RelativeColumn(4);
                                    });

                                    // Header
                                    foreach (var h in new[]
                                    {
                                        "#", "FECHA Y HORA", "MONTO ABONO",
                                        "SALDO ANTERIOR", "SALDO DESPUÉS", "OBSERVACIONES"
                                    })
                                        t.Cell().Background(Oscuro).Padding(4)
                                            .AlignCenter().Text(h).Bold().FontSize(7)
                                            .FontColor(Blanco);

                                    var saldoCalc = montoOriginal;
                                    int num = 1;

                                    foreach (var a in abonos)
                                    {
                                        var saldoAnt = saldoCalc;
                                        saldoCalc = Math.Round(saldoCalc - a.Monto, 2);
                                        if (saldoCalc < 0) saldoCalc = 0;
                                        var esFinal = saldoCalc == 0;
                                        var bg = num % 2 == 0
                                            ? Color.FromHex("F5F5F5")
                                            : Blanco;

                                        t.Cell().Background(bg).Padding(3).AlignCenter()
                                            .Text(num.ToString()).FontSize(7).FontColor(GrisTexto);
                                        t.Cell().Background(bg).Padding(3)
                                            .Text(a.FechaAbono.ToString("dd/MM/yyyy HH:mm")).FontSize(7);
                                        t.Cell().Background(bg).Padding(3).AlignRight()
                                            .Text($"₡{a.Monto:N2}").Bold().FontSize(7).FontColor(Verde);
                                        t.Cell().Background(bg).Padding(3).AlignRight()
                                            .Text($"₡{saldoAnt:N2}").FontSize(7).FontColor(GrisTexto);
                                        t.Cell().Background(bg).Padding(3).AlignRight()
                                            .Text(esFinal ? "₡0.00 ✓" : $"₡{saldoCalc:N2}")
                                            .FontSize(7)
                                            .FontColor(esFinal ? Verde : Color.FromHex("C55000"));

                                        t.Cell().Background(bg).Padding(3)
                                            .Text(a.Observaciones ?? "—").FontSize(6.5f)
                                            .FontColor(GrisTexto);

                                        num++;
                                    }

                                    // Fila de totales
                                    t.Cell().Background(Naranja).Padding(4).AlignCenter()
                                        .Text("∑").Bold().FontSize(9).FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4)
                                        .Text("TOTAL PAGADO").Bold().FontSize(7).FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4).AlignRight()
                                        .Text($"₡{totalPagado:N2}").Bold().FontSize(7).FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4).AlignRight()
                                        .Text($"₡{montoOriginal:N2}").FontSize(7).FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4).AlignRight()
                                        .Text($"₡{saldoPendiente:N2}").Bold().FontSize(7).FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4)
                                        .Text($"{abonos.Count} abono(s)").FontSize(7).FontColor(Blanco);
                                });
                            }

                            PiePagina(inner);
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ── GENERAR PDF CRÉDITO FERRETERÍA ────────────────────────────────────────

        public byte[] GenerarPDFCredito(CreditoFerreteria credito)
        {
            var logo = ObtenerLogoBytes();
            var emp = credito.Empleado;
            var abonos = credito.AbonosCreditoFerreteria
                .OrderBy(a => a.FechaAbono)
                .ToList();

            var totalPagado = abonos.Sum(a => a.Monto);
            var saldoPendiente = Math.Max(0, Math.Round(credito.MontoTotal - totalPagado, 2));
            var esSaldado = !credito.Activo || saldoPendiente == 0;

            var fechaFin = abonos.Any() ? abonos.Last().FechaAbono : DateTime.Now;
            var duracion = (int)(fechaFin - credito.FechaCredito).TotalDays;

            var cuotasEstimadas = credito.CuotaQuincenal > 0
                ? (int)Math.Ceiling((double)(saldoPendiente / credito.CuotaQuincenal))
                : 0;

            var (colorEstado, textoEstado, descripEstado) = esSaldado
                ? (Verde,
                   "✓ SALDADO",
                   $"Crédito cancelado en su totalidad el {fechaFin:dd/MM/yyyy}.")
                : (Color.FromHex("C55000"),
                   "EN CURSO",
                   $"Saldo pendiente: ₡{saldoPendiente:N2}  ·  " +
                   $"Cuotas estimadas restantes: {cuotasEstimadas}");

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        Encabezado(col, logo,
                            esSaldado
                                ? "FINIQUITO DE CRÉDITO FERRETERÍA"
                                : "ESTADO DE CRÉDITO FERRETERÍA",
                            $"N.° {credito.CreditoFerreteriaId:D6}",
                            textoEstado);

                        col.Item().Padding(20).Column(inner =>
                        {
                            // ── Datos del empleado ────────────────────────────────
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
                                FilaDatos(t, "DEPARTAMENTO:", emp.Departamento,
                                    "PUESTO:", emp.Puesto);
                            });

                            // ── Datos del crédito ─────────────────────────────────
                            SeccionLabel(inner, "Detalle del Crédito");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(120); c.RelativeColumn(2);
                                    c.ConstantColumn(120); c.RelativeColumn(2);
                                });
                                FilaDatos(t, "MONTO ORIGINAL:", $"₡{credito.MontoTotal:N2}",
                                    "CUOTA QUINCENAL:", $"₡{credito.CuotaQuincenal:N2}");
                                FilaDatos(t, "FECHA CRÉDITO:",
                                    $"{credito.FechaCredito:dd/MM/yyyy}",
                                    "TOTAL PAGADO:", $"₡{totalPagado:N2}");
                                FilaDatos(t, "SALDO PENDIENTE:", $"₡{saldoPendiente:N2}",
                                    esSaldado ? "DURACIÓN:" : "CUOTAS REST.:",
                                    esSaldado
                                        ? $"{duracion} días"
                                        : $"{cuotasEstimadas}");

                                // Descripción ocupa todo el ancho
                                t.Cell().Background(GrisFondo).Padding(4)
                                    .Text("DESCRIPCIÓN:").Bold().FontSize(9);
                                t.Cell().ColumnSpan(3).Padding(4)
                                    .Text(credito.Descripcion).FontSize(9);
                            });

                            // ── Banner de estado ──────────────────────────────────
                            inner.Item().PaddingTop(8)
                                .Background(esSaldado ? VerdeFondo : Color.FromHex("FFF3E0"))
                                .Padding(10).Row(r =>
                                {
                                    r.ConstantItem(4).Background(colorEstado).Text("");
                                    r.RelativeItem().PaddingLeft(8)
                                        .Text(descripEstado)
                                        .Bold().FontSize(9).FontColor(colorEstado);
                                });

                            // ── Historial de abonos ───────────────────────────────
                            if (abonos.Any())
                            {
                                SeccionLabel(inner, "Historial Completo de Abonos");
                                inner.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(c =>
                                    {
                                        c.ConstantColumn(22); // #
                                        c.RelativeColumn(3);  // Fecha
                                        c.RelativeColumn(2);  // Monto
                                        c.RelativeColumn(2);  // Saldo ant.
                                        c.RelativeColumn(2);  // Saldo desp.
                                        c.RelativeColumn(4);  // Observaciones
                                    });

                                    // Header
                                    foreach (var h in new[]
                                    {
                                "#", "FECHA Y HORA", "MONTO ABONO",
                                "SALDO ANTERIOR", "SALDO DESPUÉS", "OBSERVACIONES"
                            })
                                        t.Cell().Background(Oscuro).Padding(4)
                                            .AlignCenter().Text(h).Bold().FontSize(7)
                                            .FontColor(Blanco);

                                    var saldoCalc = credito.MontoTotal;
                                    int num = 1;

                                    foreach (var a in abonos)
                                    {
                                        var saldoAnt = saldoCalc;
                                        saldoCalc = Math.Round(saldoCalc - a.Monto, 2);
                                        if (saldoCalc < 0) saldoCalc = 0;
                                        var esFinal = saldoCalc == 0;
                                        var bg = num % 2 == 0
                                            ? Color.FromHex("F5F5F5")
                                            : Blanco;

                                        t.Cell().Background(bg).Padding(3).AlignCenter()
                                            .Text(num.ToString()).FontSize(7)
                                            .FontColor(GrisTexto);
                                        t.Cell().Background(bg).Padding(3)
                                            .Text(a.FechaAbono.ToString("dd/MM/yyyy HH:mm"))
                                            .FontSize(7);
                                        t.Cell().Background(bg).Padding(3).AlignRight()
                                            .Text($"₡{a.Monto:N2}").Bold().FontSize(7)
                                            .FontColor(Verde);
                                        t.Cell().Background(bg).Padding(3).AlignRight()
                                            .Text($"₡{saldoAnt:N2}").FontSize(7)
                                            .FontColor(GrisTexto);
                                        t.Cell().Background(bg).Padding(3).AlignRight()
                                            .Text(esFinal ? "₡0.00 ✓" : $"₡{saldoCalc:N2}")
                                            .FontSize(7)
                                            .FontColor(esFinal ? Verde : Color.FromHex("C55000"));
                                        t.Cell().Background(bg).Padding(3)
                                            .Text(a.Observaciones ?? "—").FontSize(6.5f)
                                            .FontColor(GrisTexto);

                                        num++;
                                    }

                                    // Fila de totales
                                    t.Cell().Background(Naranja).Padding(4).AlignCenter()
                                        .Text("∑").Bold().FontSize(9).FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4)
                                        .Text("TOTAL PAGADO").Bold().FontSize(7).FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4).AlignRight()
                                        .Text($"₡{totalPagado:N2}").Bold().FontSize(7)
                                        .FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4).AlignRight()
                                        .Text($"₡{credito.MontoTotal:N2}").FontSize(7)
                                        .FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4).AlignRight()
                                        .Text($"₡{saldoPendiente:N2}").Bold().FontSize(7)
                                        .FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4)
                                        .Text($"{abonos.Count} abono(s)").FontSize(7)
                                        .FontColor(Blanco);
                                });
                            }

                            // ── Resumen final si está saldado ─────────────────────
                            if (esSaldado)
                            {
                                inner.Item().PaddingTop(8).Background(VerdeFondo).Padding(12).Row(r =>
                                {
                                    r.ConstantItem(4).Background(Verde).Text("");
                                    r.RelativeItem().PaddingLeft(8).Column(c =>
                                    {
                                        c.Item().Text("CRÉDITO COMPLETAMENTE SALDADO")
                                            .Bold().FontSize(12).FontColor(Verde);
                                        c.Item().PaddingTop(4)
                                            .Text($"El empleado {emp.PrimerApellido} {emp.Nombre} " +
                                                  $"ha cancelado en su totalidad el crédito N.° " +
                                                  $"{credito.CreditoFerreteriaId:D6} por un monto de " +
                                                  $"₡{credito.MontoTotal:N2}, mediante {abonos.Count} " +
                                                  $"abono(s) realizados entre el " +
                                                  $"{credito.FechaCredito:dd/MM/yyyy} y el " +
                                                  $"{fechaFin:dd/MM/yyyy}.")
                                            .FontSize(9).FontColor(Verde);
                                    });
                                });
                            }

                            Firmas(inner,
                                $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim(),
                                emp.Cedula);
                            PiePagina(inner);
                        });
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GenerarPDFCreditoSinFirmas(CreditoFerreteria credito)
        {
            var logo = ObtenerLogoBytes();
            var emp = credito.Empleado;
            var abonos = credito.AbonosCreditoFerreteria
                .OrderBy(a => a.FechaAbono)
                .ToList();

            var totalPagado = abonos.Sum(a => a.Monto);
            var saldoPendiente = Math.Max(0, Math.Round(credito.MontoTotal - totalPagado, 2));
            var esSaldado = !credito.Activo || saldoPendiente == 0;

            var fechaFin = abonos.Any() ? abonos.Last().FechaAbono : DateTime.Now;
            var duracion = (int)(fechaFin - credito.FechaCredito).TotalDays;

            var cuotasEstimadas = credito.CuotaQuincenal > 0
                ? (int)Math.Ceiling((double)(saldoPendiente / credito.CuotaQuincenal))
                : 0;

            var (colorEstado, textoEstado, descripEstado) = esSaldado
                ? (Verde,
                   "✓ SALDADO",
                   $"Crédito cancelado en su totalidad el {fechaFin:dd/MM/yyyy}.")
                : (Color.FromHex("C55000"),
                   "EN CURSO",
                   $"Saldo pendiente: ₡{saldoPendiente:N2}  ·  " +
                   $"Cuotas estimadas restantes: {cuotasEstimadas}");

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        Encabezado(col, logo,
                            esSaldado
                                ? "FINIQUITO DE CRÉDITO FERRETERÍA — COPIA DIGITAL"
                                : "ESTADO DE CRÉDITO FERRETERÍA — COPIA DIGITAL",
                            $"N.° {credito.CreditoFerreteriaId:D6}",
                            textoEstado);

                        col.Item().Padding(20).Column(inner =>
                        {
                            inner.Item().Background(Color.FromHex("E3F2FD")).Padding(8).Row(r =>
                            {
                                r.ConstantItem(4).Background(Color.FromHex("1565C0")).Text("");
                                r.RelativeItem().PaddingLeft(8)
                                    .Text("Documento generado digitalmente por el Sistema GEPCP. No requiere firma física.")
                                    .FontSize(8).FontColor(Color.FromHex("1565C0")).Italic();
                            });

                            // ── Datos del empleado ────────────────────────────────
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
                                FilaDatos(t, "DEPARTAMENTO:", emp.Departamento,
                                    "PUESTO:", emp.Puesto);
                            });

                            // ── Datos del crédito ─────────────────────────────────
                            SeccionLabel(inner, "Detalle del Crédito");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(120); c.RelativeColumn(2);
                                    c.ConstantColumn(120); c.RelativeColumn(2);
                                });
                                FilaDatos(t, "MONTO ORIGINAL:", $"₡{credito.MontoTotal:N2}",
                                    "CUOTA QUINCENAL:", $"₡{credito.CuotaQuincenal:N2}");
                                FilaDatos(t, "FECHA CRÉDITO:",
                                    $"{credito.FechaCredito:dd/MM/yyyy}",
                                    "TOTAL PAGADO:", $"₡{totalPagado:N2}");
                                FilaDatos(t, "SALDO PENDIENTE:", $"₡{saldoPendiente:N2}",
                                    esSaldado ? "DURACIÓN:" : "CUOTAS REST.:",
                                    esSaldado
                                        ? $"{duracion} días"
                                        : $"{cuotasEstimadas}");

                                // Descripción ocupa todo el ancho
                                t.Cell().Background(GrisFondo).Padding(4)
                                    .Text("DESCRIPCIÓN:").Bold().FontSize(9);
                                t.Cell().ColumnSpan(3).Padding(4)
                                    .Text(credito.Descripcion).FontSize(9);
                            });

                            // ── Banner de estado ──────────────────────────────────
                            inner.Item().PaddingTop(8)
                                .Background(esSaldado ? VerdeFondo : Color.FromHex("FFF3E0"))
                                .Padding(10).Row(r =>
                                {
                                    r.ConstantItem(4).Background(colorEstado).Text("");
                                    r.RelativeItem().PaddingLeft(8)
                                        .Text(descripEstado)
                                        .Bold().FontSize(9).FontColor(colorEstado);
                                });

                            // ── Historial de abonos ───────────────────────────────
                            if (abonos.Any())
                            {
                                SeccionLabel(inner, "Historial Completo de Abonos");
                                inner.Item().Table(t =>
                                {
                                    t.ColumnsDefinition(c =>
                                    {
                                        c.ConstantColumn(22); // #
                                        c.RelativeColumn(3);  // Fecha
                                        c.RelativeColumn(2);  // Monto
                                        c.RelativeColumn(2);  // Saldo ant.
                                        c.RelativeColumn(2);  // Saldo desp.
                                        c.RelativeColumn(4);  // Observaciones
                                    });

                                    // Header
                                    foreach (var h in new[]
                                    {
                                "#", "FECHA Y HORA", "MONTO ABONO",
                                "SALDO ANTERIOR", "SALDO DESPUÉS", "OBSERVACIONES"
                            })
                                        t.Cell().Background(Oscuro).Padding(4)
                                            .AlignCenter().Text(h).Bold().FontSize(7)
                                            .FontColor(Blanco);

                                    var saldoCalc = credito.MontoTotal;
                                    int num = 1;

                                    foreach (var a in abonos)
                                    {
                                        var saldoAnt = saldoCalc;
                                        saldoCalc = Math.Round(saldoCalc - a.Monto, 2);
                                        if (saldoCalc < 0) saldoCalc = 0;
                                        var esFinal = saldoCalc == 0;
                                        var bg = num % 2 == 0
                                            ? Color.FromHex("F5F5F5")
                                            : Blanco;

                                        t.Cell().Background(bg).Padding(3).AlignCenter()
                                            .Text(num.ToString()).FontSize(7)
                                            .FontColor(GrisTexto);
                                        t.Cell().Background(bg).Padding(3)
                                            .Text(a.FechaAbono.ToString("dd/MM/yyyy HH:mm"))
                                            .FontSize(7);
                                        t.Cell().Background(bg).Padding(3).AlignRight()
                                            .Text($"₡{a.Monto:N2}").Bold().FontSize(7)
                                            .FontColor(Verde);
                                        t.Cell().Background(bg).Padding(3).AlignRight()
                                            .Text($"₡{saldoAnt:N2}").FontSize(7)
                                            .FontColor(GrisTexto);
                                        t.Cell().Background(bg).Padding(3).AlignRight()
                                            .Text(esFinal ? "₡0.00 ✓" : $"₡{saldoCalc:N2}")
                                            .FontSize(7)
                                            .FontColor(esFinal ? Verde : Color.FromHex("C55000"));
                                        t.Cell().Background(bg).Padding(3)
                                            .Text(a.Observaciones ?? "—").FontSize(6.5f)
                                            .FontColor(GrisTexto);

                                        num++;
                                    }

                                    // Fila de totales
                                    t.Cell().Background(Naranja).Padding(4).AlignCenter()
                                        .Text("∑").Bold().FontSize(9).FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4)
                                        .Text("TOTAL PAGADO").Bold().FontSize(7).FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4).AlignRight()
                                        .Text($"₡{totalPagado:N2}").Bold().FontSize(7)
                                        .FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4).AlignRight()
                                        .Text($"₡{credito.MontoTotal:N2}").FontSize(7)
                                        .FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4).AlignRight()
                                        .Text($"₡{saldoPendiente:N2}").Bold().FontSize(7)
                                        .FontColor(Blanco);
                                    t.Cell().Background(Naranja).Padding(4)
                                        .Text($"{abonos.Count} abono(s)").FontSize(7)
                                        .FontColor(Blanco);
                                });
                            }

                            // ── Resumen final si está saldado ─────────────────────
                            if (esSaldado)
                            {
                                inner.Item().PaddingTop(8).Background(VerdeFondo).Padding(12).Row(r =>
                                {
                                    r.ConstantItem(4).Background(Verde).Text("");
                                    r.RelativeItem().PaddingLeft(8).Column(c =>
                                    {
                                        c.Item().Text("CRÉDITO COMPLETAMENTE SALDADO")
                                            .Bold().FontSize(12).FontColor(Verde);
                                        c.Item().PaddingTop(4)
                                            .Text($"El empleado {emp.PrimerApellido} {emp.Nombre} " +
                                                  $"ha cancelado en su totalidad el crédito N.° " +
                                                  $"{credito.CreditoFerreteriaId:D6} por un monto de " +
                                                  $"₡{credito.MontoTotal:N2}, mediante {abonos.Count} " +
                                                  $"abono(s) realizados entre el " +
                                                  $"{credito.FechaCredito:dd/MM/yyyy} y el " +
                                                  $"{fechaFin:dd/MM/yyyy}.")
                                            .FontSize(9).FontColor(Verde);
                                    });
                                });
                            }

                            PiePagina(inner);
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ── VERSIÓN SIN FIRMAS (para envío por email) ─────────────────────────

        public byte[] GenerarPDFSinFirmas(PlanillaEmpleado planilla)
        {
            var logo = ObtenerLogoBytes();
            var emp = planilla.Empleado;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
                    page.Content().Column(col =>
                    {
                        Encabezado(col, logo, "COMPROBANTE DE PAGO — COPIA DIGITAL",
                            $"Período: {planilla.PeriodoPago.FechaInicio:dd/MM/yyyy} " +
                            $"— {planilla.PeriodoPago.FechaFin:dd/MM/yyyy}");

                        col.Item().Padding(20).Column(inner =>
                        {
                            // Banner informativo
                            inner.Item().Background(Color.FromHex("E3F2FD"))
                                .Padding(8).Row(r =>
                                {
                                    r.ConstantItem(4)
                                        .Background(Color.FromHex("1565C0")).Text("");
                                    r.RelativeItem().PaddingLeft(8)
                                        .Text("Documento generado digitalmente por el " +
                                              "Sistema GEPCP. No requiere firma física.")
                                        .FontSize(8).FontColor(Color.FromHex("1565C0"))
                                        .Italic();
                                });

                            SeccionLabel(inner, "Datos del Empleado");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(90); c.RelativeColumn(3);
                                    c.ConstantColumn(90); c.RelativeColumn(2);
                                });
                                FilaDatos(t, "NOMBRE:",
                                    $"{emp.PrimerApellido} {emp.SegundoApellido} " +
                                    $"{emp.Nombre}".Trim(),
                                    "CÉDULA:", emp.Cedula);
                                FilaDatos(t, "DEPARTAMENTO:", emp.Departamento,
                                    "PUESTO:", emp.Puesto);
                                FilaDatos(t, "TIPO DE PAGO:", emp.DescripcionTipoPago,
                                    "PERÍODO:", planilla.PeriodoPago.Descripcion);
                            });

                            SeccionLabel(inner, "Total Devengado");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(4); c.RelativeColumn(2);
                                    c.RelativeColumn(2); c.RelativeColumn(2);
                                });
                                foreach (var h in new[]
                                    { "CONCEPTO", "VALOR HORA", "HORAS", "MONTO" })
                                    t.Cell().Background(Naranja).Padding(4).AlignCenter()
                                        .Text(h).Bold().FontSize(8).FontColor(Blanco);

                                void FilaD(string c, string vh, string h,
                                    string m, bool bold = false)
                                {
                                    t.Cell().Background(GrisFondo).Padding(3)
                                        .Text(c).FontSize(8);
                                    t.Cell().Padding(3).AlignRight()
                                        .Text(vh).FontSize(8);
                                    t.Cell().Padding(3).AlignRight()
                                        .Text(h).FontSize(8);
                                    if (bold)
                                        t.Cell().Background(NaranjaFondo).Padding(3)
                                            .AlignRight().Text(m).Bold().FontSize(8)
                                            .FontColor(NaranjaOsc);
                                    else
                                        t.Cell().Padding(3).AlignRight()
                                            .Text(m).FontSize(8);
                                }

                                FilaD("Salario Base", "", "",
                                    $"₡{emp.SalarioBase:N2}");
                                FilaD("Jornada Ordinaria",
                                    $"₡{planilla.ValorHora:N2}",
                                    $"{planilla.HorasOrdinarias:N2}",
                                    $"₡{planilla.SalarioOrdinario:N2}");
                                FilaD("Horas Extras",
                                    planilla.HorasExtras > 0
                                        ? $"₡{planilla.ValorHoraExtra:N2}" : "—",
                                    $"{planilla.HorasExtras:N2}",
                                    $"₡{planilla.MontoHorasExtras:N2}");
                                if (planilla.AumentoAplicado > 0)
                                    FilaD("Comisión / Aumento", "", "",
                                        $"₡{planilla.AumentoAplicado:N2}");
                                if (planilla.MontoFeriados > 0)
                                    FilaD("Feriados", "", "",
                                        $"₡{planilla.MontoFeriados:N2}");
                                FilaD("TOTAL BRUTO DEVENGADO", "", "",
                                    $"₡{planilla.TotalDevengado:N2}", true);
                            });

                            SeccionLabel(inner, "Deducciones");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(6); c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                });

                                void FilaDed(string c, string pct,
                                    string m, bool total = false)
                                {
                                    var bg = total ? RojoFondo : Blanco;
                                    if (total)
                                    {
                                        t.Cell().Background(bg).Padding(3)
                                            .Text(c).FontSize(8).Bold();
                                        t.Cell().Background(bg).Padding(3)
                                            .AlignCenter().Text(pct).FontSize(8);
                                        t.Cell().Background(bg).Padding(3)
                                            .AlignRight().Text(m).FontSize(8)
                                            .Bold().FontColor(Rojo);
                                    }
                                    else
                                    {
                                        t.Cell().Background(bg).Padding(3)
                                            .Text(c).FontSize(8);
                                        t.Cell().Background(bg).Padding(3)
                                            .AlignCenter().Text(pct).FontSize(8);
                                        t.Cell().Background(bg).Padding(3)
                                            .AlignRight().Text(m).FontSize(8)
                                            .FontColor(Color.FromHex("222222"));
                                    }
                                }
                                // CCSS desglosada (fallback a valores estándar si el período no los tiene)
                                var totalBruto2 = planilla.TotalDevengado;
                                var per2 = planilla.PeriodoPago;
                                var factorMes2 = per2.TipoPeriodo switch
                                {
                                    TipoPeriodo.Semanal => 52m / 12m,
                                    TipoPeriodo.Mensual => 1m,
                                    _ => 2m
                                };
                                var brutoMensual2 = totalBruto2 * factorMes2;
                                var pctSEM2 = per2.PorcentajeSEM > 0 ? per2.PorcentajeSEM : 5.50m;
                                var pctIVM2 = per2.PorcentajeIVM > 0 ? per2.PorcentajeIVM : 4.33m;
                                var pctBP2  = per2.PorcentajeBP  > 0 ? per2.PorcentajeBP  : 1.00m;
                                var sem2 = Math.Round(totalBruto2 * (pctSEM2 / 100m), 2);
                                var ivm2 = Math.Round(totalBruto2 * (pctIVM2 / 100m), 2);
                                var bp2 = Math.Round(totalBruto2 * (pctBP2 / 100m), 2);

                                foreach (var h in new[] { "CONCEPTO", "%", "MONTO" })
                                    t.Cell().Background(Naranja).Padding(4)
                                        .Text(h).Bold().FontSize(8).FontColor(Blanco);

                                // Salario bruto mensual (referencia)
                                t.Cell().ColumnSpan(3).Padding(3)
                                    .Text($"Salario Bruto Mensual (referencia): ₡{brutoMensual2:N2}")
                                    .Bold().FontSize(8).FontColor(Color.FromHex("333333"));

                                t.Cell().ColumnSpan(3).Background(GrisFondo).Padding(3)
                                    .Text($"CARGAS SOCIALES (CCSS) — {planilla.PorcentajeCCSS:N2}%")
                                    .Bold().FontSize(7.5f).FontColor(Color.FromHex("555555"));

                                FilaDed("   Seguro Enfermedad y Maternidad (SEM)", $"{pctSEM2:N2}%",
                                    $"₡{sem2:N2}");
                                FilaDed("   Invalidez, Vejez y Muerte (IVM)", $"{pctIVM2:N2}%",
                                    $"₡{ivm2:N2}");
                                FilaDed("   Banco Popular (Aporte trabajador)", $"{pctBP2:N2}%",
                                    $"₡{bp2:N2}");
                                FilaDed("Subtotal CCSS",
                                    $"{planilla.PorcentajeCCSS:N2}%",
                                    $"₡{planilla.DeduccionCCSS:N2}");

                                if (planilla.DeduccionRenta > 0)
                                {
                                    var baseImponibleMensual2 = brutoMensual2;
                                    t.Cell().ColumnSpan(3).Background(GrisFondo).Padding(3)
                                        .Text($"IMPUESTO SOBRE LA RENTA (ISR) — Base mensual: ₡{baseImponibleMensual2:N2}")
                                        .Bold().FontSize(7.5f).FontColor(Color.FromHex("555555"));

                                    if (baseImponibleMensual2 > per2.ISR_Tramo2_Desde)
                                        FilaDed($"   Excedente ₡{per2.ISR_Tramo2_Desde:N0} – ₡{per2.ISR_Tramo2_Hasta:N0}",
                                            $"{per2.ISR_Tramo2_Porcentaje:N0}%",
                                            $"₡{Math.Round(Math.Min(Math.Max(baseImponibleMensual2 - per2.ISR_Tramo2_Desde, 0), per2.ISR_Tramo2_Hasta - per2.ISR_Tramo2_Desde) * (per2.ISR_Tramo2_Porcentaje / 100m) / factorMes2, 2):N2}");
                                    if (baseImponibleMensual2 > per2.ISR_Tramo3_Desde)
                                        FilaDed($"   Excedente ₡{per2.ISR_Tramo3_Desde:N0} – ₡{per2.ISR_Tramo3_Hasta:N0}",
                                            $"{per2.ISR_Tramo3_Porcentaje:N0}%",
                                            $"₡{Math.Round(Math.Min(Math.Max(baseImponibleMensual2 - per2.ISR_Tramo3_Desde, 0), per2.ISR_Tramo3_Hasta - per2.ISR_Tramo3_Desde) * (per2.ISR_Tramo3_Porcentaje / 100m) / factorMes2, 2):N2}");
                                    if (baseImponibleMensual2 > per2.ISR_Tramo4_Desde)
                                        FilaDed($"   Excedente ₡{per2.ISR_Tramo4_Desde:N0} – ₡{per2.ISR_Tramo4_Hasta:N0}",
                                            $"{per2.ISR_Tramo4_Porcentaje:N0}%",
                                            $"₡{Math.Round(Math.Min(Math.Max(baseImponibleMensual2 - per2.ISR_Tramo4_Desde, 0), per2.ISR_Tramo4_Hasta - per2.ISR_Tramo4_Desde) * (per2.ISR_Tramo4_Porcentaje / 100m) / factorMes2, 2):N2}");
                                    if (baseImponibleMensual2 > per2.ISR_Tramo5_Desde)
                                        FilaDed($"   Excedente sobre ₡{per2.ISR_Tramo5_Desde:N0}",
                                            $"{per2.ISR_Tramo5_Porcentaje:N0}%",
                                            $"₡{Math.Round((baseImponibleMensual2 - per2.ISR_Tramo5_Desde) * (per2.ISR_Tramo5_Porcentaje / 100m) / factorMes2, 2):N2}");

                                    // Créditos fiscales
                                    var credHijos2 = emp.NumHijos * per2.ISR_CreditoHijo;
                                    var credConyuge2 = emp.TieneConyuge ? per2.ISR_CreditoConyuge : 0m;
                                    var totalCreditos2 = credHijos2 + credConyuge2;
                                    if (totalCreditos2 > 0)
                                        FilaDed($"   Créditos fiscales ({emp.NumHijos} hijo(s), {(emp.TieneConyuge ? "cónyuge" : "sin cónyuge")})",
                                            "", $"-₡{Math.Round(totalCreditos2 / factorMes2, 2):N2}");

                                    FilaDed("Subtotal ISR (retención por período)", "",
                                        $"₡{planilla.DeduccionRenta:N2}");
                                }
                                else
                                {
                                    FilaDed("Impuesto sobre la Renta (ISR)", "Exento",
                                        $"₡0.00");
                                }
                                if (planilla.DeduccionCreditoFerreteria > 0)
                                    FilaDed("Crédito Ferretería", "",
                                        $"₡{planilla.DeduccionCreditoFerreteria:N2}");
                                if (planilla.DeduccionPrestamos > 0)
                                    FilaDed("Préstamo Personal", "",
                                        $"₡{planilla.DeduccionPrestamos:N2}");
                                if (planilla.DeduccionHorasNoLaboradas > 0)
                                    FilaDed("Horas No Laboradas", "",
                                        $"₡{planilla.DeduccionHorasNoLaboradas:N2}");
                                if (planilla.DeduccionIncapacidad > 0)
                                    FilaDed("Incapacidad", "",
                                        $"₡{planilla.DeduccionIncapacidad:N2}");
                                if (planilla.DeduccionVacaciones > 0)
                                    FilaDed("Vacaciones Sin Pago", "",
                                        $"₡{planilla.DeduccionVacaciones:N2}");
                                if (planilla.OtrasDeducciones > 0)
                                    FilaDed("Otras Deducciones", "",
                                        $"₡{planilla.OtrasDeducciones:N2}");
                                FilaDed("TOTAL DEDUCCIONES", "",
                                    $"₡{planilla.TotalDeducciones:N2}", true);
                            });

                            inner.Item().PaddingTop(10).Background(Oscuro)
                                .Padding(12).Row(row =>
                                {
                                    row.RelativeItem().Text("NETO A PAGAR")
                                        .Bold().FontSize(14).FontColor(Blanco);
                                    row.ConstantItem(180).AlignRight()
                                        .Text($"₡{planilla.NetoAPagar:N2}")
                                        .Bold().FontSize(18).FontColor(Naranja);
                                });

                            PiePagina(inner, "Copia digital — válida sin firma");
                        });
                    });
                });
            }).GeneratePdf();
        }

        // ── BOLETA FERIADO ────────────────────────────────────────────────────
        public byte[] GenerarPDFFeriado(PagoFeriado pago)
        {
            return GenerarPDFFeriadoInterno(pago, conFirmas: true);
        }

        public byte[] GenerarPDFFeriadoSinFirmas(PagoFeriado pago)
        {
            return GenerarPDFFeriadoInterno(pago, conFirmas: false);
        }

        private byte[] GenerarPDFFeriadoInterno(PagoFeriado pago, bool conFirmas)
        {
            var logo = ObtenerLogoBytes();
            var emp = pago.Empleado;
            var feriado = pago.Feriado;
            var periodo = pago.PeriodoPago;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content().Column(col =>
                    {
                        Encabezado(col, logo, "BOLETA DE PAGO — FERIADO",
                            $"Período: {periodo.FechaInicio:dd/MM/yyyy} — {periodo.FechaFin:dd/MM/yyyy}");

                        col.Item().Padding(20).Column(inner =>
                        {
                            SeccionLabel(inner, "Datos del Empleado");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(); c.RelativeColumn();
                                    c.RelativeColumn(); c.RelativeColumn();
                                });
                                FilaDatos(t, "NOMBRE", $"{emp.Nombre} {emp.PrimerApellido} {emp.SegundoApellido}",
                                    "CÉDULA", emp.Cedula);
                                FilaDatos(t, "PUESTO", emp.Puesto,
                                    "DEPARTAMENTO", emp.Departamento);
                                FilaDatos(t, "SALARIO BASE", $"₡{emp.SalarioBase:N2}",
                                    "VALOR DÍA", $"₡{Math.Round(emp.SalarioBase / 30m, 2):N2}");
                            });

                            SeccionLabel(inner, "Detalle del Feriado");
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(); c.RelativeColumn();
                                    c.RelativeColumn(); c.RelativeColumn();
                                });
                                FilaDatos(t, "FERIADO", feriado.Nombre,
                                    "FECHA", feriado.Fecha.ToString("dd/MM/yyyy"));
                                FilaDatos(t, "TIPO", feriado.Tipo.ToString(),
                                    "¿TRABAJÓ?", pago.Trabajado ? "Sí — Pago doble" : "No — Pago normal");
                            });

                            inner.Item().PaddingTop(10).Background(pago.Trabajado ? VerdeFondo : NaranjaFondo)
                                .Padding(12).Row(row =>
                                {
                                    row.RelativeItem().Text(pago.Trabajado
                                        ? "El empleado trabajó en día feriado. Se le reconoce pago doble por ley."
                                        : "El empleado no trabajó en día feriado. El pago está incluido en salario ordinario.")
                                        .FontSize(9).Bold().FontColor(pago.Trabajado ? Verde : NaranjaOsc);
                                });

                            inner.Item().PaddingTop(10).Background(Oscuro)
                                .Padding(12).Row(row =>
                                {
                                    row.RelativeItem().Text("MONTO EXTRA A PAGAR")
                                        .Bold().FontSize(14).FontColor(Blanco);
                                    row.ConstantItem(180).AlignRight()
                                        .Text($"₡{pago.MontoTotal:N2}")
                                        .Bold().FontSize(18).FontColor(Naranja);
                                });

                            if (conFirmas)
                                Firmas(inner, $"{emp.Nombre} {emp.PrimerApellido}", emp.Cedula);
                            else
                                PiePagina(inner, "Copia digital — válida sin firma");
                        });
                    });
                });
            }).GeneratePdf();
        }
    }
}