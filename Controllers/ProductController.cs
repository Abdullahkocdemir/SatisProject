using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;
using SatışProject.Entities;
using SatışProject.Models;
using System.Linq;

namespace SatışProject.Controllers
{
    [AllowAnonymous]
    public class ProductController : Controller
    {
        private readonly SatısContext _context;

        public ProductController(SatısContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                                         .Include(p => p.Category)
                                         .Include(p => p.Brand)
                                         .ToListAsync();

            var categories = await _context.Categories.ToListAsync();

            int basketItemCount = 0;
            string? basketIdString = HttpContext.Session.GetString("BasketId");
            if (basketIdString != null && int.TryParse(basketIdString, out int id))
            {
                var basket = await _context.Baskets
                                           .Include(b => b.BasketItems)
                                           .FirstOrDefaultAsync(b => b.BasketId == id);
                if (basket != null)
                {
                    basketItemCount = basket.BasketItems.Sum(bi => bi.Quantity);
                }
            }

            var model = new ProductListViewModel
            {
                Products = products,
                Categories = categories,
                BasketItemCount = basketItemCount
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                                        .Include(p => p.Category)
                                        .Include(p => p.Brand)
                                        .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            int basketItemCount = 0;
            string? basketIdString = HttpContext.Session.GetString("BasketId");
            if (basketIdString != null && int.TryParse(basketIdString, out int idVal))
            {
                var basket = await _context.Baskets
                                           .Include(b => b.BasketItems)
                                           .FirstOrDefaultAsync(b => b.BasketId == idVal);
                if (basket != null)
                {
                    basketItemCount = basket.BasketItems.Sum(bi => bi.Quantity);
                }
            }

            ViewData["BasketItemCount"] = basketItemCount;
            return View(product);
        }

        public class AddToBasketRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; } = 1;
        }

        [HttpPost]
        public async Task<IActionResult> AddToBasket([FromBody] AddToBasketRequest request)
        {
            // 1. Mevcut Sepeti Al veya Yeni Sepet Oluştur
            string? basketIdString = HttpContext.Session.GetString("BasketId");
            Basket? basket;
            int basketId;

            if (basketIdString == null || !int.TryParse(basketIdString, out basketId))
            {
                basket = new Basket();
                _context.Baskets.Add(basket);
                await _context.SaveChangesAsync();
                HttpContext.Session.SetString("BasketId", basket.BasketId.ToString());
            }
            else
            {
                basket = await _context.Baskets
                                       .Include(b => b.BasketItems)
                                       .FirstOrDefaultAsync(b => b.BasketId == basketId);

                if (basket == null)
                {
                    basket = new Basket();
                    _context.Baskets.Add(basket);
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetString("BasketId", basket.BasketId.ToString());
                }
            }

            // 2. Sepete eklenecek ürünü bul
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return NotFound(); // Ürün bulunamazsa 404 döndür
            }

            // **BURADA EKLEME YAPILDI: Ürün fiyatının geçerliliğini kontrol et**
            if (product.UnitPrice <= 0)
            {
                // Konsola veya loglama servisine hata mesajı yazdır
                Console.WriteLine($"HATA: Ürün ID {product.ProductId} için birim fiyat sıfır veya negatif. Sepete eklenemiyor.");
                return Json(new { success = false, message = "Ürün fiyatı geçersiz veya sıfır. Sepete eklenemedi." });
            }


            // 3. Sepet öğesini ekle veya güncelle
            var existingBasketItem = basket.BasketItems.FirstOrDefault(bi => bi.ProductId == request.ProductId);

            if (existingBasketItem != null)
            {
                existingBasketItem.Quantity += request.Quantity;
            }
            else
            {
                var newBasketItem = new BasketItem
                {
                    ProductId = request.ProductId,
                    BasketId = basket.BasketId,
                    Quantity = request.Quantity,
                    UnitPriceAtTimeOfAddition = product.UnitPrice // Bu değer product.UnitPrice'dan geliyor
                };
                _context.BasketItems.Add(newBasketItem);
            }

            await _context.SaveChangesAsync();

            // Güncel sepet öğesi sayısını al
            int currentBasketItemCount = basket.BasketItems.Sum(bi => bi.Quantity);

            return Json(new { success = true, basketItemCount = currentBasketItemCount });
        }
    }
}