using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;
using SatışProject.Entities;

namespace SatışProject.Controllers
{
    public class AdminBrandController : Controller
    {
        private readonly SatısContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminBrandController(SatısContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
        public async Task<IActionResult> Create(Brand brand, IFormFile? LogoFile)
        {
            if (ModelState.IsValid)
            {
                if (LogoFile != null && LogoFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/brands");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(LogoFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await LogoFile.CopyToAsync(stream);
                    }

                    brand.LogoPath = "/uploads/brands/" + fileName;
                }


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
        public async Task<IActionResult> Edit(Brand brand, IFormFile? LogoFile)
        {
            if (!ModelState.IsValid)
                return View(brand);

            var existingBrand = await _context.Brands.AsNoTracking().FirstOrDefaultAsync(b => b.BrandID == brand.BrandID);
            if (existingBrand == null)
                return NotFound();

            // Yeni logo yüklenmişse
            if (LogoFile != null && LogoFile.Length > 0)
            {
                // Eski logoyu sil
                if (!string.IsNullOrEmpty(existingBrand.LogoPath))
                {
                    var oldLogoPath = Path.Combine(_environment.WebRootPath, existingBrand.LogoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldLogoPath))
                        System.IO.File.Delete(oldLogoPath);
                }

                // Yeni logoyu yükle
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "brands");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(LogoFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await LogoFile.CopyToAsync(stream);
                }

                brand.LogoPath = "/uploads/brands/" + fileName;
            }
            else
            {
                // Yeni logo yüklenmediyse eski logo yolu korunur
                brand.LogoPath = existingBrand.LogoPath;
            }

            _context.Brands.Update(brand);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
                return NotFound();

            // Logo dosyası varsa sunucudan sil
            if (!string.IsNullOrEmpty(brand.LogoPath))
            {
                var logoFullPath = Path.Combine(_environment.WebRootPath, brand.LogoPath.TrimStart('/'));

                if (System.IO.File.Exists(logoFullPath))
                {
                    System.IO.File.Delete(logoFullPath);
                }
            }

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

    }
}
