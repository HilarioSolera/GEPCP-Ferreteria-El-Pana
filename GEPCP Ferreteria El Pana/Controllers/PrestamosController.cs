using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH")]
    public class PrestamosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PrestamosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Prestamos
        public async Task<IActionResult> Index()
        {
            ViewBag.Usuario = HttpContext.Session.GetString("Usuario");

            var prestamos = await _context.Prestamos
                .Include(p => p.Empleado)
                .OrderByDescending(p => p.FechaPrestamo)
                .ToListAsync();

            return View(prestamos);
        }

        // GET: /Prestamos/Create
        public async Task<IActionResult> Create()
        {
            await CargarEmpleadosViewBag();
            return View(new PrestamoViewModel());
        }

        // POST: /Prestamos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PrestamoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarEmpleadosViewBag();
                return View(model);
            }

            var prestamo = new Prestamo
            {
                EmpleadoId = model.EmpleadoId,
                Monto = model.MontoPrincipal,
                FechaPrestamo = DateTime.Now,
                Interes = 0,
                Cuotas = model.CuotasTotal,
                CuotaMensual = model.CuotaMensual,
                Activo = true
            };

            _context.Add(prestamo);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Préstamo registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Prestamos/RegistrarAbono
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarAbono(int prestamoId, decimal monto)
        {
            var prestamo = await _context.Prestamos.FindAsync(prestamoId);

            if (prestamo == null)
            {
                TempData["Error"] = "Préstamo no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            if (!prestamo.Activo)
            {
                TempData["Error"] = "Este préstamo ya está cerrado.";
                return RedirectToAction(nameof(Index));
            }

            if (monto <= 0)
            {
                TempData["Error"] = "El monto del abono debe ser mayor a cero.";
                return RedirectToAction(nameof(Index));
            }

            if (monto > prestamo.Monto)
            {
                TempData["Error"] = $"El abono (₡{monto:N0}) no puede superar el saldo actual (₡{prestamo.Monto:N0}).";
                return RedirectToAction(nameof(Index));
            }

            // Aplicar abono
            prestamo.Monto -= monto;

            // Cerrar préstamo automáticamente si saldo llega a cero
            if (prestamo.Monto <= 0)
            {
                prestamo.Monto = 0;
                prestamo.Activo = false;
                TempData["Success"] = $"Abono de ₡{monto:N0} registrado. ¡Préstamo saldado completamente!";
            }
            else
            {
                TempData["Success"] = $"Abono de ₡{monto:N0} registrado. Saldo restante: ₡{prestamo.Monto:N0}.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: /Prestamos/Cerrar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cerrar(int id)
        {
            var prestamo = await _context.Prestamos.FindAsync(id);

            if (prestamo == null)
            {
                TempData["Error"] = "Préstamo no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            prestamo.Activo = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Préstamo cerrado manualmente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarEmpleadosViewBag()
        {
            ViewBag.Empleados = await _context.Empleados
                .Where(e => e.Activo)
                .OrderBy(e => e.PrimerApellido)
                .Select(e => new SelectListItem
                {
                    Value = e.EmpleadoId.ToString(),
                    Text = $"{e.PrimerApellido} {e.SegundoApellido} {e.Nombre} — {e.Cedula}"
                })
                .ToListAsync();
        }
    }
}