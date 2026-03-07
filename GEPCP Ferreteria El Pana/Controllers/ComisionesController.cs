using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH")]
    public class ComisionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ComisionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Comisiones
        public async Task<IActionResult> Index()
        {
            var comisiones = await _context.Comisiones
                .Include(c => c.Empleado)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            return View(comisiones);
        }

        // GET: /Comisiones/Create
        public async Task<IActionResult> Create()
        {
            await CargarEmpleadosViewBag();
            return View(new Comision { Fecha = DateTime.Now });
        }

        // POST: /Comisiones/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Comision model)
        {
            // Quitar validación de navegación (EF la resuelve por FK)
            ModelState.Remove("Empleado");

            if (!ModelState.IsValid)
            {
                await CargarEmpleadosViewBag();
                return View(model);
            }

            _context.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Comisión registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Comisiones/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var comision = await _context.Comisiones.FindAsync(id);
            if (comision == null) return NotFound();

            await CargarEmpleadosViewBag(comision.EmpleadoId);
            return View(comision);
        }

        // POST: /Comisiones/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Comision model)
        {
            if (id != model.ComisionId) return NotFound();

            ModelState.Remove("Empleado");

            if (!ModelState.IsValid)
            {
                await CargarEmpleadosViewBag(model.EmpleadoId);
                return View(model);
            }

            try
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Comisión actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Comisiones.AnyAsync(c => c.ComisionId == id))
                    return NotFound();
                throw;
            }
        }

        // POST: /Comisiones/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var comision = await _context.Comisiones.FindAsync(id);
            if (comision == null)
            {
                TempData["Error"] = "Comisión no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            _context.Comisiones.Remove(comision);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Comisión eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarEmpleadosViewBag(int? selectedId = null)
        {
            ViewBag.Empleados = await _context.Empleados
                .Where(e => e.Activo)
                .OrderBy(e => e.PrimerApellido)
                .Select(e => new SelectListItem
                {
                    Value = e.EmpleadoId.ToString(),
                    Text = $"{e.PrimerApellido} {e.SegundoApellido} {e.Nombre}",
                    Selected = e.EmpleadoId == selectedId
                })
                .ToListAsync();
        }
    }
}