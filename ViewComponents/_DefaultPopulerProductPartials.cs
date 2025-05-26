using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;

namespace SatışProject.ViewComponents
{
    public class _DefaultPopulerProductPartials : ViewComponent
    {
        private readonly SatısContext _context;

        public _DefaultPopulerProductPartials(SatısContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var values = _context.Products.Include(x => x.Brand)
                .Include(y => y.Category)
                .Where(z => z.popularProduct == true).ToList();
            return View(values);
        }
    }
}
