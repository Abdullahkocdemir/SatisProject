using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SatışProject.Context;
using SatışProject.Entities;
using SatışProject.Models;
using System.Drawing.Imaging;
using System.Drawing;
using System.Globalization; // Add this namespace
using ZXing.QrCode.Internal;
using Microsoft.AspNetCore.Authorization;

namespace SatışProject.Controllers
{
    public class AdminProductController : Controller
    {
        private readonly SatısContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminProductController(SatısContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Index(int? categoryId)
        {
            // Ürünleri kategori ve marka ile birlikte al
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => !p.IsDeleted); // Soft delete'e dikkat

            // Eğer filtreleme yapılmışsa
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            var products = productsQuery.ToList();

            // Kategori listesini ViewBag ile gönder (dropdown için)
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.SelectedCategoryId = categoryId;

            return View(products);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            LoadDropdowns();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            // If model state is not valid, reload dropdowns and return view
            if (!ModelState.IsValid)
            {
                LoadDropdowns(product.CategoryId, product.BrandId);
                return View(product);
            }

            // Stok Kodu
            var now = DateTime.Now;
            string datePart = now.ToString("MMMyyyydd"); // Ör: May2516
            string randomCode = new Random().Next(100000, 999999).ToString();
            product.SKU = $"{datePart}-{randomCode}";

            // QR Kod Oluşturma - URL içeren QR kod
            var qrGenerator = new QRCodeGenerator();

            // QR kod için URL oluşturma (tarayınca ürün detayına gidecek)
            // Ensure the URL is correctly generated. Using Request.Scheme is important for absolute URLs.
            string qrUrl = Url.Action("QRInfo", "AdminProduct", new { id = product.SKU }, Request.Scheme)!;

            var qrCodeData = qrGenerator.CreateQrCode(qrUrl!, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            using (var ms = new MemoryStream(qrCode.GetGraphic(20)))
            {
                using (Bitmap qrCodeImage = new Bitmap(ms))
                {
                    var qrFileName = $"{product.SKU}.jpg";
                    var qrPath = Path.Combine(_environment.WebRootPath, "QRCodes", qrFileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(qrPath)!);
                    qrCodeImage?.Save(qrPath!, ImageFormat.Jpeg);

                    product.Barcode = $"/QRCodes/{qrFileName}";
                }
            }

            // Ürün görseli yükleme işlemi
            if (imageFile != null && imageFile.Length > 0)
            {
                var ext = Path.GetExtension(imageFile.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var savePath = Path.Combine(_environment.WebRootPath, "Product", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                product.ImageUrl = $"/Product/{fileName}";
            }

            product.CreatedDate = DateTime.Now;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ürün başarıyla eklendi.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                TempData["Error"] = "Ürün bulunamadı.";
                return RedirectToAction("Index");
            }
            LoadDropdowns(product.CategoryId, product.BrandId);
            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
        {
            if (id != product.ProductId)
            {
                TempData["Error"] = "Geçersiz ürün ID'si.";
                return RedirectToAction("Index");
            }

            // Manually remove validation errors for decimal fields if they are the cause
            // This can be a quick fix for culture-related parsing issues in model binding
            ModelState.Remove("UnitPrice");
            ModelState.Remove("CostPrice");
            ModelState.Remove("TaxRate");

            try
            {
                var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == id);

                if (existingProduct == null)
                {
                    TempData["Error"] = "Ürün bulunamadı.";
                    return RedirectToAction("Index");
                }

                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                    {
                        string oldImagePath = Path.Combine(_environment.WebRootPath, existingProduct.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Save new image
                    var ext = Path.GetExtension(imageFile.FileName);
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var savePath = Path.Combine(_environment.WebRootPath, "Product", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    product.ImageUrl = $"/Product/{fileName}";
                }
                else
                {
                    // Keep the existing image if no new one is uploaded
                    product.ImageUrl = existingProduct.ImageUrl;
                }

                // Preserve CreatedDate and SKU/Barcode as they are generated on creation
                product.CreatedDate = existingProduct.CreatedDate;
                product.SKU = existingProduct.SKU;
                product.Barcode = existingProduct.Barcode;

                // Update UpdatedDate
                product.UpdatedDate = DateTime.Now;

                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Ürün başarıyla güncellendi.";
                return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.ProductId))
                {
                    TempData["Error"] = "Ürün bulunamadı veya eşzamanlılık hatası oluştu.";
                    return RedirectToAction("Index");
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ürün güncellenirken bir hata oluştu: {ex.Message}";
                LoadDropdowns(product.CategoryId, product.BrandId);
                return View(product);
            }

            LoadDropdowns(product.CategoryId, product.BrandId);
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                TempData["Error"] = "Ürün bulunamadı.";
                return RedirectToAction("Index");
            }

            return View(product);
        }

        // QR kod tarandığında gösterilecek ürün bilgileri sayfası
        [HttpGet]
        public async Task<IActionResult> QRInfo(string id)
        {
            // SKU'ya göre ürünü bulma
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.SKU == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    TempData["Error"] = "Ürün bulunamadı.";
                    return RedirectToAction("Index");
                }

                // İlişkili kayıtları kontrol et
                bool hasRelatedRecords = await CheckForRelatedRecords(product);

                if (hasRelatedRecords)
                {
                    // Eğer ilişkili kayıt varsa soft delete uygula
                    product.IsDeleted = true;
                    product.UpdatedDate = DateTime.Now;
                    _context.Products.Update(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Ürün pasif duruma alındı. İlişkili kayıtlar olduğu için tamamen silinemedi.";
                }
                else
                {
                    // İlişkili dosyaları sil
                    DeleteProductFiles(product);

                    // Veritabanından ürünü kaldır
                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Ürün başarıyla silindi.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Hata durumunda
                TempData["Error"] = $"Silme işlemi sırasında bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // İlişkili kayıtları kontrol eden yardımcı metod
        private async Task<bool> CheckForRelatedRecords(Product product)
        {
            // Satış kaydı veya fatura kaydı var mı kontrol et
            bool hasSales = await _context.SaleItems.AnyAsync(s => s.ProductId == product.ProductId);
            bool hasInvoiceItems = await _context.InvoiceItems.AnyAsync(i => i.ProductId == product.ProductId);

            return hasSales || hasInvoiceItems;
        }

        // Ürün dosyalarını silen yardımcı metod
        private void DeleteProductFiles(Product product)
        {
            try
            {
                // QR Kodu dosyasını sil
                if (!string.IsNullOrEmpty(product.Barcode))
                {
                    // Remove leading '/' from Barcode path before combining with WebRootPath
                    string qrFilePath = Path.Combine(_environment.WebRootPath, product.Barcode.TrimStart('/'));
                    if (System.IO.File.Exists(qrFilePath))
                    {
                        System.IO.File.Delete(qrFilePath);
                    }
                }

                // Ürün görseli dosyasını sil
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    // Remove leading '/' from ImageUrl path before combining with WebRootPath
                    string imageFilePath = Path.Combine(_environment.WebRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imageFilePath))
                    {
                        System.IO.File.Delete(imageFilePath);
                    }
                }
            }
            catch
            {
                // Dosya silme işlemi başarısız olursa devam et
                // Sadece veritabanından kayıt silme işlemini etkilememesi için hata yönetimi
            }
        }

        private void LoadDropdowns(int? selectedCategoryId = null, int? selectedBrandId = null)
        {
            ViewBag.Categories = _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name,
                    Selected = selectedCategoryId.HasValue && c.CategoryId == selectedCategoryId.Value
                })
                .ToList();

            ViewBag.Brands = _context.Brands
                .Where(b => b.IsActive)
                .Select(b => new SelectListItem
                {
                    Value = b.BrandID.ToString(),
                    Text = b.Name,
                    Selected = selectedBrandId.HasValue && b.BrandID == selectedBrandId.Value
                })
                .ToList();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}