using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Models; 
using SatışProject.Entities;
using SatışProject.Context;

namespace SatışProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly SatısContext _context;

        public HomeController(SatısContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {

            var featuredProducts = await _context.Products
                                                .Include(p => p.Category)
                                                .Include(p => p.Brand)
                                                .Where(p => !p.IsDeleted)
                                                .OrderBy(p => p.ProductId) 
                                                .Take(12) 
                                                .ToListAsync();

            var model = new ProductListViewModel
            {
                Products = featuredProducts,
                Categories = new List<Category>() 
            };

            return View(model);
        }
        public IActionResult Index2()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }
    }
}