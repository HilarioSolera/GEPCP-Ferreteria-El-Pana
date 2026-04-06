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
        private const decimal DiasLey = 14m;
        private const decimal SemanasLey = 50m;

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

                ViewBag.TotalSaldoPrestamos = (await _context.Prestamos
                    .Where(p => p.Activo)
                    .Select(p => p.Monto)
                    .ToListAsync()).Sum();

                ViewBag.TotalComisiones = (await _context.Comisiones
                    .Select(c => c.Monto)
                    .ToListAsync()).Sum();

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
                    ViewBag.NetoPeriodoActivo = (await _context.PlanillasEmpleado
                        .Where(pe => pe.PeriodoPagoId == periodoActivo.PeriodoPagoId)
                        .Select(pe => pe.NetoAPagar)
                        .ToListAsync()).Sum();

                    ViewBag.EmpleadosEnPlanilla = await _context.PlanillasEmpleado
                        .CountAsync(pe => pe.PeriodoPagoId == periodoActivo.PeriodoPagoId);

                    ViewBag.TotalHorasExtrasPeriodo = (await _context.HorasExtras
                        .Where(h => h.PeriodoPagoId == periodoActivo.PeriodoPagoId)
                        .Select(h => h.MontoTotal)
                        .ToListAsync()).Sum();

                    ViewBag.TotalCreditosActivos = (await _context.CreditosFerreteria
                        .Where(c => c.Activo)
                        .Select(c => c.Saldo)
                        .ToListAsync()).Sum();
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

                ViewBag.TotalMontoIncapacidades = (await _context.Incapacidades
                    .Where(i => i.FechaFin >= DateTime.Today &&
                                i.ResponsablePago == ResponsablePago.Patrono)
                    .Select(i => i.MontoTotal)
                    .ToListAsync()).Sum();

                // Últimos 6 períodos
                var ultimos6 = await _context.PeriodosPago
                    .AsNoTracking()
                    .Where(p => p.Estado == EstadoPeriodo.Cerrado)
                    .OrderByDescending(p => p.Anio)
                    .ThenByDescending(p => p.Mes)
                    .ThenByDescending(p => p.Quincena)
                    .Take(6)
                    .ToListAsync();

                ultimos6.Reverse();

                var graficoLabels = ultimos6.Select(p => p.Descripcion).ToList();
                var graficoNetos = new List<decimal>();

                foreach (var p in ultimos6)
                {
                    var neto = (await _context.PlanillasEmpleado
                        .Where(pe => pe.PeriodoPagoId == p.PeriodoPagoId)
                        .Select(pe => pe.NetoAPagar)
                        .ToListAsync()).Sum();
                    graficoNetos.Add(neto);
                }

                ViewBag.GraficoLabels = graficoLabels;
                ViewBag.GraficoNetos = graficoNetos;

                // Préstamos próximos a saldarse
                var prestamosRaw = await _context.Prestamos
                    .Include(p => p.Empleado)
                    .AsNoTracking()
                    .Where(p => p.Activo && p.CuotaMensual > 0)
                    .ToListAsync();

                ViewBag.PrestamosProximos = prestamosRaw
                    .Where(p => p.Monto <= p.CuotaMensual * 2)
                    .OrderBy(p => p.Monto)
                    .ToList();

                // ── VACACIONES - CORREGIDO ─────────────────────────────────────
                var empleadosActivos = await _context.Empleados
                    .AsNoTracking()
                    .Where(e => e.Activo)
                    .ToListAsync();

                // Traemos datos y sumamos en memoria
                var diasTomadosPorEmpleado = await _context.Vacaciones
                    .Where(v => v.Estado == EstadoVacacion.Aprobada &&
                                v.Tipo == TipoVacacion.ConPago)
                    .Select(v => new { v.EmpleadoId, v.DiasHabiles })
                    .ToListAsync();

                var diasTomadosDict = diasTomadosPorEmpleado
                    .GroupBy(x => x.EmpleadoId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(x => x.DiasHabiles)   // decimal
                    );

                // Vacaciones en alerta
                var vacacionesAlerta = new List<object>();
                foreach (var emp in empleadosActivos)
                {
                    var semanas = (decimal)(DateTime.Today - emp.FechaIngreso).TotalDays / 7;
                    var periodos = Math.Floor(semanas / SemanasLey);
                    var diasBase = periodos * DiasLey;

                    decimal tomados = diasTomadosDict.TryGetValue(emp.EmpleadoId, out var valor) ? valor : 0m;
                    var disponibles = diasBase - tomados;

                    if (disponibles >= DiasLey)
                    {
                        vacacionesAlerta.Add(new
                        {
                            EmpleadoId = emp.EmpleadoId,
                            Nombre = $"{emp.PrimerApellido} {emp.Nombre}",
                            Puesto = emp.Puesto,
                            DiasBase = diasBase,
                            Tomados = tomados,
                            Disponibles = disponibles,
                            FechaIngreso = emp.FechaIngreso
                        });
                    }
                }

                ViewBag.VacacionesAlerta = vacacionesAlerta
                    .OrderByDescending(x => ((dynamic)x).Disponibles)
                    .ToList();

                ViewBag.TotalVacAlerta = vacacionesAlerta.Count;

                // Reporte completo de vacaciones
                var reporteVacaciones = new List<object>();
                foreach (var emp in empleadosActivos.OrderBy(e => e.PrimerApellido))
                {
                    var semanas = (decimal)(DateTime.Today - emp.FechaIngreso).TotalDays / 7;
                    var periodos = Math.Floor(semanas / SemanasLey);
                    var diasBase = periodos * DiasLey;

                    decimal tomados = diasTomadosDict.TryGetValue(emp.EmpleadoId, out var valor) ? valor : 0m;
                    var disponibles = diasBase - tomados;

                    reporteVacaciones.Add(new
                    {
                        EmpleadoId = emp.EmpleadoId,
                        Nombre = $"{emp.PrimerApellido} {emp.SegundoApellido} {emp.Nombre}".Trim(),
                        Puesto = emp.Puesto,
                        DiasBase = diasBase,
                        Tomados = tomados,
                        Disponibles = disponibles,
                        FechaIngreso = emp.FechaIngreso,
                        Semanas = Math.Round(semanas, 1)
                    });
                }

                ViewBag.ReporteVacaciones = reporteVacaciones;

                _logger.LogInformation("Dashboard cargado por usuario: {U}", HttpContext.Session.GetString("Usuario"));

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el Dashboard");
                TempData["Error"] = "Ocurrió un error al cargar el Dashboard.";

                // Reset de ViewBags en caso de error
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
                ViewBag.GraficoLabels = new List<string>();
                ViewBag.GraficoNetos = new List<decimal>();
                ViewBag.PrestamosProximos = new List<Prestamo>();
                ViewBag.VacacionesAlerta = new List<object>();
                ViewBag.TotalVacAlerta = 0;
                ViewBag.ReporteVacaciones = new List<object>();

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