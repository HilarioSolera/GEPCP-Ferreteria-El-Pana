using GEPCP_Ferreteria_El_Pana.Filters;
using GEPCP_Ferreteria_El_Pana.Models;
using Microsoft.AspNetCore.Mvc;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH")]
    public class PrestamosController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Usuario = HttpContext.Session.GetString("Usuario");
            return View();
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PrestamoViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            TempData["Success"] = "Préstamo registrado correctamente";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegistrarAbono(int prestamoId, decimal monto)
        {
            TempData["Success"] = $"Abono de ₡{monto} registrado correctamente.";
            return RedirectToAction("Index");
        }
    }
}