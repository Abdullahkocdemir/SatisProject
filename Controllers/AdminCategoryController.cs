using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SatışProject.Context;
using SatışProject.Entities;
using Microsoft.EntityFrameworkCore;

namespace SatışProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCategoryController : Controller
    {
        private readonly SatısContext _context;

        public AdminCategoryController(SatısContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var values = _context.Categories.ToList();
            return View(values);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Added for security
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                _context.SaveChanges();
                TempData["Success"] = "Kategori başarıyla eklendi!";
                return RedirectToAction("Index");
            }
            return View(category);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null)
            {
                TempData["Error"] = "Kategori bulunamadı."; // Using TempData for consistency
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Added for security
        public IActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    _context.SaveChanges();
                    TempData["Success"] = "Kategori başarıyla güncellendi!";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Handle concurrency if needed, or simply reload if not found
                    if (!_context.Categories.Any(e => e.CategoryId == category.CategoryId))
                    {
                        TempData["Error"] = "Kategori bulunamadı veya eşzamanlılık hatası oluştu.";
                        return RedirectToAction("Index");
                    }
                    throw; // Rethrow if it's a true concurrency issue
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Kategori güncellenirken bir hata oluştu: {ex.Message}";
                    return View(category); // Return to view with current data to show errors
                }
            }
            // If ModelState is not valid, return the view with validation errors
            return View(category);
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var category = _context.Categories
                .Include(c => c.Products) // Include products to see related items if necessary
                .FirstOrDefault(c => c.CategoryId == id);

            if (category == null)
            {
                TempData["Error"] = "Kategori bulunamadı.";
                return NotFound();
            }
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null)
            {
                TempData["Error"] = "Kategori bulunamadı.";
                return RedirectToAction("Index");
            }

            // Kategoriye bağlı ürünler var mı kontrol et
            var hasProducts = _context.Products.Any(p => p.CategoryId == id);
            if (hasProducts)
            {
                TempData["Error"] = "Bu kategoriye bağlı ürünler bulunmaktadır. Önce ürünleri başka kategoriye taşıyın veya silin.";
                return RedirectToAction("Index");
            }

            try
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
                TempData["Success"] = "Kategori başarıyla silindi!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Kategori silinirken bir hata oluştu: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}