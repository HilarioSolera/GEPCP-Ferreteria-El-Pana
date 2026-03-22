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

        public HomeController(
            ApplicationDbContext context,
            ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                ViewBag.Usuario = HttpContext.Session.GetString("Usuario");
                ViewBag.Rol = HttpContext.Session.GetString("Rol");

                ViewBag.TotalEmpleadosActivos = await _context.Empleados
                    .CountAsync(e => e.Activo);

                ViewBag.TotalSaldoPrestamos = await _context.Prestamos
                    .Where(p => p.Activo)
                    .SumAsync(p => (decimal?)p.Monto) ?? 0m;

                ViewBag.TotalComisiones = await _context.Comisiones
                    .SumAsync(c => (decimal?)c.Monto) ?? 0m;

                ViewBag.TotalPrestamosActivos = await _context.Prestamos
                    .CountAsync(p => p.Activo);

                ViewBag.TotalPuestosActivos = await _context.Puestos
                    .CountAsync(p => p.Activo);

                var periodoActivo = await _context.PeriodosPago
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Estado == EstadoPeriodo.Abierto);

                ViewBag.PeriodoActivo = periodoActivo;

                if (periodoActivo != null)
                {
                    ViewBag.NetoPeriodoActivo = await _context.PlanillasEmpleado
                        .Where(pe => pe.PeriodoPagoId == periodoActivo.PeriodoPagoId)
                        .SumAsync(pe => (decimal?)pe.NetoAPagar) ?? 0m;

                    ViewBag.EmpleadosEnPlanilla = await _context.PlanillasEmpleado
                        .CountAsync(pe => pe.PeriodoPagoId == periodoActivo.PeriodoPagoId);

                    ViewBag.TotalHorasExtrasPeriodo = await _context.HorasExtras
                        .Where(h => h.PeriodoPagoId == periodoActivo.PeriodoPagoId)
                        .SumAsync(h => (decimal?)h.MontoTotal) ?? 0m;

                    ViewBag.TotalCreditosActivos = await _context.CreditosFerreteria
                        .Where(c => c.Activo)
                        .SumAsync(c => (decimal?)c.Saldo) ?? 0m;
                }
                else
                {
                    ViewBag.NetoPeriodoActivo = 0m;
                    ViewBag.EmpleadosEnPlanilla = 0;
                    ViewBag.TotalHorasExtrasPeriodo = 0m;
                    ViewBag.TotalCreditosActivos = 0m;
                }

                ViewBag.TotalIncapacidadesActivas = await _context.Incapacidades
                    .CountAsync(i => i.FechaFin >= DateTime.Today);

                ViewBag.TotalMontoIncapacidades = await _context.Incapacidades
                    .Where(i =>
                        i.FechaFin >= DateTime.Today &&
                        i.ResponsablePago == ResponsablePago.Patrono)
                    .SumAsync(i => (decimal?)i.MontoTotal) ?? 0m;

                _logger.LogInformation("Dashboard cargado por usuario: {U}",
                    HttpContext.Session.GetString("Usuario"));

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el Dashboard");
                TempData["Error"] = "Ocurrió un error al cargar el Dashboard.";

                ViewBag.TotalEmpleadosActivos = 0;
                ViewBag.TotalSaldoPrestamos = 0m;
                ViewBag.TotalComisiones = 0m;
                ViewBag.TotalPrestamosActivos = 0;
                ViewBag.TotalPuestosActivos = 0;
                ViewBag.PeriodoActivo = null;
                ViewBag.NetoPeriodoActivo = 0m;
                ViewBag.EmpleadosEnPlanilla = 0;
                ViewBag.TotalHorasExtrasPeriodo = 0m;
                ViewBag.TotalCreditosActivos = 0m;
                ViewBag.TotalIncapacidadesActivas = 0;
                ViewBag.TotalMontoIncapacidades = 0m;

                return View();
            }
        }

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