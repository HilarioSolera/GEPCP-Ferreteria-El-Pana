using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Filters;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH")]
    public class PuestosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PuestosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Puestos
        public async Task<IActionResult> Index()
        {
            var puestos = await _context.Puestos
                .OrderBy(p => p.Nombre)
                .ToListAsync();
            return View(puestos);
        }

        // GET: /Puestos/Create
        public IActionResult Create()
        {
            return View(new Puesto { Activo = true });
        }

        // POST: /Puestos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Puesto model)
        {
            // Validar nombre único
            if (await _context.Puestos.AnyAsync(p => p.Nombre == model.Nombre))
            {
                ModelState.AddModelError("Nombre", "Ya existe un puesto con ese nombre.");
            }

            if (!ModelState.IsValid)
                return View(model);

            _context.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Puesto creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Puestos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var puesto = await _context.Puestos.FindAsync(id);
            if (puesto == null) return NotFound();

            return View(puesto);
        }

        // POST: /Puestos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Puesto model)
        {
            if (id != model.PuestoId) return NotFound();

            // Validar nombre único excluyendo el actual
            if (await _context.Puestos.AnyAsync(p => p.Nombre == model.Nombre && p.PuestoId != id))
            {
                ModelState.AddModelError("Nombre", "Ya existe otro puesto con ese nombre.");
            }

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Puesto actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Puestos.AnyAsync(p => p.PuestoId == id))
                    return NotFound();
                throw;
            }
        }

        // POST: /Puestos/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var puesto = await _context.Puestos.FindAsync(id);
            if (puesto == null)
            {
                TempData["Error"] = "Puesto no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar que no haya empleados usando este puesto
            var enUso = await _context.Empleados
                .AnyAsync(e => e.Puesto == puesto.Nombre);

            if (enUso)
            {
                TempData["Error"] = $"No se puede eliminar el puesto '{puesto.Nombre}' porque hay empleados asignados a él.";
                return RedirectToAction(nameof(Index));
            }

            _context.Puestos.Remove(puesto);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Puesto eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Puestos/Desactivar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            var puesto = await _context.Puestos.FindAsync(id);
            if (puesto == null)
            {
                TempData["Error"] = "Puesto no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            puesto.Activo = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Puesto '{puesto.Nombre}' desactivado correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}