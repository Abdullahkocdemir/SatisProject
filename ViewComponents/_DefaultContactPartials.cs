using Microsoft.AspNetCore.Mvc;

namespace SatışProject.ViewComponents
{
    public class _DefaultContactPartials : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
