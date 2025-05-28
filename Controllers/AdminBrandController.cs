using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;
using SatışProject.Entities;

namespace SatışProject.Controllers
{
    public class AdminBrandController : Controller
    {
        private readonly SatısContext _context;

        public AdminBrandController(SatısContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var values = _context.Brands.ToList();
            return View(values);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand)
        {
            if (ModelState.IsValid)
            {
                _context.Brands.Add(brand);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
                return NotFound();

            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Brand brand)
        {
            if (!ModelState.IsValid)
                return View(brand);

            var existingBrand = await _context.Brands.AsNoTracking().FirstOrDefaultAsync(b => b.BrandID == brand.BrandID);
            if (existingBrand == null)
                return NotFound();

            _context.Brands.Update(brand);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
                return NotFound();

            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
                return NotFound();

            // Silmek yerine pasif yap
            brand.IsActive = false;

            _context.Brands.Update(brand);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

    }
}
