using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Filters;
using System.Text.RegularExpressions;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH")]
    public class EmpleadosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmpleadosController> _logger;

        private static readonly List<string> DepartamentosValidos = new()
{
    "Administrativo", "Caja", "Ventas", "Bodega", "Conductores"
};

        public EmpleadosController(ApplicationDbContext context, ILogger<EmpleadosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private async Task<SelectList> ObtenerPuestosSelectList(string? selectedPuesto = null)
        {
            var puestos = await _context.Puestos
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
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
            SalarioBase = e.SalarioBase,
            Telefono = e.Telefono,
            CorreoElectronico = e.CorreoElectronico,
            NumeroCuenta = e.NumeroCuenta,
            FormaPago = e.FormaPago,
            Estado = e.Activo ? "Activo" : "Inactivo"
        };

        private void AplicarValidacionesPersonalizadas(EmpleadoViewModel model, int? idActual)
        {
            // Cédula
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

            // Nombre
            if (!string.IsNullOrWhiteSpace(model.Nombre) &&
                !Regex.IsMatch(model.Nombre.Trim(), @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s'\-]{2,100}$"))
                ModelState.AddModelError("Nombre",
                    "El nombre solo puede contener letras, espacios, apóstrofes y guiones.");

            // Primer Apellido
            if (!string.IsNullOrWhiteSpace(model.PrimerApellido) &&
                !Regex.IsMatch(model.PrimerApellido.Trim(), @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s'\-]{2,50}$"))
                ModelState.AddModelError("PrimerApellido",
                    "El primer apellido solo puede contener letras.");

            // Segundo Apellido
            if (!string.IsNullOrWhiteSpace(model.SegundoApellido) &&
                !Regex.IsMatch(model.SegundoApellido.Trim(), @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s'\-]{2,50}$"))
                ModelState.AddModelError("SegundoApellido",
                    "El segundo apellido solo puede contener letras.");

            // Departamento
            if (string.IsNullOrWhiteSpace(model.Departamento))
                ModelState.AddModelError("Departamento", "El departamento es obligatorio.");
            else if (!DepartamentosValidos.Contains(model.Departamento.Trim()))
                ModelState.AddModelError("Departamento", "Seleccioná un departamento válido.");

            // Fecha de ingreso
            if (model.FechaIngreso == default)
                ModelState.AddModelError("FechaIngreso", "La fecha de ingreso es obligatoria.");
            else if (model.FechaIngreso > DateTime.Today)
                ModelState.AddModelError("FechaIngreso",
                    "La fecha de ingreso no puede ser futura.");
            else if (model.FechaIngreso < new DateTime(1990, 1, 1))
                ModelState.AddModelError("FechaIngreso",
                    "La fecha de ingreso no puede ser anterior a 1990.");

            // Salario
            if (model.SalarioBase < 0)
                ModelState.AddModelError("SalarioBase", "El salario no puede ser negativo.");
            else if (model.SalarioBase > 9_999_999.99m)
                ModelState.AddModelError("SalarioBase",
                    "El salario excede el límite máximo permitido (₡9,999,999.99).");

            // Teléfono
            if (!EsTelefonoValido(model.Telefono))
                ModelState.AddModelError("Telefono",
                    "El teléfono solo puede contener números, guiones, espacios y paréntesis.");

            // Correo
            if (!string.IsNullOrWhiteSpace(model.CorreoElectronico) &&
                !Regex.IsMatch(model.CorreoElectronico.Trim(),
                    @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$"))
                ModelState.AddModelError("CorreoElectronico",
                    "El formato del correo electrónico no es válido.");

            // Número de cuenta
            if (!string.IsNullOrWhiteSpace(model.NumeroCuenta) &&
                model.NumeroCuenta.Trim().Length > 30)
                ModelState.AddModelError("NumeroCuenta",
                    "El número de cuenta no puede superar 30 caracteres.");
        }

        // ── INDEX ─────────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(string? busqueda)
        {
            try
            {
                ViewBag.Usuario = HttpContext.Session.GetString("Usuario");
                ViewBag.Busqueda = busqueda;

                var query = _context.Empleados.AsNoTracking().AsQueryable();

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
                        (e.Telefono != null && e.Telefono.Contains(termino))
                    );
                }

                var empleados = await query
                    .OrderBy(e => e.PrimerApellido)
                    .ThenBy(e => e.Nombre)
                    .ToListAsync();

                var viewModels = empleados.Select(e => new EmpleadoViewModel
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
                    SalarioBase = e.SalarioBase,
                    Telefono = e.Telefono,
                    CorreoElectronico = e.CorreoElectronico,
                    NumeroCuenta = e.NumeroCuenta,
                    FormaPago = e.FormaPago,
                    Estado = e.Activo ? "Activo" : "Inactivo"
                }).ToList();

                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar listado de empleados. Búsqueda: {B}", busqueda);
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

                return View(MapearAViewModel(empleado));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles del empleado ID: {Id}", id);
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
                    FechaIngreso = model.FechaIngreso,
                    SalarioBase = Math.Round(model.SalarioBase, 2),
                    Telefono = string.IsNullOrWhiteSpace(model.Telefono)
                                            ? null : model.Telefono.Trim(),
                    CorreoElectronico = string.IsNullOrWhiteSpace(model.CorreoElectronico)
                                            ? null : model.CorreoElectronico.Trim().ToLower(),
                    NumeroCuenta = string.IsNullOrWhiteSpace(model.NumeroCuenta)
                                            ? null : model.NumeroCuenta.Trim(),
                    FormaPago = model.FormaPago,
                    Activo = true
                };

                _context.Add(empleado);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Empleado creado: {Cedula} - {Nombre} {Apellido}",
                    empleado.Cedula, empleado.Nombre, empleado.PrimerApellido);
                TempData["Success"] = $"Empleado '{empleado.PrimerApellido} {empleado.Nombre}' creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al crear empleado: {Cedula}", model.Cedula);
                ModelState.AddModelError(string.Empty,
                    "Error al guardar en la base de datos. Verificá que la cédula no esté duplicada.");
                await CargarViewBagPuestos();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear empleado");
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Intentá de nuevo.");
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

                empleado.Nombre = SanitizarTexto(model.Nombre);
                empleado.PrimerApellido = SanitizarTexto(model.PrimerApellido);
                empleado.SegundoApellido = string.IsNullOrWhiteSpace(model.SegundoApellido)
                                                ? null : SanitizarTexto(model.SegundoApellido);
                empleado.Puesto = model.Puesto.Trim();
                empleado.Departamento = model.Departamento.Trim();
                empleado.TipoJornada = model.TipoJornada;
                empleado.FechaIngreso = model.FechaIngreso;
                empleado.SalarioBase = Math.Round(model.SalarioBase, 2);
                empleado.Telefono = string.IsNullOrWhiteSpace(model.Telefono)
                                                ? null : model.Telefono.Trim();
                empleado.CorreoElectronico = string.IsNullOrWhiteSpace(model.CorreoElectronico)
                                                ? null : model.CorreoElectronico.Trim().ToLower();
                empleado.NumeroCuenta = string.IsNullOrWhiteSpace(model.NumeroCuenta)
                                                ? null : model.NumeroCuenta.Trim();
                empleado.FormaPago = model.FormaPago;

                _context.Update(empleado);
                await _context.SaveChangesAsync();

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
                    "El registro fue modificado por otro usuario. Recargá e intentá de nuevo.");
                await CargarViewBagPuestos(model.Puesto);
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al editar empleado ID: {Id}", id);
                ModelState.AddModelError(string.Empty,
                    "Error al guardar en la base de datos. Verificá que la cédula no esté duplicada.");
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

                _logger.LogInformation("Empleado desactivado: ID {Id}", id);
                TempData["Success"] = $"Empleado '{empleado.PrimerApellido} {empleado.Nombre}' desactivado correctamente.";
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

                _logger.LogInformation("Empleado activado: ID {Id}", id);
                TempData["Success"] = $"Empleado '{empleado.PrimerApellido} {empleado.Nombre}' activado correctamente.";
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
                _logger.LogError(ex, "Error al cargar confirmación de eliminación, ID: {Id}", id);
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
                    TempData["Error"] = "No se puede eliminar un empleado con préstamos activos. Desactivalo en su lugar.";
                    return RedirectToAction(nameof(Index));
                }

                if (await _context.CreditosFerreteria.AnyAsync(c => c.EmpleadoId == id && c.Activo))
                {
                    TempData["Error"] = "No se puede eliminar un empleado con créditos de ferretería activos. Desactivalo en su lugar.";
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

                _logger.LogInformation("Empleado eliminado: ID {Id} - {Cedula}", id, empleado.Cedula);
                TempData["Success"] = $"Empleado '{empleado.PrimerApellido} {empleado.Nombre}' eliminado correctamente.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de BD al eliminar empleado ID: {Id}", id);
                TempData["Error"] = "No se puede eliminar el empleado porque tiene registros asociados. Desactivalo en su lugar.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al eliminar empleado ID: {Id}", id);
                TempData["Error"] = "Ocurrió un error inesperado al eliminar el empleado.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── API: salario sugerido por puesto ──────────────────────────────────

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
    }
}