using Microsoft.AspNetCore.Mvc;
using GEPCP_Ferreteria_El_Pana.Filters;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class PlanillaController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Usuario = HttpContext.Session.GetString("Usuario");
            ViewBag.Periodo = "Julio 2025 - Quincena 1";
            ViewBag.Estado = "Abierto";
            ViewBag.TotalBruto = 4850000m;
            ViewBag.TotalDeducciones = 1240000m;
            ViewBag.TotalNeto = 3610000m;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Calcular()
        {
            TempData["Success"] = "Planilla calculada correctamente - GEPCP Ferretería El Pana";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CerrarPeriodo()
        {
            TempData["Success"] = "¡Período cerrado e inmutable correctamente!";
            return RedirectToAction("Index");
        }
    }
}