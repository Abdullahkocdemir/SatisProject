using Microsoft.AspNetCore.Mvc;

namespace SatışProject.Controllers
{
    public class DenemeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult UserManager()
        {
            return View();
        }
    }
}
