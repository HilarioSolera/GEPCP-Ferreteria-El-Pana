using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Services;
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

        // INDEX: historial de todos los empleados

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

        // HISTORIAL POR EMPLEADO

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

        // ENVIAR HISTORIAL POR EMAIL

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarPorEmail(int? empleadoId)
        {
            try
            {
                if (empleadoId == null || empleadoId <= 0)
                {
                    TempData["Error"] = "Empleado no especificado.";
                    return RedirectToAction(nameof(Index));
                }

                var empleado = await _context.Empleados
                    .FirstOrDefaultAsync(e => e.EmpleadoId == empleadoId);

                if (empleado == null)
                {
                    TempData["Error"] = "Empleado no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                var correo = empleado.CorreoElectronico;
                if (string.IsNullOrWhiteSpace(correo))
                {
                    TempData["Error"] = $"{empleado.PrimerApellido} {empleado.Nombre} no tiene correo registrado.";
                    return RedirectToAction(nameof(Empleado), new { id = empleadoId });
                }

                var historial = await _context.HistorialSalarios
                    .AsNoTracking()
                    .Where(h => h.EmpleadoId == empleadoId)
                    .OrderByDescending(h => h.FechaCambio)
                    .ToListAsync();

                var emailSvc = HttpContext.RequestServices
                    .GetRequiredService<EmailService>();

                var asunto = $"Historial de Salarios — {empleado.PrimerApellido} {empleado.Nombre}";

                var filasHistorial = historial.Select(h =>
                {
                    var diferencia = h.SalarioNuevo - h.SalarioAnterior;
                    var estilo = diferencia >= 0 ? "color:green;" : "color:red;";
                    return $@"<tr>
                        <td style='padding:8px;border-bottom:1px solid #eee;'>{h.FechaCambio:dd/MM/yyyy HH:mm}</td>
                        <td style='padding:8px;border-bottom:1px solid #eee;text-align:right;'>₡{h.SalarioAnterior:N0}</td>
                        <td style='padding:8px;border-bottom:1px solid #eee;text-align:right;'>₡{h.SalarioNuevo:N0}</td>
                        <td style='padding:8px;border-bottom:1px solid #eee;text-align:right;{estilo}font-weight:bold;'>
                            {(diferencia >= 0 ? "+" : "")}₡{diferencia:N0}
                        </td>
                        <td style='padding:8px;border-bottom:1px solid #eee;'>{h.ModificadoPor ?? "—"}</td>
                    </tr>";
                }).Aggregate("", (a, b) => a + b);

                var cuerpo = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;'>
    <div style='background:#1A1A2E;padding:20px;'>
        <h2 style='color:#FF7A00;margin:0;'>Ferretería El Pana SRL</h2>
        <p style='color:#888;margin:4px 0 0;font-size:13px;'>
            Departamento de Recursos Humanos
        </p>
    </div>
    <div style='padding:20px;border:1px solid #eee;'>
        <p>Estimado(a) <strong>{empleado.PrimerApellido} {empleado.Nombre}</strong>,</p>
        <p>Adjunto encontrará su historial de cambios de salario registrados en el sistema.</p>

        <table style='width:100%;border-collapse:collapse;margin:20px 0;'>
            <thead>
                <tr style='background:#FF7A00;color:white;'>
                    <th style='padding:8px;text-align:left;font-weight:bold;'>Fecha</th>
                    <th style='padding:8px;text-align:right;font-weight:bold;'>Salario Anterior</th>
                    <th style='padding:8px;text-align:right;font-weight:bold;'>Salario Nuevo</th>
                    <th style='padding:8px;text-align:right;font-weight:bold;'>Diferencia</th>
                    <th style='padding:8px;text-align:left;font-weight:bold;'>Modificado por</th>
                </tr>
            </thead>
            <tbody>
                {filasHistorial}
            </tbody>
        </table>

        <p style='color:#666;font-size:0.9rem;margin-top:20px;'>
            <i class='bi bi-info-circle'></i>
            Este es un documento informativo generado automáticamente por el sistema GEPCP.
        </p>
    </div>
    <div style='background:#f5f5f5;padding:15px;border-top:1px solid #ddd;font-size:0.85rem;color:#666;'>
        <p style='margin:0;'>
            © 2024 Ferretería El Pana SRL - Todos los derechos reservados<br/>
            Este correo fue enviado automáticamente. Por favor no responder.
        </p>
    </div>
</div>";

                await emailSvc.EnviarAsync(correo, asunto, cuerpo);

                TempData["Exito"] = $"Historial enviado exitosamente a {correo}.";
                return RedirectToAction(nameof(Empleado), new { id = empleadoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar historial por email");
                TempData["Error"] = "Error al enviar el correo. Intentá de nuevo.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}