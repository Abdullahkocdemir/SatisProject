using Microsoft.AspNetCore.Mvc;

namespace SatışProject.ViewComponents
{
    public class _DefaultFooterPartials : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
