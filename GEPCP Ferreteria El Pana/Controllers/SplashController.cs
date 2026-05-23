using Microsoft.AspNetCore.Mvc;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    public class SplashController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
