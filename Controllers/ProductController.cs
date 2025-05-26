using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // DbContext için
using SatışProject.Context;
using SatışProject.Entities;
using SatışProject.Models; // Product ve Category için

namespace SatışProject.Controllers
{
    public class ProductController : Controller
    {
        private readonly SatısContext _context;

        public ProductController(SatısContext context)
        {
            _context = context;
        }



        // Tüm ürünleri ve kategorileri listeleyen metod
        public async Task<IActionResult> Index()
        {
            // Tüm ürünleri ilgili kategori bilgileriyle birlikte çek
            var products = await _context.Products
                                         .Include(p => p.Category) // Kategori bilgisini de yükle
                                         .Include(p => p.Brand)    // Marka bilgisini de yükle
                                         .Where(p => !p.IsDeleted) // Silinmemiş ürünleri getir
                                         .ToListAsync();

            // Tüm kategorileri çek
            var categories = await _context.Categories
                                           .Where(c => c.IsActive) // Aktif kategorileri getir
                                           .ToListAsync();

            // ViewModel oluşturarak hem ürünleri hem de kategorileri View'a gönder
            // Bunun için bir ViewModel sınıfına ihtiyacımız var.
            var model = new ProductListViewModel
            {
                Products = products,
                Categories = categories
            };

            return View(model);
        }

        // Ürün Detay sayfası (eğer varsa, dinamik hale getirelim)
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                                        .Include(p => p.Category)
                                        .Include(p => p.Brand)
                                        .FirstOrDefaultAsync(p => p.ProductId == id && !p.IsDeleted);

            if (product == null)
            {
                return NotFound(); // Ürün bulunamazsa 404 döndür
            }

            return View(product);
        }

        // Ana sayfadaki öne çıkan ürünler için bir Controller Action'ı oluşturabiliriz.
        // Veya Home/Index View'ı ProductController'dan veri çekebilir.
        // Basitlik adına, şimdilik Home Controller'ı kullanmıyorum ve ProductController'da kalıyorum.
        // Eğer Anasayfada öne çıkan ürünler göstermek istiyorsan, Home Controller'ına da benzer bir yapı eklemelisin.
    }
}