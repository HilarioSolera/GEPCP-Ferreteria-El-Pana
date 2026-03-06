using Microsoft.AspNetCore.Mvc;
using GEPCP_Ferreteria_El_Pana.Models;
using GEPCP_Ferreteria_El_Pana.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH")]
    public class EmpleadosController : Controller
    {
        private static List<string> ObtenerPuestos()   // ← static para eliminar warning
        {
            return new List<string>
            {
                "Encargada de RR.HH.", "Bodeguero", "Vendedor", "Gerente General",
                "Chofer de Entregas", "Cajero", "Asistente de Ventas", "Contador",
                "Atención al Cliente", "Jefe de Bodega", "Repositor", "Especialista en Pinturas"
            };
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Usuario = HttpContext.Session.GetString("Usuario");

            var empleados = new List<EmpleadoViewModel>
            {
                new EmpleadoViewModel { EmpleadoId = 1, Cedula = "116100256", Nombre = "Hilario", PrimerApellido = "Solera", SegundoApellido = "Meza", Puesto = "Encargada de RR.HH.", SalarioBase = 450000, Estado = "Activo" },
                new EmpleadoViewModel { EmpleadoId = 2, Cedula = "112340567", Nombre = "Juan", PrimerApellido = "Pérez", SegundoApellido = "", Puesto = "Chofer de Entregas", SalarioBase = 380000, Estado = "Activo" }
            };

            return View(empleados);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Puestos = new SelectList(ObtenerPuestos());
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(EmpleadoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Puestos = new SelectList(ObtenerPuestos());
                return View(model);
            }

            TempData["Success"] = "Empleado creado correctamente";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var empleado = new EmpleadoViewModel
            {
                EmpleadoId = id,
                Cedula = id == 1 ? "116100256" : "112340567",
                Nombre = id == 1 ? "Hilario" : "Juan",
                PrimerApellido = id == 1 ? "Solera" : "Pérez",
                SegundoApellido = id == 1 ? "Meza" : "",
                Puesto = id == 1 ? "Encargada de RR.HH." : "Chofer de Entregas",
                SalarioBase = id == 1 ? 450000 : 380000,
                Estado = "Activo"
            };

            ViewBag.Puestos = new SelectList(ObtenerPuestos(), empleado.Puesto);
            return View(empleado);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(EmpleadoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Puestos = new SelectList(ObtenerPuestos());
                return View(model);
            }

            TempData["Success"] = "Empleado actualizado correctamente";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Desactivar(int id)
        {
            TempData["Success"] = "Empleado desactivado correctamente";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            TempData["Success"] = "Empleado eliminado correctamente (demo)";
            return RedirectToAction("Index");
        }
    }
}