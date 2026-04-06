using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("Jefatura")]
    public class AuditoriaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditoriaController> _logger;

        public AuditoriaController(
            ApplicationDbContext context,
            ILogger<AuditoriaController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
     string? usuario,
     string? modulo,
     string? desde,
     string? hasta,
     int pagina = 1)
        {
            try
            {
                const int porPagina = 50;

                ViewBag.Usuario = usuario;
                ViewBag.Modulo = modulo;
                ViewBag.Desde = desde;
                ViewBag.Hasta = hasta;
                ViewBag.Pagina = pagina;

                if (string.IsNullOrWhiteSpace(usuario) &&
                    string.IsNullOrWhiteSpace(modulo) &&
                    string.IsNullOrWhiteSpace(desde) &&
                    string.IsNullOrWhiteSpace(hasta))
                {
                    ViewBag.Modulos = await ObtenerModulos();
                    ViewBag.TotalRegistros = 0;
                    ViewBag.TotalPaginas = 0;
                    return View(new List<RegistroAuditoria>());
                }

                var query = _context.RegistrosAuditoria
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(usuario))
                    query = query.Where(r =>
                        r.Usuario.ToLower().Contains(usuario.Trim().ToLower()));

                if (!string.IsNullOrWhiteSpace(modulo))
                    query = query.Where(r => r.Modulo == modulo);

                if (DateTime.TryParse(desde, out var fechaDesde))
                    query = query.Where(r => r.FechaHora >= fechaDesde);

                if (DateTime.TryParse(hasta, out var fechaHasta))
                    query = query.Where(r => r.FechaHora <= fechaHasta.AddDays(1));

                var total = await query.CountAsync();
                var totalPaginas = (int)Math.Ceiling(total / (double)porPagina);

                pagina = Math.Max(1, Math.Min(pagina, Math.Max(1, totalPaginas)));

                var registros = await query
                    .OrderByDescending(r => r.FechaHora)
                    .Skip((pagina - 1) * porPagina)
                    .Take(porPagina)
                    .ToListAsync();

                ViewBag.Modulos = await ObtenerModulos();
                ViewBag.TotalRegistros = total;
                ViewBag.TotalPaginas = totalPaginas;

                return View(registros);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar auditoría");
                TempData["Error"] = "Error al cargar la auditoría.";
                return View(new List<RegistroAuditoria>());
            }
        }

        private async Task<List<string>> ObtenerModulos()
        {
            return await _context.RegistrosAuditoria
                .AsNoTracking()
                .Select(r => r.Modulo)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
        }
    }
}