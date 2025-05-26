using Microsoft.AspNetCore.Mvc;

namespace SatışProject.ViewComponents
{
    public class _DefaultWhyUsePartials : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
