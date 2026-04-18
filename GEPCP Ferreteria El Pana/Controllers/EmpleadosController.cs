using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Services;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class EmpleadosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmpleadosController> _logger;
        private readonly AuditoriaService _auditoria;
        private readonly HttpClient _httpClient;
        private static readonly List<string> DepartamentosValidos = Departamentos.Lista;

        public EmpleadosController(
            ApplicationDbContext context,
            ILogger<EmpleadosController> logger,
            AuditoriaService auditoria,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _auditoria = auditoria;
            _httpClient = httpClientFactory.CreateClient();
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private async Task<SelectList> ObtenerPuestosSelectList(string? selectedPuesto = null)
        {
            var puestos = await _context.Puestos
                .Where(p => p.Activo).OrderBy(p => p.Nombre)
                .Select(p => new { p.Nombre, p.SalarioBase })
                .ToListAsync();
            return new SelectList(puestos, "Nombre", "Nombre", selectedPuesto);
        }

        private static string SanitizarTexto(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return Regex.Replace(input.Trim(), @"[<>""'%;()&+\-\-]", string.Empty);
        }

        private static bool EsCedulaValida(string cedula) =>
            Regex.IsMatch(cedula.Trim(), @"^[\d\- ]{8,20}$");

        private static bool EsTelefonoValido(string? telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono)) return true;
            return Regex.IsMatch(telefono.Trim(), @"^[\d\- \+\(\)]{7,20}$");
        }

        private async Task CargarViewBagPuestos(string? selected = null)
        {
            ViewBag.Puestos = await ObtenerPuestosSelectList(selected);
        }

        private static EmpleadoViewModel MapearAViewModel(Empleado e) => new()
        {
            EmpleadoId = e.EmpleadoId,
            Cedula = e.Cedula,
            Nombre = e.Nombre,
            PrimerApellido = e.PrimerApellido,
            SegundoApellido = e.SegundoApellido,
            Puesto = e.Puesto,
            Departamento = e.Departamento,
            TipoJornada = e.TipoJornada,
            FechaIngreso = e.FechaIngreso,
            FechaNacimiento = e.FechaNacimiento,
            SalarioBase = e.SalarioBase,
            Telefono = e.Telefono,
            CorreoElectronico = e.CorreoElectronico,
            NumeroCuenta = e.NumeroCuenta,
            FormaPago = e.FormaPago,
            Estado = e.Activo ? "Activo" : "Inactivo",
            // Contrato
            TipoContrato = e.TipoContrato,
            FechaVencimientoContrato = e.FechaVencimientoContrato,
            // Dirección
            DireccionProvincia = e.DireccionProvincia,
            DireccionCanton = e.DireccionCanton,
            DireccionDistrito = e.DireccionDistrito,
            DireccionExacta = e.DireccionExacta,
            TipoPago = e.TipoPago,
            // Contacto emergencia
            ContactoEmergenciaNombre = e.ContactoEmergenciaNombre,
            ContactoEmergenciaTelefono = e.ContactoEmergenciaTelefono,
            // ISR créditos fiscales
            NumHijos = e.NumHijos,
            TieneConyuge = e.TieneConyuge
        };

        private void AplicarValidacionesPersonalizadas(EmpleadoViewModel model, int? idActual)
        {
            if (!string.IsNullOrWhiteSpace(model.Cedula))
            {
                if (!EsCedulaValida(model.Cedula))
                    ModelState.AddModelError("Cedula",
                        "La cédula solo puede contener números, guiones y espacios (8-20 caracteres).");
                else if (_context.Empleados.Any(e =>
                    e.Cedula == model.Cedula.Trim() &&
                    (idActual == null || e.EmpleadoId != idActual)))
                    ModelState.AddModelError("Cedula",
                        "Ya existe un empleado registrado con esa cédula.");
            }

            if (!string.IsNullOrWhiteSpace(model.Nombre) &&
                !Regex.IsMatch(model.Nombre.Trim(), @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s'\-]{2,100}$"))
                ModelState.AddModelError("Nombre",
                    "El nombre solo puede contener letras, espacios, apóstrofes y guiones.");

            if (!string.IsNullOrWhiteSpace(model.PrimerApellido) &&
                !Regex.IsMatch(model.PrimerApellido.Trim(), @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s'\-]{2,50}$"))
                ModelState.AddModelError("PrimerApellido", "El primer apellido solo puede contener letras.");

            if (!string.IsNullOrWhiteSpace(model.SegundoApellido) &&
                !Regex.IsMatch(model.SegundoApellido.Trim(), @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s'\-]{2,50}$"))
                ModelState.AddModelError("SegundoApellido", "El segundo apellido solo puede contener letras.");

            if (string.IsNullOrWhiteSpace(model.Departamento))
                ModelState.AddModelError("Departamento", "El departamento es obligatorio.");
            else if (!DepartamentosValidos.Contains(model.Departamento.Trim()))
                ModelState.AddModelError("Departamento", "Seleccioná un departamento válido.");

            if (model.FechaIngreso == default)
                ModelState.AddModelError("FechaIngreso", "La fecha de ingreso es obligatoria.");
            else if (model.FechaIngreso > DateTime.Today)
                ModelState.AddModelError("FechaIngreso", "La fecha de ingreso no puede ser futura.");
            else if (model.FechaIngreso < new DateTime(1990, 1, 1))
                ModelState.AddModelError("FechaIngreso", "La fecha de ingreso no puede ser anterior a 1990.");

            if (model.SalarioBase < 0)
                ModelState.AddModelError("SalarioBase", "El salario no puede ser negativo.");
            else if (model.SalarioBase > 9_999_999.99m)
                ModelState.AddModelError("SalarioBase", "El salario excede el límite máximo permitido.");

            if (!EsTelefonoValido(model.Telefono))
                ModelState.AddModelError("Telefono",
                    "El teléfono solo puede contener números, guiones, espacios y paréntesis.");

            if (!string.IsNullOrWhiteSpace(model.CorreoElectronico) &&
                !Regex.IsMatch(model.CorreoElectronico.Trim(),
                    @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$"))
                ModelState.AddModelError("CorreoElectronico", "El formato del correo no es válido.");

            if (!string.IsNullOrWhiteSpace(model.NumeroCuenta) &&
                model.NumeroCuenta.Trim().Length > 30)
                ModelState.AddModelError("NumeroCuenta", "El número de cuenta no puede superar 30 caracteres.");

            // Contrato a plazo fijo requiere fecha de vencimiento
            if (model.TipoContrato == TipoContrato.PlazoFijo &&
                !model.FechaVencimientoContrato.HasValue)
                ModelState.AddModelError("FechaVencimientoContrato",
                    "Para contrato a plazo fijo debés indicar la fecha de vencimiento.");

            if (model.FechaVencimientoContrato.HasValue &&
                model.FechaVencimientoContrato.Value < model.FechaIngreso)
                ModelState.AddModelError("FechaVencimientoContrato",
                    "La fecha de vencimiento no puede ser anterior a la fecha de ingreso.");

            if (!string.IsNullOrWhiteSpace(model.ContactoEmergenciaTelefono) &&
                !EsTelefonoValido(model.ContactoEmergenciaTelefono))
                ModelState.AddModelError("ContactoEmergenciaTelefono",
                    "El teléfono de emergencia solo puede contener números, guiones y paréntesis.");
        }

        // ── INDEX ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(string? busqueda, bool mostrarTodos = false, string filtroEstado = "activos")
        {
            try
            {
                ViewBag.Usuario = HttpContext.Session.GetString("Usuario");
                ViewBag.Busqueda = busqueda;
                ViewBag.MostrarTodos = mostrarTodos;
                ViewBag.FiltroEstado = filtroEstado;

                if (string.IsNullOrWhiteSpace(busqueda) && !mostrarTodos)
                    return View(new List<EmpleadoViewModel>());

                var query = _context.Empleados.AsNoTracking().AsQueryable();

                // Filtro por estado
                if (filtroEstado == "activos")
                    query = query.Where(e => e.Activo);
                else if (filtroEstado == "inactivos")
                    query = query.Where(e => !e.Activo);
                // "todos" → sin filtro

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var termino = busqueda.Trim().ToLower();
                    query = query.Where(e =>
                        e.Cedula.ToLower().Contains(termino) ||
                        e.Nombre.ToLower().Contains(termino) ||
                        e.PrimerApellido.ToLower().Contains(termino) ||
                        (e.SegundoApellido != null && e.SegundoApellido.ToLower().Contains(termino)) ||
                        e.Puesto.ToLower().Contains(termino) ||
                        e.Departamento.ToLower().Contains(termino) ||
                        (e.CorreoElectronico != null && e.CorreoElectronico.ToLower().Contains(termino)) ||
                        (e.Telefono != null && e.Telefono.Contains(termino)));
                }

                var empleados = await query
                    .OrderBy(e => e.PrimerApellido).ThenBy(e => e.Nombre)
                    .ToListAsync();

                var viewModels = empleados.Select(MapearAViewModel).ToList();
                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar empleados. Búsqueda: {B}", busqueda);
                TempData["Error"] = "Ocurrió un error al cargar los empleados. Intentá de nuevo.";
                return View(new List<EmpleadoViewModel>());
            }
        }

        // ── DETAILS ───────────────────────────────────────────────────────────

        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                if (id == null || id <= 0) return NotFound();

                var empleado = await _context.Empleados
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpleadoId == id);
                if (empleado == null) return NotFound();

                var prestamos = await _context.Prestamos
                    .Include(p => p.AbonosPrestamo).AsNoTracking()
                    .Where(p => p.EmpleadoId == id)
                    .OrderByDescending(p => p.FechaPrestamo).ToListAsync();

                var creditos = await _context.CreditosFerreteria
                    .Include(c => c.AbonosCreditoFerreteria).AsNoTracking()
                    .Where(c => c.EmpleadoId == id)
                    .OrderByDescending(c => c.FechaCredito).ToListAsync();

                ViewBag.Prestamos = prestamos;
                ViewBag.Creditos = creditos;
                ViewBag.Busqueda = Request.Query["busqueda"].ToString();

                return View(MapearAViewModel(empleado));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles empleado ID: {Id}", id);
                TempData["Error"] = "Ocurrió un error al cargar los detalles del empleado.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── CREATE ────────────────────────────────────────────────────────────

        public async Task<IActionResult> Create()
        {
            try
            {
                await CargarViewBagPuestos();
                return View(new EmpleadoViewModel { FechaIngreso = DateTime.Today });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de creación de empleado");
                TempData["Error"] = "Error al cargar el formulario. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmpleadoViewModel model)
        {
            try
            {
                AplicarValidacionesPersonalizadas(model, null);
                if (!ModelState.IsValid)
                {
                    await CargarViewBagPuestos();
                    return View(model);
                }

                var empleado = new Empleado
                {
                    Cedula = model.Cedula.Trim(),
                    Nombre = SanitizarTexto(model.Nombre),
                    PrimerApellido = SanitizarTexto(model.PrimerApellido),
                    SegundoApellido = string.IsNullOrWhiteSpace(model.SegundoApellido)
                                            ? null : SanitizarTexto(model.SegundoApellido),
                    Puesto = model.Puesto.Trim(),
                    Departamento = model.Departamento.Trim(),
                    TipoJornada = model.TipoJornada,
                    TipoPago = model.TipoPago,
                    FechaIngreso = model.FechaIngreso,
                    FechaNacimiento = model.FechaNacimiento,
                    SalarioBase = Math.Round(model.SalarioBase, 2),
                    Telefono = string.IsNullOrWhiteSpace(model.Telefono)
                                            ? null : model.Telefono.Trim(),
                    CorreoElectronico = string.IsNullOrWhiteSpace(model.CorreoElectronico)
                                            ? null : model.CorreoElectronico.Trim().ToLower(),
                    NumeroCuenta = string.IsNullOrWhiteSpace(model.NumeroCuenta)
                                            ? null : model.NumeroCuenta.Trim(),
                    FormaPago = model.FormaPago,
                    TipoContrato = model.TipoContrato,
                    FechaVencimientoContrato = model.TipoContrato == TipoContrato.Indefinido
                                            ? null : model.FechaVencimientoContrato,
                    DireccionProvincia = string.IsNullOrWhiteSpace(model.DireccionProvincia)
                                            ? null : SanitizarTexto(model.DireccionProvincia),
                    DireccionCanton = string.IsNullOrWhiteSpace(model.DireccionCanton)
                                            ? null : SanitizarTexto(model.DireccionCanton),
                    DireccionDistrito = string.IsNullOrWhiteSpace(model.DireccionDistrito)
                                            ? null : SanitizarTexto(model.DireccionDistrito),
                    DireccionExacta = string.IsNullOrWhiteSpace(model.DireccionExacta)
                                            ? null : SanitizarTexto(model.DireccionExacta),
                    ContactoEmergenciaNombre = string.IsNullOrWhiteSpace(model.ContactoEmergenciaNombre)
                                            ? null : SanitizarTexto(model.ContactoEmergenciaNombre),
                    ContactoEmergenciaTelefono = string.IsNullOrWhiteSpace(model.ContactoEmergenciaTelefono)
                                            ? null : model.ContactoEmergenciaTelefono.Trim(),
                    NumHijos = model.NumHijos,
                    TieneConyuge = model.TieneConyuge,
                    Activo = true
                };

                _context.Add(empleado);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Crear empleado", "Empleados",
                    $"Cédula: {empleado.Cedula} — {empleado.PrimerApellido} {empleado.Nombre}");

                _logger.LogInformation("Empleado creado: {Cedula} - {Nombre} {Apellido}",
                    empleado.Cedula, empleado.Nombre, empleado.PrimerApellido);

                // ── Aviso de antigüedad mínima ────────────────────────────────────
                var diasDesdeIngreso = (DateTime.Today - empleado.FechaIngreso).Days;
                if (diasDesdeIngreso < 15)
                    TempData["Warning"] =
                        $"Empleado '{empleado.PrimerApellido} {empleado.Nombre}' creado. " +
                        $"⚠️ Tiene {diasDesdeIngreso} día(s) de antigüedad — " +
                        "no será incluido en la planilla hasta completar 15 días.";
                else
                    TempData["Success"] =
                        $"Empleado '{empleado.PrimerApellido} {empleado.Nombre}' creado correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al crear empleado: {Cedula}", model.Cedula);
                ModelState.AddModelError(string.Empty,
                    "Error al guardar. Verificá que la cédula no esté duplicada.");
                await CargarViewBagPuestos();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear empleado");
                ModelState.AddModelError(string.Empty,
                    "Ocurrió un error inesperado. Intentá de nuevo.");
                await CargarViewBagPuestos();
                return View(model);
            }
        }
        // ── EDIT ──────────────────────────────────────────────────────────────

        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null || id <= 0) return NotFound();
                var empleado = await _context.Empleados.FindAsync(id);
                if (empleado == null) return NotFound();
                var model = MapearAViewModel(empleado);
                await CargarViewBagPuestos(model.Puesto);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de edición, ID: {Id}", id);
                TempData["Error"] = "Error al cargar el formulario de edición.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmpleadoViewModel model)
        {
            try
            {
                if (id != model.EmpleadoId) return NotFound();
                AplicarValidacionesPersonalizadas(model, id);
                if (!ModelState.IsValid)
                {
                    await CargarViewBagPuestos(model.Puesto);
                    return View(model);
                }

                var empleado = await _context.Empleados.FindAsync(id);
                if (empleado == null) return NotFound();

                var salarioNuevo = Math.Round(model.SalarioBase, 2);
                if (empleado.SalarioBase != salarioNuevo)
                {
                    _context.HistorialSalarios.Add(new HistorialSalario
                    {
                        EmpleadoId = empleado.EmpleadoId,
                        SalarioAnterior = empleado.SalarioBase,
                        SalarioNuevo = salarioNuevo,
                        FechaCambio = DateTime.Now,
                        ModificadoPor = HttpContext.Session.GetString("Usuario") ?? "Sistema"
                    });
                }

                empleado.Nombre = SanitizarTexto(model.Nombre);
                empleado.PrimerApellido = SanitizarTexto(model.PrimerApellido);
                empleado.SegundoApellido = string.IsNullOrWhiteSpace(model.SegundoApellido)
                                                      ? null : SanitizarTexto(model.SegundoApellido);
                empleado.TipoPago = model.TipoPago;
                empleado.Puesto = model.Puesto.Trim();
                empleado.Departamento = model.Departamento.Trim();
                empleado.TipoJornada = model.TipoJornada;
                empleado.FechaIngreso = model.FechaIngreso;
                empleado.FechaNacimiento = model.FechaNacimiento;
                empleado.SalarioBase = salarioNuevo;
                empleado.Telefono = string.IsNullOrWhiteSpace(model.Telefono)
                                                      ? null : model.Telefono.Trim();
                empleado.CorreoElectronico = string.IsNullOrWhiteSpace(model.CorreoElectronico)
                                                      ? null : model.CorreoElectronico.Trim().ToLower();
                empleado.NumeroCuenta = string.IsNullOrWhiteSpace(model.NumeroCuenta)
                                                      ? null : model.NumeroCuenta.Trim();
                empleado.FormaPago = model.FormaPago;
                // Contrato
                empleado.TipoContrato = model.TipoContrato;
                empleado.FechaVencimientoContrato = model.TipoContrato == TipoContrato.Indefinido
                                                      ? null : model.FechaVencimientoContrato;
                // Dirección
                empleado.DireccionProvincia = string.IsNullOrWhiteSpace(model.DireccionProvincia)
                                                      ? null : SanitizarTexto(model.DireccionProvincia);
                empleado.DireccionCanton = string.IsNullOrWhiteSpace(model.DireccionCanton)
                                                      ? null : SanitizarTexto(model.DireccionCanton);
                empleado.DireccionDistrito = string.IsNullOrWhiteSpace(model.DireccionDistrito)
                                                      ? null : SanitizarTexto(model.DireccionDistrito);
                empleado.DireccionExacta = string.IsNullOrWhiteSpace(model.DireccionExacta)
                                                      ? null : SanitizarTexto(model.DireccionExacta);
                // Contacto emergencia
                empleado.ContactoEmergenciaNombre = string.IsNullOrWhiteSpace(model.ContactoEmergenciaNombre)
                                                      ? null : SanitizarTexto(model.ContactoEmergenciaNombre);
                empleado.ContactoEmergenciaTelefono = string.IsNullOrWhiteSpace(model.ContactoEmergenciaTelefono)
                                                      ? null : model.ContactoEmergenciaTelefono.Trim();
                // ISR créditos fiscales
                empleado.NumHijos = model.NumHijos;
                empleado.TieneConyuge = model.TieneConyuge;

                _context.Update(empleado);
                await _context.SaveChangesAsync();

                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Editar empleado", "Empleados",
                    $"Cédula: {empleado.Cedula} — {empleado.PrimerApellido} {empleado.Nombre}");

                _logger.LogInformation("Empleado actualizado: ID {Id} - {Cedula}", id, empleado.Cedula);
                TempData["Success"] = $"Empleado '{empleado.PrimerApellido} {empleado.Nombre}' actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error de concurrencia al editar empleado ID: {Id}", id);
                if (!await _context.Empleados.AnyAsync(e => e.EmpleadoId == id))
                    return NotFound();
                ModelState.AddModelError(string.Empty,
                    "El registro fue modificado. Recargá e intentá de nuevo.");
                await CargarViewBagPuestos(model.Puesto);
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al editar empleado ID: {Id}", id);
                ModelState.AddModelError(string.Empty,
                    "Error al guardar. Verificá que la cédula no esté duplicada.");
                await CargarViewBagPuestos(model.Puesto);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al editar empleado ID: {Id}", id);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
                await CargarViewBagPuestos(model.Puesto);
                return View(model);
            }
        }

        // ── ACTIVAR / DESACTIVAR ──────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            try
            {
                var empleado = await _context.Empleados.FindAsync(id);
                if (empleado == null)
                {
                    TempData["Error"] = "Empleado no encontrado.";
                    return RedirectToAction(nameof(Index));
                }
                empleado.Activo = false;
                await _context.SaveChangesAsync();
                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Desactivar empleado", "Empleados",
                    $"Cédula: {empleado.Cedula} — {empleado.PrimerApellido} {empleado.Nombre}");
                _logger.LogInformation("Empleado desactivado: ID {Id}", id);
                TempData["Success"] = $"Empleado '{empleado.PrimerApellido} {empleado.Nombre}' desactivado.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar empleado ID: {Id}", id);
                TempData["Error"] = "Error al desactivar el empleado. Intentá de nuevo.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(int id)
        {
            try
            {
                var empleado = await _context.Empleados.FindAsync(id);
                if (empleado == null)
                {
                    TempData["Error"] = "Empleado no encontrado.";
                    return RedirectToAction(nameof(Index));
                }
                empleado.Activo = true;
                await _context.SaveChangesAsync();
                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Activar empleado", "Empleados",
                    $"Cédula: {empleado.Cedula} — {empleado.PrimerApellido} {empleado.Nombre}");
                _logger.LogInformation("Empleado activado: ID {Id}", id);
                TempData["Success"] = $"Empleado '{empleado.PrimerApellido} {empleado.Nombre}' activado.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar empleado ID: {Id}", id);
                TempData["Error"] = "Error al activar el empleado. Intentá de nuevo.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        public async Task<IActionResult> Delete(int? id)
        {
            try
            {
                if (id == null || id <= 0) return NotFound();
                var empleado = await _context.Empleados
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpleadoId == id);
                if (empleado == null) return NotFound();
                return View(MapearAViewModel(empleado));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar confirmación eliminación ID: {Id}", id);
                TempData["Error"] = "Error al cargar la confirmación.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                if (await _context.Prestamos.AnyAsync(p => p.EmpleadoId == id && p.Activo))
                {
                    TempData["Error"] = "No se puede eliminar un empleado con préstamos activos.";
                    return RedirectToAction(nameof(Index));
                }
                if (await _context.CreditosFerreteria.AnyAsync(c => c.EmpleadoId == id && c.Activo))
                {
                    TempData["Error"] = "No se puede eliminar un empleado con créditos activos.";
                    return RedirectToAction(nameof(Index));
                }
                var empleado = await _context.Empleados.FindAsync(id);
                if (empleado == null)
                {
                    TempData["Error"] = "Empleado no encontrado.";
                    return RedirectToAction(nameof(Index));
                }
                _context.Empleados.Remove(empleado);
                await _context.SaveChangesAsync();
                await _auditoria.RegistrarAsync(
                    HttpContext.Session.GetString("Usuario") ?? "",
                    "Eliminar empleado", "Empleados",
                    $"Cédula: {empleado.Cedula} — {empleado.PrimerApellido} {empleado.Nombre}");
                _logger.LogInformation("Empleado eliminado: ID {Id} - {Cedula}", id, empleado.Cedula);
                TempData["Success"] = $"Empleado '{empleado.PrimerApellido} {empleado.Nombre}' eliminado.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al eliminar empleado ID: {Id}", id);
                TempData["Error"] = "No se puede eliminar el empleado porque tiene registros asociados.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al eliminar empleado ID: {Id}", id);
                TempData["Error"] = "Ocurrió un error inesperado al eliminar el empleado.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ── API: salario por puesto ────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> ObtenerSalarioPuesto(string nombre)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                    return Json(new { salario = (decimal?)null });
                var puesto = await _context.Puestos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Nombre == nombre && p.Activo);
                return Json(new { salario = puesto?.SalarioBase });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener salario del puesto: {Nombre}", nombre);
                return Json(new { salario = (decimal?)null });
            }
        }

        // ── API: consulta cédula gometa.org ───────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> ConsultarCedulaTSE(string cedula)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cedula))
                    return Json(new { exito = false, mensaje = "Cédula vacía." });

                var cedulaLimpia = cedula.Trim().Replace("-", "").Replace(" ", "");
                var url = $"https://apis.gometa.org/cedulas/{cedulaLimpia}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return Json(new { exito = false, mensaje = "Cédula no encontrada." });

                var json = await response.Content.ReadAsStringAsync();
                var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                var resultcount = data.TryGetProperty("resultcount", out var rc) ? rc.GetInt32() : 0;

                if (resultcount == 0)
                    return Json(new { exito = false, mensaje = "Cédula no encontrada en el registro." });

                var result = data.GetProperty("results")[0];
                var nombre = ObtenerCampo(result, "firstname1");
                var nombre2 = ObtenerCampo(result, "firstname2");
                var apellido1 = ObtenerCampo(result, "lastname1");
                var apellido2 = ObtenerCampo(result, "lastname2");

                return Json(new
                {
                    exito = true,
                    nombre = ToTitleCase(nombre),
                    nombre2 = ToTitleCase(nombre2),
                    apellido1 = ToTitleCase(apellido1),
                    apellido2 = ToTitleCase(apellido2)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar gometa. Cédula: {C}", cedula);
                return Json(new { exito = false, mensaje = "Error al consultar. Ingresá los datos manualmente." });
            }
        }

        // ── HELPERS PRIVADOS ──────────────────────────────────────────────────

        private static string ObtenerCampo(System.Text.Json.JsonElement data, string campo)
        {
            if (data.TryGetProperty(campo, out var prop))
                return prop.GetString() ?? string.Empty;
            return string.Empty;
        }

        private static string ToTitleCase(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return System.Globalization.CultureInfo.CurrentCulture
                .TextInfo.ToTitleCase(input.ToLower());
        }

        // ── HISTORIAL PDF ────────────────────────────────────────────────────

        public async Task<IActionResult> DescargarHistorial(int id)
        {
            try
            {
                var empleado = await _context.Empleados.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmpleadoId == id);
                if (empleado == null) return NotFound();

                var planillas = await _context.PlanillasEmpleado
                    .Include(pe => pe.PeriodoPago)
                    .AsNoTracking()
                    .Where(pe => pe.EmpleadoId == id)
                    .OrderByDescending(pe => pe.PeriodoPago.FechaInicio)
                    .ToListAsync();

                var horasExtras = await _context.HorasExtras
                    .Include(h => h.PeriodoPago)
                    .AsNoTracking()
                    .Where(h => h.EmpleadoId == id)
                    .OrderByDescending(h => h.Fecha)
                    .ToListAsync();

                var vacaciones = await _context.Vacaciones
                    .AsNoTracking()
                    .Where(v => v.EmpleadoId == id)
                    .OrderByDescending(v => v.FechaInicio)
                    .ToListAsync();

                var incapacidades = await _context.Incapacidades
                    .AsNoTracking()
                    .Where(i => i.EmpleadoId == id)
                    .OrderByDescending(i => i.FechaInicio)
                    .ToListAsync();

                var aguinaldos = await _context.Aguinaldos
                    .AsNoTracking()
                    .Where(a => a.EmpleadoId == id)
                    .OrderByDescending(a => a.Anio)
                    .ToListAsync();

                var comisiones = await _context.Comisiones
                    .AsNoTracking()
                    .Where(c => c.EmpleadoId == id)
                    .OrderByDescending(c => c.Fecha)
                    .ToListAsync();

                var prestamos = await _context.Prestamos
                    .Include(p => p.AbonosPrestamo)
                    .AsNoTracking()
                    .Where(p => p.EmpleadoId == id)
                    .OrderByDescending(p => p.FechaPrestamo)
                    .ToListAsync();

                var creditos = await _context.CreditosFerreteria
                    .Include(c => c.AbonosCreditoFerreteria)
                    .AsNoTracking()
                    .Where(c => c.EmpleadoId == id)
                    .OrderByDescending(c => c.FechaCredito)
                    .ToListAsync();

                var feriados = await _context.PagosFeriado
                    .Include(f => f.Feriado)
                    .Include(f => f.PeriodoPago)
                    .AsNoTracking()
                    .Where(f => f.EmpleadoId == id)
                    .OrderByDescending(f => f.Feriado.Fecha)
                    .ToListAsync();

                var pdf = GenerarHistorialPdf(empleado, planillas, horasExtras, vacaciones,
                    incapacidades, aguinaldos, comisiones, prestamos, creditos, feriados);

                var nombreArchivo = $"Historial_{empleado.PrimerApellido}_{empleado.Nombre}_{DateTime.Now:yyyyMMdd}.pdf";
                return File(pdf, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar historial empleado ID: {Id}", id);
                TempData["Error"] = "Error al generar el historial del empleado.";
                return RedirectToAction(nameof(Index));
            }
        }

        private byte[] GenerarHistorialPdf(
            Empleado emp,
            List<PlanillaEmpleado> planillas,
            List<HorasExtras> horasExtras,
            List<Vacacion> vacaciones,
            List<Incapacidad> incapacidades,
            List<Aguinaldo> aguinaldos,
            List<Comision> comisiones,
            List<Prestamo> prestamos,
            List<CreditoFerreteria> creditos,
            List<PagoFeriado> feriados)
        {
            var naranja = Color.FromHex("FF7A00");
            var grisClaro = Color.FromHex("F5F5F5");
            var oscuro = Color.FromHex("1A1A2E");

            return QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.Letter);
                    page.MarginHorizontal(30);
                    page.MarginVertical(25);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // ── HEADER ──
                    page.Header().Column(col =>
                    {
                        col.Item().Background(oscuro).Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("GEPCP Ferretería El Pana").FontSize(14).Bold().FontColor(naranja);
                                c.Item().Text("Historial Laboral del Empleado").FontSize(10).FontColor(QuestPDF.Helpers.Colors.White);
                                c.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                            });
                        });

                        // Datos del empleado
                        col.Item().PaddingTop(8).Background(grisClaro).Border(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(8).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}").Bold().FontSize(11);
                                c.Item().Text($"Cédula: {emp.Cedula}  |  Puesto: {emp.Puesto}  |  Depto: {emp.Departamento}");
                                c.Item().Text($"Ingreso: {emp.FechaIngreso:dd/MM/yyyy}  |  Jornada: {(emp.TipoJornada == TipoJornada.Completa ? "Completa" : "Media")}  |  Estado: {(emp.Activo ? "Activo" : "Inactivo")}");
                                c.Item().Text($"Salario Base: ₡{emp.SalarioBase:N0}  |  Valor Hora: ₡{emp.ValorHora:N2}");
                            });
                        });

                        col.Item().PaddingBottom(5);
                    });

                    // ── CONTENT ──
                    page.Content().Column(col =>
                    {
                        // ── PLANILLAS ──
                        if (planillas.Any())
                        {
                            SeccionTitulo(col, "Planillas", naranja);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2.5f); // Periodo
                                    c.RelativeColumn(1.2f); // Devengado
                                    c.RelativeColumn(1.2f); // CCSS
                                    c.RelativeColumn(1.2f); // ISR
                                    c.RelativeColumn(1.2f); // Deducciones
                                    c.RelativeColumn(1.2f); // Neto
                                });
                                EncabezadoTabla(table, naranja, "Período", "Devengado", "CCSS", "ISR", "Deducciones", "Neto");
                                bool alt = false;
                                foreach (var p in planillas)
                                {
                                    var bg = alt ? grisClaro : QuestPDF.Helpers.Colors.White;
                                    CeldaTabla(table, bg, $"{p.PeriodoPago.FechaInicio:dd/MM} - {p.PeriodoPago.FechaFin:dd/MM/yyyy}");
                                    CeldaTabla(table, bg, $"₡{p.TotalDevengado:N0}");
                                    CeldaTabla(table, bg, $"₡{p.DeduccionCCSS:N0}");
                                    CeldaTabla(table, bg, $"₡{p.DeduccionRenta:N0}");
                                    CeldaTabla(table, bg, $"₡{p.TotalDeducciones:N0}");
                                    CeldaTabla(table, bg, $"₡{p.NetoAPagar:N0}");
                                    alt = !alt;
                                }
                            });
                            col.Item().PaddingBottom(3).AlignRight().Text($"Total Neto Histórico: ₡{planillas.Sum(p => p.NetoAPagar):N0}").Bold().FontSize(8);
                        }

                        // ── HORAS EXTRAS ──
                        if (horasExtras.Any())
                        {
                            SeccionTitulo(col, "Horas Extras", naranja);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2f);
                                    c.RelativeColumn(1f);
                                    c.RelativeColumn(1f);
                                    c.RelativeColumn(1.2f);
                                });
                                EncabezadoTabla(table, naranja, "Fecha", "Horas", "Porcentaje", "Monto");
                                bool alt = false;
                                foreach (var h in horasExtras)
                                {
                                    var bg = alt ? grisClaro : QuestPDF.Helpers.Colors.White;
                                    CeldaTabla(table, bg, h.Fecha.ToString("dd/MM/yyyy"));
                                    CeldaTabla(table, bg, h.TotalHoras.ToString("N1"));
                                    CeldaTabla(table, bg, $"x{h.Porcentaje:N1}");
                                    CeldaTabla(table, bg, $"₡{h.MontoTotal:N0}");
                                    alt = !alt;
                                }
                            });
                            col.Item().PaddingBottom(3).AlignRight().Text($"Total: ₡{horasExtras.Sum(h => h.MontoTotal):N0}").Bold().FontSize(8);
                        }

                        // ── VACACIONES ──
                        if (vacaciones.Any())
                        {
                            SeccionTitulo(col, "Vacaciones", naranja);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2f);
                                    c.RelativeColumn(1f);
                                    c.RelativeColumn(1f);
                                    c.RelativeColumn(1.2f);
                                });
                                EncabezadoTabla(table, naranja, "Período", "Días", "Estado", "Monto Ded.");
                                bool alt = false;
                                foreach (var v in vacaciones)
                                {
                                    var bg = alt ? grisClaro : QuestPDF.Helpers.Colors.White;
                                    CeldaTabla(table, bg, $"{v.FechaInicio:dd/MM} - {v.FechaFin:dd/MM/yyyy}");
                                    CeldaTabla(table, bg, v.DiasHabiles.ToString("N1"));
                                    CeldaTabla(table, bg, v.Estado.ToString());
                                    CeldaTabla(table, bg, $"₡{v.MontoDeducido:N0}");
                                    alt = !alt;
                                }
                            });
                            col.Item().PaddingBottom(3).AlignRight().Text($"Total días: {vacaciones.Where(v => v.Estado == EstadoVacacion.Aprobada).Sum(v => v.DiasHabiles):N1}").Bold().FontSize(8);
                        }

                        // ── INCAPACIDADES ──
                        if (incapacidades.Any())
                        {
                            SeccionTitulo(col, "Incapacidades", naranja);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2f);
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(0.8f);
                                    c.RelativeColumn(1.2f);
                                });
                                EncabezadoTabla(table, naranja, "Período", "Tipo", "Días", "Monto");
                                bool alt = false;
                                foreach (var i in incapacidades)
                                {
                                    var bg = alt ? grisClaro : QuestPDF.Helpers.Colors.White;
                                    CeldaTabla(table, bg, $"{i.FechaInicio:dd/MM} - {i.FechaFin:dd/MM/yyyy}");
                                    CeldaTabla(table, bg, i.TipoIncapacidad);
                                    CeldaTabla(table, bg, i.TotalDias.ToString());
                                    CeldaTabla(table, bg, $"₡{i.MontoTotal:N0}");
                                    alt = !alt;
                                }
                            });
                        }

                        // ── AGUINALDOS ──
                        if (aguinaldos.Any())
                        {
                            SeccionTitulo(col, "Aguinaldos", naranja);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1f);
                                    c.RelativeColumn(2f);
                                    c.RelativeColumn(1.2f);
                                });
                                EncabezadoTabla(table, naranja, "Año", "Período", "Monto");
                                bool alt = false;
                                foreach (var a in aguinaldos)
                                {
                                    var bg = alt ? grisClaro : QuestPDF.Helpers.Colors.White;
                                    CeldaTabla(table, bg, a.Anio.ToString());
                                    CeldaTabla(table, bg, $"{a.FechaInicio:dd/MM/yyyy} - {a.FechaFin:dd/MM/yyyy}");
                                    CeldaTabla(table, bg, $"₡{a.MontoTotal:N0}");
                                    alt = !alt;
                                }
                            });
                            col.Item().PaddingBottom(3).AlignRight().Text($"Total: ₡{aguinaldos.Sum(a => a.MontoTotal):N0}").Bold().FontSize(8);
                        }

                        // ── COMISIONES ──
                        if (comisiones.Any())
                        {
                            SeccionTitulo(col, "Comisiones", naranja);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(2.5f);
                                    c.RelativeColumn(1.2f);
                                });
                                EncabezadoTabla(table, naranja, "Fecha", "Descripción", "Monto");
                                bool alt = false;
                                foreach (var c in comisiones)
                                {
                                    var bg = alt ? grisClaro : QuestPDF.Helpers.Colors.White;
                                    CeldaTabla(table, bg, c.Fecha.ToString("dd/MM/yyyy"));
                                    CeldaTabla(table, bg, c.Descripcion);
                                    CeldaTabla(table, bg, $"₡{c.Monto:N0}");
                                    alt = !alt;
                                }
                            });
                            col.Item().PaddingBottom(3).AlignRight().Text($"Total: ₡{comisiones.Sum(c => c.Monto):N0}").Bold().FontSize(8);
                        }

                        // ── PRÉSTAMOS ──
                        if (prestamos.Any())
                        {
                            SeccionTitulo(col, "Préstamos", naranja);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(1.2f);
                                    c.RelativeColumn(1f);
                                    c.RelativeColumn(1.2f);
                                    c.RelativeColumn(1f);
                                });
                                EncabezadoTabla(table, naranja, "Fecha", "Monto", "Cuotas", "Cuota Mensual", "Estado");
                                bool alt = false;
                                foreach (var p in prestamos)
                                {
                                    var bg = alt ? grisClaro : QuestPDF.Helpers.Colors.White;
                                    CeldaTabla(table, bg, p.FechaPrestamo.ToString("dd/MM/yyyy"));
                                    CeldaTabla(table, bg, $"₡{p.MontoOriginal:N0}");
                                    CeldaTabla(table, bg, p.Cuotas.ToString());
                                    CeldaTabla(table, bg, $"₡{p.CuotaMensual:N0}");
                                    CeldaTabla(table, bg, p.Activo ? "Activo" : "Pagado");
                                    alt = !alt;
                                }
                            });
                        }

                        // ── CRÉDITOS FERRETERÍA ──
                        if (creditos.Any())
                        {
                            SeccionTitulo(col, "Créditos Ferretería", naranja);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(2f);
                                    c.RelativeColumn(1.2f);
                                    c.RelativeColumn(1f);
                                });
                                EncabezadoTabla(table, naranja, "Fecha", "Descripción", "Monto", "Estado");
                                bool alt = false;
                                foreach (var c in creditos)
                                {
                                    var bg = alt ? grisClaro : QuestPDF.Helpers.Colors.White;
                                    CeldaTabla(table, bg, c.FechaCredito.ToString("dd/MM/yyyy"));
                                    CeldaTabla(table, bg, c.Descripcion ?? "—");
                                    CeldaTabla(table, bg, $"₡{c.MontoTotal:N0}");
                                    CeldaTabla(table, bg, c.Activo ? "Activo" : "Pagado");
                                    alt = !alt;
                                }
                            });
                        }

                        // ── FERIADOS ──
                        if (feriados.Any())
                        {
                            SeccionTitulo(col, "Feriados Pagados", naranja);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(2f);
                                    c.RelativeColumn(1f);
                                    c.RelativeColumn(1.2f);
                                });
                                EncabezadoTabla(table, naranja, "Fecha", "Feriado", "Trabajado", "Monto");
                                bool alt = false;
                                foreach (var f in feriados)
                                {
                                    var bg = alt ? grisClaro : QuestPDF.Helpers.Colors.White;
                                    CeldaTabla(table, bg, f.Feriado.Fecha.ToString("dd/MM/yyyy"));
                                    CeldaTabla(table, bg, f.Feriado.Nombre);
                                    CeldaTabla(table, bg, f.Trabajado ? "Sí" : "No");
                                    CeldaTabla(table, bg, $"₡{f.MontoTotal:N0}");
                                    alt = !alt;
                                }
                            });
                            col.Item().PaddingBottom(3).AlignRight().Text($"Total: ₡{feriados.Sum(f => f.MontoTotal):N0}").Bold().FontSize(8);
                        }

                        // Sin datos
                        if (!planillas.Any() && !horasExtras.Any() && !vacaciones.Any() &&
                            !incapacidades.Any() && !aguinaldos.Any() && !comisiones.Any() &&
                            !prestamos.Any() && !creditos.Any() && !feriados.Any())
                        {
                            col.Item().PaddingTop(30).AlignCenter().Text("No se encontraron registros para este empleado.").FontSize(11).Italic();
                        }
                    });

                    // ── FOOTER ──
                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Página ").FontSize(7);
                        t.CurrentPageNumber().FontSize(7);
                        t.Span(" de ").FontSize(7);
                        t.TotalPages().FontSize(7);
                    });
                });
            }).GeneratePdf();
        }

        private static void SeccionTitulo(ColumnDescriptor col, string titulo, Color color)
        {
            col.Item().PaddingTop(8).PaddingBottom(3).BorderBottom(2).BorderColor(color)
                .Text(titulo).Bold().FontSize(10).FontColor(color);
        }

        private static void EncabezadoTabla(TableDescriptor table, Color color, params string[] columnas)
        {
            foreach (var c in columnas)
            {
                table.Cell().Background(color).Padding(3)
                    .Text(c).FontSize(7).Bold().FontColor(QuestPDF.Helpers.Colors.White);
            }
        }

        private static void CeldaTabla(TableDescriptor table, Color bg, string texto)
        {
            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2)
                .Padding(3).Text(texto).FontSize(7);
        }
    }
}