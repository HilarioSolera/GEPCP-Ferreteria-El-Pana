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

        public async Task<IActionResult> Index()
        {
            var comisiones = await _context.Comisiones.Include(c => c.Empleado).ToListAsync();
            return View(comisiones);
        }

        // GET: Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Empleados = await _context.Empleados.Select(e => new SelectListItem
            {
                Value = e.EmpleadoId.ToString(),
                Text = $"{e.Nombre} {e.PrimerApellido}"
            }).ToListAsync();

            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Comision model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Empleados = await _context.Empleados.Select(e => new SelectListItem
                {
                    Value = e.EmpleadoId.ToString(),
                    Text = $"{e.Nombre} {e.PrimerApellido}"
                }).ToListAsync();
                return View(model);
            }

            _context.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Comisión creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comision = await _context.Comisiones.FindAsync(id);
            if (comision == null)
            {
                return NotFound();
            }

            ViewBag.Empleados = await _context.Empleados.Select(e => new SelectListItem
            {
                Value = e.EmpleadoId.ToString(),
                Text = $"{e.Nombre} {e.PrimerApellido}",
                Selected = e.EmpleadoId == comision.EmpleadoId
            }).ToListAsync();

            return View(comision);
        }

        // POST: Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Comision model)
        {
            if (id != model.ComisionId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Empleados = await _context.Empleados.Select(e => new SelectListItem
                {
                    Value = e.EmpleadoId.ToString(),
                    Text = $"{e.Nombre} {e.PrimerApellido}"
                }).ToListAsync();
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
                {
                    return NotFound();
                }
                throw;
            }
        }

        // POST: Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var comision = await _context.Comisiones.FindAsync(id);
            if (comision == null)
            {
                return NotFound();
            }

            _context.Comisiones.Remove(comision);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Comisión eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}