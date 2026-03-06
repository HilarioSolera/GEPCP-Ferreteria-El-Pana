using Microsoft.AspNetCore.Mvc;
using GEPCP_Ferreteria_El_Pana.Filters;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [CustomAuthorize("RRHH", "Jefatura")]
    public class ComisionesController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Usuario = HttpContext.Session.GetString("Usuario");
            ViewBag.Periodo = "Julio 2025 - Quincena 1";

            // Datos de prueba (después vendrán de la BD)
            var comisiones = new List<dynamic>
            {
                new { Chofer = "Juan Pérez", Entregas = 45, MontoCalculado = 225000m, MontoFinal = 225000m, Ajustado = false },
                new { Chofer = "Carlos Ramírez", Entregas = 32, MontoCalculado = 160000m, MontoFinal = 175000m, Ajustado = true }
            };

            ViewBag.Comisiones = comisiones;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GuardarReglaGlobal(string tipoCalculo, decimal valor)
        {
            TempData["Success"] = $"Regla global guardada: {tipoCalculo} = {valor} - GEPCP Ferretería El Pana";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AjustarComision(int id, decimal montoFinal)
        {
            TempData["Success"] = $"Comisión ajustada a ₡{montoFinal} correctamente";
            return RedirectToAction("Index");
        }
    }
}