using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;

namespace SatışProject.ViewComponents
{
    public class _DefaultPopulerCategoryPartials : ViewComponent
    {
        private readonly SatısContext _context;

        public _DefaultPopulerCategoryPartials(SatısContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var values = _context.Categories.Include(x => x.Products).Where(x=>x.popularCategory==true).ToList();
            return View(values);
        }
    }
}
