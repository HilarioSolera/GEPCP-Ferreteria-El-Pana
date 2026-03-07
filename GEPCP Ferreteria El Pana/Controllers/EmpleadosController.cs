using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Filters;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH")]
    public class EmpleadosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmpleadosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lista estática de puestos (migra a tabla más adelante)
        private static readonly List<string> PuestosDisponibles = new()
        {
            "Encargada de RR.HH.", "Bodeguero", "Vendedor", "Gerente General",
            "Chofer de Entregas", "Cajero", "Asistente de Ventas", "Contador",
            "Atención al Cliente", "Jefe de Bodega", "Repositor", "Especialista en Pinturas"
        };

        // GET: /Empleados (Listado)
        public async Task<IActionResult> Index()
        {
            ViewBag.Usuario = HttpContext.Session.GetString("Usuario");

            var empleados = await _context.Empleados
                .AsNoTracking()
                .OrderBy(e => e.EmpleadoId)  // Temporal por ID para evitar errores; cambia a PrimerApellido cuando arregles longitudes
                .ToListAsync();

            var viewModels = empleados.Select(e => new EmpleadoViewModel
            {
                EmpleadoId = e.EmpleadoId,
                Cedula = e.Cedula,
                Nombre = e.Nombre,
                PrimerApellido = e.PrimerApellido,
                SegundoApellido = e.SegundoApellido,
                Puesto = e.Puesto,
                SalarioBase = e.SalarioBase,
                Estado = e.Activo ? "Activo" : "Inactivo"
            }).ToList();

            return View(viewModels);
        }

        // GET: /Empleados/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.EmpleadoId == id);
            if (empleado == null)
            {
                return NotFound();
            }

            var model = new EmpleadoViewModel
            {
                EmpleadoId = empleado.EmpleadoId,
                Cedula = empleado.Cedula,
                Nombre = empleado.Nombre,
                PrimerApellido = empleado.PrimerApellido,
                SegundoApellido = empleado.SegundoApellido,
                Puesto = empleado.Puesto,
                SalarioBase = empleado.SalarioBase,
                Estado = empleado.Activo ? "Activo" : "Inactivo"
            };

            return View(model);
        }

        // GET: /Empleados/Create
        public IActionResult Create()
        {
            ViewBag.Puestos = new SelectList(PuestosDisponibles);
            return View(new EmpleadoViewModel());
        }

        // POST: /Empleados/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmpleadoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Puestos = new SelectList(PuestosDisponibles);
                return View(model);
            }

            var empleado = new Empleado
            {
                Cedula = model.Cedula,
                Nombre = model.Nombre,
                PrimerApellido = model.PrimerApellido,
                SegundoApellido = model.SegundoApellido,
                Puesto = model.Puesto,
                SalarioBase = model.SalarioBase,
                Activo = true
            };

            _context.Add(empleado);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Empleado creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Empleados/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null)
            {
                return NotFound();
            }

            var model = new EmpleadoViewModel
            {
                EmpleadoId = empleado.EmpleadoId,
                Cedula = empleado.Cedula,
                Nombre = empleado.Nombre,
                PrimerApellido = empleado.PrimerApellido,
                SegundoApellido = empleado.SegundoApellido,
                Puesto = empleado.Puesto,
                SalarioBase = empleado.SalarioBase,
                Estado = empleado.Activo ? "Activo" : "Inactivo"
            };

            ViewBag.Puestos = new SelectList(PuestosDisponibles, model.Puesto);
            return View(model);
        }

        // POST: /Empleados/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmpleadoViewModel model)
        {
            if (id != model.EmpleadoId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Puestos = new SelectList(PuestosDisponibles);
                return View(model);
            }

            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null)
            {
                return NotFound();
            }

            empleado.Cedula = model.Cedula;
            empleado.Nombre = model.Nombre;
            empleado.PrimerApellido = model.PrimerApellido;
            empleado.SegundoApellido = model.SegundoApellido;
            empleado.Puesto = model.Puesto;
            empleado.SalarioBase = model.SalarioBase;

            try
            {
                _context.Update(empleado);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Empleado actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Empleados.AnyAsync(e => e.EmpleadoId == id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        // POST: /Empleados/Desactivar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null)
            {
                TempData["Error"] = "Empleado no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            empleado.Activo = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Empleado desactivado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Empleados/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            if (empleado == null)
            {
                TempData["Error"] = "Empleado no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            _context.Empleados.Remove(empleado);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Empleado eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Empleados/Delete/5 (vista de confirmación para eliminar)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.EmpleadoId == id);
            if (empleado == null)
            {
                return NotFound();
            }

            var model = new EmpleadoViewModel
            {
                EmpleadoId = empleado.EmpleadoId,
                Cedula = empleado.Cedula,
                Nombre = empleado.Nombre,
                PrimerApellido = empleado.PrimerApellido,
                Puesto = empleado.Puesto,
                SalarioBase = empleado.SalarioBase
            };

            return View(model);
        }
    }
}