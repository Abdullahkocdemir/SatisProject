using Microsoft.AspNetCore.Mvc;

namespace SatışProject.ViewComponents
{
    public class _DefaultLayoutHeaderPartials : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
