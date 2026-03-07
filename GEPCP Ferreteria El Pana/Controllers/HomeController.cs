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

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.Usuario = HttpContext.Session.GetString("Usuario");
            ViewBag.Rol = HttpContext.Session.GetString("Rol");

            // KPIs reales desde la BD
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

            return View();
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