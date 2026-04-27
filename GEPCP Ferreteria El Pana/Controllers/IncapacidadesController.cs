using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class IncapacidadesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IncapacidadesController> _logger;
        private readonly ComprobantePlanillaService _servicioPDF;
        private readonly ReglasNegocioConfig _reglas;
        private readonly AuditoriaService _auditoria;

        public IncapacidadesController(
            ApplicationDbContext context,
            ILogger<IncapacidadesController> logger,
            ComprobantePlanillaService servicioPDF,
            IOptions<ReglasNegocioConfig> reglas,
            AuditoriaService auditoria)
        {
            _context = context;
            _logger = logger;
            _servicioPDF = servicioPDF;
            _reglas = reglas.Value;
            _auditoria = auditoria;
        }

        // INDEX
        public async Task<IActionResult> Index(string? busqueda, string? entidad, bool verTodos = false)
        {
            ViewBag.Busqueda = busqueda;
            ViewBag.EntidadFiltro = entidad;
            ViewBag.VerTodos = verTodos;

            if (!verTodos && string.IsNullOrWhiteSpace(busqueda) && string.IsNullOrWhiteSpace(entidad))
                return View(new List<Incapacidad>());

            var query = _context.Incapacidades
                .Include(i => i.Empleado)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(entidad) && Enum.TryParse<EntidadIncapacidad>(entidad, out var ent))
                query = query.Where(i => i.Entidad == ent);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var t = busqueda.Trim().ToLower();
                query = query.Where(i =>
                    i.Empleado.Nombre.ToLower().Contains(t) ||
                    i.Empleado.PrimerApellido.ToLower().Contains(t) ||
                    i.Empleado.Cedula.Contains(t) ||
                    i.TipoIncapacidad.ToLower().Contains(t) ||
                    (i.TiqueteCCSS != null && i.TiqueteCCSS.ToLower().Contains(t)));
            }

            var incapacidades = await query
                .OrderByDescending(i => i.FechaInicio)
                .ThenBy(i => i.Empleado.PrimerApellido)
                .ToListAsync();

            ViewBag.TotalRegistros = incapacidades.Count;
            ViewBag.TotalDias = incapacidades.Sum(i => i.TotalDias);
            ViewBag.TotalMonto = incapacidades.Sum(i => i.MontoTotal);
            ViewBag.TotalPatrono = incapacidades.Sum(i => i.MontoTotal);

            return View(incapacidades);
        }

        // CREATE
        public async Task<IActionResult> Create()
        {
            await CargarEmpleadosViewBag();
            return View(new Incapacidad
            {
                FechaInicio = DateTime.Today,
                FechaFin = DateTime.Today.AddDays(1),
                PorcentajePago = 50,
                Entidad = EntidadIncapacidad.CCSS
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Incapacidad model)
        {
            try
            {
                ModelState.Remove("Empleado");
                await AplicarValidacionesAsync(model);

                if (!ModelState.IsValid)
                {
                    await CargarEmpleadosViewBag(model.EmpleadoId);
                    return View(model);
                }

                var empleado = await _context.Empleados.FindAsync(model.EmpleadoId);
                if (empleado == null)
                {
                    ModelState.AddModelError("EmpleadoId", "Empleado no encontrado.");
                    await CargarEmpleadosViewBag();
                    return View(model);
                }

                var solapamiento = await _context.Incapacidades.AnyAsync(i =>
                    i.EmpleadoId == model.EmpleadoId &&
                    i.FechaInicio <= model.FechaFin &&
                    i.FechaFin >= model.FechaInicio);

                if (solapamiento)
                {
                    ModelState.AddModelError("", "Ya existe una incapacidad que se solapa con estas fechas.");
                    await CargarEmpleadosViewBag(model.EmpleadoId);
                    return View(model);
                }

                model.TotalDias = (model.FechaFin - model.FechaInicio).Days + 1;
                var divisor = _reglas.SalarioDivisorMensual > 0 ? _reglas.SalarioDivisorMensual : 30;
                var salarioDiario = Math.Round(empleado.SalarioBase / divisor, 2);

                // Cálculo según ley CR por tipo de incapacidad
                Enum.TryParse<TipoIncapacidad>(model.TipoIncapacidad, out var tipoInc);

                if (model.Entidad == EntidadIncapacidad.CCSS)
                {
                    model.ResponsablePago = ResponsablePago.CCSS;

                    if (tipoInc == TipoIncapacidad.LicenciaMaternidad)
                    {
                        // Art. 95 CT: Patrono paga 50%, CCSS paga 50% durante 4 meses
                        model.PorcentajePago = 50;
                        model.MontoPorDia = Math.Round(salarioDiario * 0.50m, 2);
                        model.DiasPagadosPatrono = model.TotalDias;
                        model.MontoTotal = Math.Round(model.MontoPorDia * model.DiasPagadosPatrono, 2);
                    }
                    else if (tipoInc == TipoIncapacidad.LicenciaPaternidad)
                    {
                        // Ley 9877: Patrono paga 100% por 2 días hábiles
                        model.PorcentajePago = 100;
                        model.MontoPorDia = salarioDiario;
                        model.DiasPagadosPatrono = model.TotalDias;
                        model.ResponsablePago = ResponsablePago.Patrono;
                        model.MontoTotal = Math.Round(model.MontoPorDia * model.DiasPagadosPatrono, 2);
                    }
                    else
                    {
                        // Enfermedad común: Patrono paga 50% primeros 3 días,
                        // CCSS paga 60% a partir del día 4
                        model.DiasPagadosPatrono = Math.Min(model.TotalDias, 3);
                        model.PorcentajePago = 50;
                        model.MontoPorDia = Math.Round(salarioDiario * 0.50m, 2);
                        model.MontoTotal = Math.Round(model.MontoPorDia * model.DiasPagadosPatrono, 2);
                    }
                }
                else // INS
                {
                    // Accidente laboral/tránsito: INS paga 60% desde el día 1
                    model.DiasPagadosPatrono = 0;
                    model.PorcentajePago = 60;
                    model.MontoPorDia = Math.Round(salarioDiario * 0.60m, 2);
                    model.MontoTotal = 0; // Patrono no paga, INS cubre
                    model.ResponsablePago = ResponsablePago.INS;
                }

                _context.Add(model);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Registrar incapacidad", "Incapacidades",
                    $"{empleado.PrimerApellido} {empleado.Nombre} — {model.TotalDias} días");

                TempData["Success"] = $"Incapacidad de {model.TotalDias} días registrada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar incapacidad");
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }
        }

        // EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0) return NotFound();

            var incapacidad = await _context.Incapacidades
                .Include(i => i.Empleado)
                .FirstOrDefaultAsync(i => i.IncapacidadId == id);

            if (incapacidad == null) return NotFound();

            ViewBag.EmpleadoNombre = $"{incapacidad.Empleado.PrimerApellido} {incapacidad.Empleado.Nombre}";
            ViewBag.EmpleadoCedula = incapacidad.Empleado.Cedula;

            await CargarEmpleadosViewBag(incapacidad.EmpleadoId);
            return View(incapacidad);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Incapacidad model)
        {
            try
            {
                if (id != model.IncapacidadId) return NotFound();

                ModelState.Remove("Empleado");
                await AplicarValidacionesAsync(model);

                if (!ModelState.IsValid)
                {
                    var reg = await _context.Incapacidades.Include(i => i.Empleado)
                        .FirstOrDefaultAsync(i => i.IncapacidadId == id);

                    ViewBag.EmpleadoNombre = reg != null ? $"{reg.Empleado.PrimerApellido} {reg.Empleado.Nombre}" : "";
                    ViewBag.EmpleadoCedula = reg?.Empleado.Cedula;

                    await CargarEmpleadosViewBag(model.EmpleadoId);
                    return View(model);
                }

                var registro = await _context.Incapacidades
                    .Include(i => i.Empleado)
                    .FirstOrDefaultAsync(i => i.IncapacidadId == id);

                if (registro == null) return NotFound();

                var solapamiento = await _context.Incapacidades.AnyAsync(i =>
                    i.EmpleadoId == model.EmpleadoId &&
                    i.IncapacidadId != id &&
                    i.FechaInicio <= model.FechaFin &&
                    i.FechaFin >= model.FechaInicio);

                if (solapamiento)
                {
                    ModelState.AddModelError("", "Ya existe una incapacidad que se solapa con estas fechas.");
                    await CargarEmpleadosViewBag(model.EmpleadoId);
                    return View(model);
                }

                registro.EmpleadoId = model.EmpleadoId;
                registro.Entidad = model.Entidad;
                registro.TipoIncapacidad = model.TipoIncapacidad;
                registro.FechaInicio = model.FechaInicio;
                registro.FechaFin = model.FechaFin;
                registro.TiqueteCCSS = model.TiqueteCCSS;
                registro.PorcentajePago = model.PorcentajePago;
                registro.Observaciones = model.Observaciones;
                registro.TotalDias = (model.FechaFin - model.FechaInicio).Days + 1;

                var empleado = await _context.Empleados.FindAsync(model.EmpleadoId);
                if (empleado != null)
                {
                    var divisor = _reglas.SalarioDivisorMensual > 0 ? _reglas.SalarioDivisorMensual : 30;
                    var salarioDiario = Math.Round(empleado.SalarioBase / divisor, 2);
                    Enum.TryParse<TipoIncapacidad>(model.TipoIncapacidad, out var tipoInc);

                    if (registro.Entidad == EntidadIncapacidad.CCSS)
                    {
                        registro.ResponsablePago = ResponsablePago.CCSS;

                        if (tipoInc == TipoIncapacidad.LicenciaMaternidad)
                        {
                            registro.PorcentajePago = 50;
                            registro.MontoPorDia = Math.Round(salarioDiario * 0.50m, 2);
                            registro.DiasPagadosPatrono = registro.TotalDias;
                            registro.MontoTotal = Math.Round(registro.MontoPorDia * registro.DiasPagadosPatrono, 2);
                        }
                        else if (tipoInc == TipoIncapacidad.LicenciaPaternidad)
                        {
                            registro.PorcentajePago = 100;
                            registro.MontoPorDia = salarioDiario;
                            registro.DiasPagadosPatrono = registro.TotalDias;
                            registro.ResponsablePago = ResponsablePago.Patrono;
                            registro.MontoTotal = Math.Round(registro.MontoPorDia * registro.DiasPagadosPatrono, 2);
                        }
                        else
                        {
                            registro.DiasPagadosPatrono = Math.Min(registro.TotalDias, 3);
                            registro.PorcentajePago = 50;
                            registro.MontoPorDia = Math.Round(salarioDiario * 0.50m, 2);
                            registro.MontoTotal = Math.Round(registro.MontoPorDia * registro.DiasPagadosPatrono, 2);
                        }
                    }
                    else
                    {
                        registro.DiasPagadosPatrono = 0;
                        registro.PorcentajePago = 60;
                        registro.MontoPorDia = Math.Round(salarioDiario * 0.60m, 2);
                        registro.MontoTotal = 0;
                        registro.ResponsablePago = ResponsablePago.INS;
                    }
                }

                _context.Update(registro);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Editar incapacidad", "Incapacidades",
                    $"{registro.Empleado.PrimerApellido} {registro.Empleado.Nombre}");

                TempData["Success"] = "Incapacidad actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar incapacidad");
                await CargarEmpleadosViewBag(model.EmpleadoId);
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
                var registro = await _context.Incapacidades
                    .Include(i => i.Empleado)
                    .FirstOrDefaultAsync(i => i.IncapacidadId == id);

                if (registro == null)
                {
                    TempData["Error"] = "Registro no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Incapacidades.Remove(registro);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Eliminar incapacidad", "Incapacidades",
                    $"{registro.Empleado.PrimerApellido} {registro.Empleado.Nombre}");

                TempData["Success"] = "Incapacidad eliminada correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar incapacidad");
                TempData["Error"] = "Error al eliminar el registro.";
            }
            return RedirectToAction(nameof(Index));
        }

        // PDF
        [HttpGet]
        public async Task<IActionResult> DescargarPDF(int id)
        {
            try
            {
                var registro = await _context.Incapacidades
                    .Include(i => i.Empleado)
                    .FirstOrDefaultAsync(i => i.IncapacidadId == id);

                if (registro == null) return NotFound();

                var usuario = HttpContext.Session.GetString("Usuario") ?? "Sistema";
                var pdfBytes = _servicioPDF.GenerarPDFIncapacidad(registro, usuario);
                var nombre = $"Incapacidad_{registro.Empleado.PrimerApellido}_{registro.FechaInicio:ddMMyyyy}.pdf";

                await _auditoria.RegistrarAsync(
                    usuario,
                    "Descargar PDF incapacidad", "Incapacidades",
                    $"{registro.Empleado.PrimerApellido} {registro.Empleado.Nombre}");

                return File(pdfBytes, "application/pdf", nombre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF");
                TempData["Error"] = "Error al generar el PDF.";
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
                var registro = await _context.Incapacidades
                    .Include(i => i.Empleado)
                    .FirstOrDefaultAsync(i => i.IncapacidadId == id);

                if (registro == null)
                {
                    TempData["Error"] = "Incapacidad no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                var correo = registro.Empleado.CorreoElectronico;
                if (string.IsNullOrWhiteSpace(correo))
                {
                    TempData["Error"] =
                        $"{registro.Empleado.PrimerApellido} " +
                        $"{registro.Empleado.Nombre} no tiene correo registrado.";
                    return RedirectToAction(nameof(Index));
                }

                var pdfBytes = _servicioPDF.GenerarPDFIncapacidadSinFirmas(registro,
                    HttpContext.Session.GetString("Usuario") ?? "Sistema");
                var nombreArchivo =
                    $"Incapacidad_{registro.Empleado.PrimerApellido}_{registro.FechaInicio:ddMMyyyy}.pdf";

                var emailSvc = HttpContext.RequestServices
                    .GetRequiredService<EmailService>();

                var asunto = $"Boleta de Incapacidad — {registro.FechaInicio:dd/MM/yyyy}";
                var cuerpo = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;'>
    <div style='background:#1A1A2E;padding:20px;'>
        <h2 style='color:#FF7A00;margin:0;'>Ferretería El Pana SRL</h2>
        <p style='color:#888;margin:4px 0 0;font-size:13px;'>
            Departamento de Recursos Humanos
        </p>
    </div>
    <div style='padding:20px;border:1px solid #eee;'>
        <p>Estimado(a) <strong>{registro.Empleado.PrimerApellido}
           {registro.Empleado.Nombre}</strong>,</p>
        <p>Adjunto encontrará su boleta de incapacidad correspondiente al
           período <strong>{registro.FechaInicio:dd/MM/yyyy} - {registro.FechaFin:dd/MM/yyyy}</strong>.</p>
        <table style='width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;'>
            <tr style='background:#f9f9f9;'>
                <td style='padding:8px;border:1px solid #eee;'>Período</td>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    {registro.FechaInicio:dd/MM/yyyy} - {registro.FechaFin:dd/MM/yyyy}
                </td>
            </tr>
            <tr>
                <td style='padding:8px;border:1px solid #eee;'>Tipo</td>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    {registro.TipoIncapacidad}
                </td>
            </tr>
            <tr>
                <td style='padding:8px;border:1px solid #eee;'>Días</td>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    {registro.TotalDias}
                </td>
            </tr>
            <tr style='background:#fff9f0;'>
                <td style='padding:8px;border:1px solid #eee;font-weight:bold;'>
                    Monto Total
                </td>
                <td style='padding:8px;border:1px solid #eee;
                           font-weight:bold;font-size:16px;color:#FF7A00;'>
                    ₡{registro.MontoTotal:N2}
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
                    $"{registro.Empleado.PrimerApellido} {registro.Empleado.Nombre}",
                    asunto, cuerpo, pdfBytes, nombreArchivo);

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Enviar boleta por email", "Incapacidades",
                    $"{registro.Empleado.PrimerApellido} {registro.Empleado.Nombre} " +
                    $"→ {correo}");

                TempData[enviado ? "Success" : "Error"] = enviado
                    ? $"Boleta enviada a {correo}."
                    : "Error al enviar el correo. Verificá la configuración SMTP.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email incapacidad ID: {Id}", id);
                TempData["Error"] = "Error al enviar el correo. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }

        // BUSCADOR EMPLEADOS
        [HttpGet]
        public async Task<IActionResult> BuscarEmpleados(string? termino)
        {
            if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                return Json(new List<object>());

            var t = termino.Trim().ToLower();
            var empleados = await (from e in _context.Empleados
                .Where(e => e.Activo && (
                    e.Nombre.ToLower().Contains(t) ||
                    e.PrimerApellido.ToLower().Contains(t) ||
                    (e.SegundoApellido != null && e.SegundoApellido.ToLower().Contains(t)) ||
                    e.Cedula.Contains(t)))
                join p in _context.Puestos on e.Puesto equals p.Nombre into puestoGroup
                from p in puestoGroup.DefaultIfEmpty()
                orderby e.PrimerApellido
                select new
                {
                    id = e.EmpleadoId,
                    nombre = (e.PrimerApellido + " " + e.SegundoApellido + " " + e.Nombre).Trim(),
                    cedula = e.Cedula,
                    puesto = p != null ? p.Codigo + " - " + e.Puesto : e.Puesto
                })
                .AsNoTracking()
                .Take(10)
                .ToListAsync();

            return Json(empleados);
        }

        [HttpGet]
        public async Task<IActionResult> TodosLosEmpleados()
        {
            var empleados = await (from e in _context.Empleados
                .Where(e => e.Activo)
                join p in _context.Puestos on e.Puesto equals p.Nombre into puestoGroup
                from p in puestoGroup.DefaultIfEmpty()
                orderby e.PrimerApellido
                select new
                {
                    id = e.EmpleadoId,
                    nombre = (e.PrimerApellido + " " + e.SegundoApellido + " " + e.Nombre).Trim(),
                    cedula = e.Cedula,
                    puesto = p != null ? p.Codigo + " - " + e.Puesto : e.Puesto
                })
                .AsNoTracking()
                .ToListAsync();

            return Json(empleados);
        }

        // HELPERS
        private async Task CargarEmpleadosViewBag(int? selectedId = null)
        {
            ViewBag.Empleados = await _context.Empleados
                .Where(e => e.Activo)
                .OrderBy(e => e.PrimerApellido).ThenBy(e => e.Nombre)
                .Select(e => new SelectListItem
                {
                    Value = e.EmpleadoId.ToString(),
                    Text = $"{e.PrimerApellido} {e.SegundoApellido} {e.Nombre} — {e.Cedula}",
                    Selected = e.EmpleadoId == selectedId
                })
                .ToListAsync();
        }

        private async Task AplicarValidacionesAsync(Incapacidad model)
        {
            if (model.EmpleadoId <= 0)
                ModelState.AddModelError("EmpleadoId", "Debe seleccionar un empleado.");

            if (string.IsNullOrWhiteSpace(model.TipoIncapacidad))
                ModelState.AddModelError("TipoIncapacidad", "Debe seleccionar el tipo de incapacidad.");

            if (model.FechaInicio == default)
                ModelState.AddModelError("FechaInicio", "La fecha de inicio es obligatoria.");

            if (model.FechaFin == default)
                ModelState.AddModelError("FechaFin", "La fecha de fin es obligatoria.");

            if (model.FechaInicio > model.FechaFin)
                ModelState.AddModelError("FechaFin", "La fecha de fin no puede ser anterior a la de inicio.");

            if (model.Entidad == EntidadIncapacidad.CCSS && string.IsNullOrWhiteSpace(model.TiqueteCCSS))
                ModelState.AddModelError("TiqueteCCSS", "El tiquete CCSS es obligatorio para incapacidades de la CCSS.");

            // Validaciones por tipo según ley de Costa Rica
            if (model.FechaInicio != default && model.FechaFin != default)
            {
                var dias = (model.FechaFin - model.FechaInicio).Days + 1;

                if (Enum.TryParse<TipoIncapacidad>(model.TipoIncapacidad, out var tipo))
                {
                    switch (tipo)
                    {
                        case TipoIncapacidad.LicenciaMaternidad:
                            // Art. 95 CT: licencia de maternidad = 4 meses (aprox. 120 días)
                            if (dias < 112 || dias > 128)
                                ModelState.AddModelError("FechaFin",
                                    "La licencia de maternidad debe ser de aproximadamente 4 meses (112-128 días) según Art. 95 del Código de Trabajo.");
                            if (model.Entidad != EntidadIncapacidad.CCSS)
                                ModelState.AddModelError("Entidad",
                                    "La licencia de maternidad es responsabilidad de la CCSS.");
                            break;

                        case TipoIncapacidad.LicenciaPaternidad:
                            // Ley 9877: licencia de paternidad = 2 días hábiles
                            if (dias > 4)
                                ModelState.AddModelError("FechaFin",
                                    "La licencia de paternidad es de máximo 2 días hábiles según Ley 9877.");
                            break;

                        case TipoIncapacidad.AccidenteLaboral:
                            // Los accidentes laborales son responsabilidad del INS
                            if (model.Entidad != EntidadIncapacidad.INS)
                                ModelState.AddModelError("Entidad",
                                    "Los accidentes laborales deben ser reportados al INS, no a la CCSS.");
                            break;
                    }
                }

                // Validar que la incapacidad no exceda 1 año
                if (dias > 365)
                    ModelState.AddModelError("FechaFin",
                        "La incapacidad no puede exceder 365 días. Para extensiones, registre una nueva.");
            }

            // Porcentaje de pago debe estar entre 0 y 100
            if (model.PorcentajePago < 0 || model.PorcentajePago > 100)
                ModelState.AddModelError("PorcentajePago",
                    "El porcentaje de pago debe estar entre 0% y 100%.");
        }
    }
}