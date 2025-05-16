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
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .ToList();

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
            // SKU oluştur
            var now = DateTime.Now;
            string datePart = now.ToString("MMMyydd"); // Ör: May2516
            string randomCode = new Random().Next(100000, 999999).ToString();
            product.SKU = $"{datePart}-{randomCode}";

            // QR Kod oluşturma - JPG formatında
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(product.SKU, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            using (var ms = new MemoryStream(qrCode.GetGraphic(20)))
            {
                using (Bitmap qrCodeImage = new Bitmap(ms))
                {
                    var qrFileName = $"{product.SKU}.jpg";
                    var qrPath = Path.Combine(_environment.WebRootPath, "QRCodes", qrFileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(qrPath)!);
                    qrCodeImage.Save(qrPath, System.Drawing.Imaging.ImageFormat.Jpeg);

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
            return View(product);
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
