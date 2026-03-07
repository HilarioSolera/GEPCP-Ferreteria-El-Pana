using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class PlanillaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlanillaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Planilla
        public async Task<IActionResult> Index()
        {
            ViewBag.Usuario = HttpContext.Session.GetString("Usuario");
            ViewBag.Rol = HttpContext.Session.GetString("Rol");

            // Obtener empleados activos con sus préstamos y comisiones
            var empleados = await _context.Empleados
                .Where(e => e.Activo)
                .Include(e => e.Prestamos.Where(p => p.Activo))
                .Include(e => e.Comisiones)
                .AsNoTracking()
                .ToListAsync();

            // Calcular detalle por empleado
            var detalle = empleados.Select(e =>
            {
                var salarioBase = e.SalarioBase;
                var comisiones = e.Comisiones.Sum(c => c.Monto);
                var cuotasPrestamos = e.Prestamos
                    .Where(p => p.Activo)
                    .Sum(p => p.CuotaMensual);
                var salarioNeto = salarioBase + comisiones - cuotasPrestamos;

                return new PlanillaDetalleViewModel
                {
                    EmpleadoId = e.EmpleadoId,
                    NombreCompleto = $"{e.PrimerApellido} {e.SegundoApellido} {e.Nombre}".Trim(),
                    Cedula = e.Cedula,
                    Puesto = e.Puesto,
                    SalarioBase = salarioBase,
                    Comisiones = comisiones,
                    CuotasPrestamos = cuotasPrestamos,
                    SalarioNeto = salarioNeto < 0 ? 0 : salarioNeto
                };
            }).ToList();

            // Totales generales
            ViewBag.TotalBruto = detalle.Sum(d => d.SalarioBase + d.Comisiones);
            ViewBag.TotalDeducciones = detalle.Sum(d => d.CuotasPrestamos);
            ViewBag.TotalNeto = detalle.Sum(d => d.SalarioNeto);
            ViewBag.TotalEmpleados = detalle.Count;
            ViewBag.Periodo = $"{DateTime.Now:MMMM yyyy}";

            // Verificar si ya existe planilla cerrada para este período
            var inicioPeriodo = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var finPeriodo = inicioPeriodo.AddMonths(1).AddDays(-1);
            var planillaCerrada = await _context.Planillas
                .AnyAsync(p => p.FechaInicio >= inicioPeriodo && p.Pagada);
            ViewBag.PeriodoCerrado = planillaCerrada;

            return View(detalle);
        }

        // POST: /Planilla/Calcular
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calcular()
        {
            // Verificar que haya empleados activos
            var hayEmpleados = await _context.Empleados.AnyAsync(e => e.Activo);
            if (!hayEmpleados)
            {
                TempData["Error"] = "No hay empleados activos para calcular la planilla.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Planilla calculada correctamente. Revisá el detalle antes de cerrar el período.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Planilla/CerrarPeriodo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarPeriodo()
        {
            var inicioPeriodo = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var finPeriodo = inicioPeriodo.AddMonths(1).AddDays(-1);

            // Verificar que no esté ya cerrado
            var yaCerrado = await _context.Planillas
                .AnyAsync(p => p.FechaInicio >= inicioPeriodo && p.Pagada);

            if (yaCerrado)
            {
                TempData["Error"] = "Este período ya fue cerrado anteriormente.";
                return RedirectToAction(nameof(Index));
            }

            // Obtener empleados activos con sus datos
            var empleados = await _context.Empleados
                .Where(e => e.Activo)
                .Include(e => e.Prestamos.Where(p => p.Activo))
                .Include(e => e.Comisiones)
                .ToListAsync();

            if (!empleados.Any())
            {
                TempData["Error"] = "No hay empleados activos para cerrar la planilla.";
                return RedirectToAction(nameof(Index));
            }

            // Crear registros de planilla para cada empleado
            foreach (var e in empleados)
            {
                var salarioBruto = e.SalarioBase + e.Comisiones.Sum(c => c.Monto);
                var deducciones = e.Prestamos.Where(p => p.Activo).Sum(p => p.CuotaMensual);
                var salarioNeto = salarioBruto - deducciones;

                var planilla = new Planilla
                {
                    EmpleadoId = e.EmpleadoId,
                    FechaInicio = inicioPeriodo,
                    FechaFin = finPeriodo,
                    SalarioBruto = salarioBruto,
                    Deducciones = deducciones,
                    SalarioNeto = salarioNeto < 0 ? 0 : salarioNeto,
                    Pagada = true
                };

                _context.Planillas.Add(planilla);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Período {DateTime.Now:MMMM yyyy} cerrado y consolidado correctamente. Los datos son ahora inmutables.";
            return RedirectToAction(nameof(Index));
        }
    }
}