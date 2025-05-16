using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SatışProject.Context;
using SatışProject.Entities;
using SatışProject.Models;
using System.Drawing.Imaging;
using System.Drawing;
using ZXing.QrCode.Internal;

namespace SatışProject.Controllers
{
    public class ProductController : Controller
    {
        private readonly SatısContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductController(SatısContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult Index()
        {
            // Ürünlerle birlikte Kategori ve Marka bilgilerini getir
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .ToList();

            // ViewBag ile kategorileri filtreleme için gönder
            var categories = _context.Categories.ToList();
            ViewBag.Categories = categories;

            return View(products);
        }

        [HttpGet]
        public IActionResult Create()
        {
            LoadDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            // Stok Kodu
            var now = DateTime.Now;
            string datePart = now.ToString("MMMyydd"); // Ör: May2516
            string randomCode = new Random().Next(100000, 999999).ToString();
            product.SKU = $"{datePart}-{randomCode}";

            // QR Kod Oluşturma - URL içeren QR kod
            var qrGenerator = new QRCodeGenerator();

            // QR kod için URL oluşturma (tarayınca ürün detayına gidecek)
            string qrUrl = Url.Action("QRInfo", "Product", new { id = product.SKU }, Request.Scheme)!;

            var qrCodeData = qrGenerator.CreateQrCode(qrUrl!, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            using (var ms = new MemoryStream(qrCode.GetGraphic(20)))
            {
                using (Bitmap qrCodeImage = new Bitmap(ms))
                {
                    var qrFileName = $"{product.SKU}.jpg";
                    var qrPath = Path.Combine(_environment.WebRootPath, "QRCodes", qrFileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(qrPath)!);
                    qrCodeImage?.Save(qrPath!,ImageFormat.Jpeg);

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
            bool hasSales = await _context.Sales.AnyAsync(s => s.ProductId == product.ProductId);
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
                    string qrFilePath = Path.Combine(_environment.WebRootPath, product.Barcode.TrimStart('/'));
                    if (System.IO.File.Exists(qrFilePath))
                    {
                        System.IO.File.Delete(qrFilePath);
                    }
                }

                // Ürün görseli dosyasını sil
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
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
    }

}