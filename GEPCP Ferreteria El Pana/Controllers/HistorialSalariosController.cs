using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // ── INDEX: historial de todos los empleados ───────────────────────────

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

        // ── HISTORIAL POR EMPLEADO ────────────────────────────────────────────

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
    }
}