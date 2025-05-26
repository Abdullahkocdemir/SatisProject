using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Models; // ViewModel için
using SatışProject.Entities;
using SatışProject.Context; // Product için

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
            // Öne çıkan ürünler için bir filtreleme yapabiliriz, örneğin ilk 8 ürün veya özel bir property'e göre.
            // Şimdilik sadece ilk 8 ürünü alalım.
            var featuredProducts = await _context.Products
                                                .Include(p => p.Category)
                                                .Include(p => p.Brand)
                                                .Where(p => !p.IsDeleted)
                                                .OrderBy(p => p.ProductId) // Veya başka bir sıralama kriteri
                                                .Take(12) // İlk 8 ürünü al
                                                .ToListAsync();

            // Ana sayfa için sadece ürün listesi içeren bir ViewModel de oluşturabiliriz,
            // veya ProductListViewModel'i de kullanabiliriz. Şimdilik ProductListViewModel'i kullanalım.
            var model = new ProductListViewModel
            {
                Products = featuredProducts,
                Categories = new List<Category>() // Ana sayfada kategorilere ihtiyacın yoksa boş bırakabilirsin
            };

            return View(model);
        }
        public IActionResult Index2()
        {
            return View();
        }
        // Privacy gibi diğer action'lar...
        public IActionResult Privacy()
        {
            return View();
        }
    }
}