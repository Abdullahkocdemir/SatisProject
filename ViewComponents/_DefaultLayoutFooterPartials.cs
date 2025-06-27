using Microsoft.AspNetCore.Mvc;

namespace SatışProject.ViewComponents
{
    public class _DefaultLayoutFooterPartials : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
