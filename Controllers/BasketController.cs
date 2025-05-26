using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;
using SatışProject.Entities;
using SatışProject.Models;

namespace SatışProject.Controllers
{
    [AllowAnonymous]
    public class BasketController : Controller
    {
        private readonly SatısContext _context;

        public BasketController(SatısContext context)
        {
            _context = context;
        }

        // Sepet sayfasını görüntüleme
        public async Task<IActionResult> Index()
        {
            string? basketIdString = HttpContext.Session.GetString("BasketId");
            BasketViewModel viewModel = new BasketViewModel
            {
                BasketItems = new List<BasketItemViewModel>(),
                TotalPrice = 0
            };

            if (basketIdString != null && int.TryParse(basketIdString, out int basketId))
            {
                var basket = await _context.Baskets
                    .Include(b => b.BasketItems)
                        .ThenInclude(bi => bi.Product)
                    .FirstOrDefaultAsync(b => b.BasketId == basketId);

                if (basket != null && basket.BasketItems.Any())
                {
                    viewModel.BasketItems = basket.BasketItems.Select(bi => new BasketItemViewModel
                    {
                        BasketItemId = bi.BasketItemId,
                        ProductId = bi.ProductId,
                        ProductName = bi.Product.Name,
                        Quantity = bi.Quantity,
                        UnitPrice = bi.UnitPriceAtTimeOfAddition,
                        ImageUrl = bi.Product.ImageUrl
                    }).ToList();

                    viewModel.TotalPrice = viewModel.BasketItems.Sum(bi => bi.Quantity * bi.UnitPrice);
                }
            }

            if (!viewModel.BasketItems.Any())
            {
                ViewBag.BasketMessage = "Sepetinizde henüz ürün bulunmamaktadır.";
            }

            return View(viewModel);
        }

        // Sepet miktar güncelleme (AJAX)
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int basketItemId, int newQuantity)
        {
            if (newQuantity < 0)
            {
                return Json(new { success = false, message = "Miktar negatif olamaz." });
            }

            var basketItem = await _context.BasketItems
                .Include(bi => bi.Product)
                .FirstOrDefaultAsync(bi => bi.BasketItemId == basketItemId);

            if (basketItem == null)
            {
                return Json(new { success = false, message = "Sepet öğesi bulunamadı." });
            }

            int quantityBeforeUpdate = basketItem.Quantity;
            bool itemRemoved = false;

            if (newQuantity == 0)
            {
                _context.BasketItems.Remove(basketItem);
                itemRemoved = true;
            }
            else
            {
                if (basketItem.Product.StockQuantity < newQuantity)
                {
                    var currentQuantity = quantityBeforeUpdate;
                    return Json(new
                    {
                        success = false,
                        message = $"Üzgünüz, bu üründen yeterli stok bulunmuyor. Mevcut stok: {basketItem.Product.StockQuantity}",
                        currentQuantity
                    });
                }

                basketItem.Quantity = newQuantity;
            }

            await _context.SaveChangesAsync();

            string? basketIdString = HttpContext.Session.GetString("BasketId");
            int currentBasketItemCount = 0;
            decimal currentTotalPrice = 0;

            if (basketIdString != null && int.TryParse(basketIdString, out int basketId))
            {
                var currentBasket = await _context.Baskets
                    .Include(b => b.BasketItems)
                        .ThenInclude(bi => bi.Product)
                    .FirstOrDefaultAsync(b => b.BasketId == basketId);

                if (currentBasket != null)
                {
                    currentBasketItemCount = currentBasket.BasketItems.Sum(bi => bi.Quantity);
                    currentTotalPrice = currentBasket.BasketItems.Sum(bi => bi.Quantity * bi.UnitPriceAtTimeOfAddition);
                }
            }

            return Json(new
            {
                success = true,
                basketItemCount = currentBasketItemCount,
                newTotal = currentTotalPrice,
                removed = itemRemoved
            });
        }

        // Sepetten ürün çıkarma (AJAX)
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int basketItemId)
        {
            var basketItem = await _context.BasketItems.FindAsync(basketItemId);

            if (basketItem == null)
            {
                return Json(new { success = false, message = "Sepet öğesi bulunamadı." });
            }

            _context.BasketItems.Remove(basketItem);
            await _context.SaveChangesAsync();

            string? basketIdString = HttpContext.Session.GetString("BasketId");
            int currentBasketItemCount = 0;
            decimal currentTotalPrice = 0;

            if (basketIdString != null && int.TryParse(basketIdString, out int basketId))
            {
                var basket = await _context.Baskets
                    .Include(b => b.BasketItems)
                        .ThenInclude(bi => bi.Product)
                    .FirstOrDefaultAsync(b => b.BasketId == basketId);

                if (basket != null)
                {
                    currentBasketItemCount = basket.BasketItems.Sum(bi => bi.Quantity);
                    currentTotalPrice = basket.BasketItems.Sum(bi => bi.Quantity * bi.UnitPriceAtTimeOfAddition);
                }
            }

            return Json(new
            {
                success = true,
                basketItemCount = currentBasketItemCount,
                newTotal = currentTotalPrice,
                removed = true
            });
        }

        // Sepet özeti (AJAX için)
        [HttpGet]
        public async Task<IActionResult> GetBasketSummary()
        {
            string? basketIdString = HttpContext.Session.GetString("BasketId");
            int currentBasketItemCount = 0;
            decimal currentTotalPrice = 0;

            if (basketIdString != null && int.TryParse(basketIdString, out int basketId))
            {
                var basket = await _context.Baskets
                    .Include(b => b.BasketItems)
                    .FirstOrDefaultAsync(b => b.BasketId == basketId);

                if (basket != null)
                {
                    currentBasketItemCount = basket.BasketItems.Sum(bi => bi.Quantity);
                    currentTotalPrice = basket.BasketItems.Sum(bi => bi.Quantity * bi.UnitPriceAtTimeOfAddition);
                }
            }

            return Json(new
            {
                success = true,
                basketItemCount = currentBasketItemCount,
                newTotal = currentTotalPrice
            });
        }
    }
}
