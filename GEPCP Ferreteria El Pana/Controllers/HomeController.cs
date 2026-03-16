using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── INDEX ─────────────────────────────────────────────────────────────

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        // ── DASHBOARD ─────────────────────────────────────────────────────────

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                ViewBag.Usuario = HttpContext.Session.GetString("Usuario");
                ViewBag.Rol = HttpContext.Session.GetString("Rol");

                // ── Ejecutar secuencialmente (EF Core no soporta paralelo en el mismo contexto)
                ViewBag.TotalEmpleadosActivos = await _context.Empleados
                    .CountAsync(e => e.Activo);

                ViewBag.TotalSaldoPrestamos = await _context.Prestamos
                    .Where(p => p.Activo)
                    .SumAsync(p => (decimal?)p.Monto) ?? 0m;

                ViewBag.TotalComisiones = await _context.Comisiones
                    .SumAsync(c => (decimal?)c.Monto) ?? 0m;

                ViewBag.TotalPlanillaEstimada = await _context.Empleados
                    .Where(e => e.Activo)
                    .SumAsync(e => (decimal?)e.SalarioBase) ?? 0m;

                ViewBag.TotalPrestamosActivos = await _context.Prestamos
                    .CountAsync(p => p.Activo);

                ViewBag.TotalPuestosActivos = await _context.Puestos
                    .CountAsync(p => p.Activo);

                _logger.LogInformation("Dashboard cargado por usuario: {Usuario}",
                    HttpContext.Session.GetString("Usuario"));

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el Dashboard");
                TempData["Error"] = "Ocurrió un error al cargar el Dashboard. Intentá de nuevo.";

                ViewBag.TotalEmpleadosActivos = 0;
                ViewBag.TotalSaldoPrestamos = 0m;
                ViewBag.TotalComisiones = 0m;
                ViewBag.TotalPlanillaEstimada = 0m;
                ViewBag.TotalPrestamosActivos = 0;
                ViewBag.TotalPuestosActivos = 0;

                return View();
            }
        }

        // ── ERROR ─────────────────────────────────────────────────────────────

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}