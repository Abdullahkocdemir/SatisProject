using Microsoft.AspNetCore.Mvc;
using SatışProject.Entities; // Product ve Category modellerinin olduğu namespace
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;
using SatışProject.Models; // .Include() metodu için gerekli

namespace SatışProject.Controllers
{
    public class DenemeController : Controller
    {
        private readonly SatısContext _context; // DbContext'i enjekte edeceğiz

        // Constructor ile DbContext'i enjekte et
        public DenemeController(SatısContext context)
        {
            _context = context;
        }

        // Index Action: Tüm ürünleri listeler
        public async Task<IActionResult> Index()
        {
            // Tüm ürünleri kategori ve marka bilgileriyle birlikte veritabanından çek
            var products = await _context.Products
                                         .Include(p => p.Category) // Kategori bilgilerini de yükle
                                         .Include(p => p.Brand)    // Marka bilgilerini de yükle
                                         .ToListAsync();

            // Tüm kategorileri de çek, bu kategori filtresi için kullanılacak
            var categories = await _context.Categories.ToListAsync();

            // ViewModel oluşturarak hem ürünleri hem de kategorileri View'a gönder
            var model = new ProductListViewModel
            {
                Products = products,
                Categories = categories
            };

            return View(model);
        }

        // Details Action: Belirli bir ürünün detaylarını gösterir
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                                        .Include(p => p.Category) // Kategori bilgilerini de yükle
                                        .Include(p => p.Brand)    // Marka bilgilerini de yükle
                                        .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound(); // Ürün bulunamazsa 404 sayfası
            }
            return View(product); // Ürünü View'a aktar
        }

        // Diğer Action'lar
        public IActionResult UserManager()
        {
            return View();
        }
        public IActionResult Index2()
        {
            return View();
        }
        public async Task<IActionResult> Index3()
        {
            // Tüm ürünleri kategori ve marka bilgileriyle birlikte veritabanından çek
            var products = await _context.Products
                                         .Include(p => p.Category) // Kategori bilgilerini de yükle
                                         .Include(p => p.Brand)    // Marka bilgilerini de yükle
                                         .ToListAsync();

            // Tüm kategorileri de çek, bu kategori filtresi için kullanılacak
            var categories = await _context.Categories.ToListAsync();

            // ViewModel oluşturarak hem ürünleri hem de kategorileri View'a gönder
            var model = new ProductListViewModel
            {
                Products = products,
                Categories = categories
            };

            return View(model);
        }
    }
}