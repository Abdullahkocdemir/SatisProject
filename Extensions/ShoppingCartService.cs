using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SatışProject.Models; // CartItemViewModel için
using System.Collections.Generic;
using System.Linq;

namespace SatışProject.Extensions
{
    public class ShoppingCartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext!.Session;
        private const string CartSessionKey = "Cart";

        public ShoppingCartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Sepet içerisindeki tüm ürünleri getirir
        public List<CartItemViewModel> GetCartItems()
        {
            string? cartJson = _session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItemViewModel>();
            }
            // JsonConvert.DeserializeObject metodunun null dönebileceği durumlar için dikkatli olun
            return JsonConvert.DeserializeObject<List<CartItemViewModel>>(cartJson) ?? new List<CartItemViewModel>();
        }

        // Sepete ürün ekler veya mevcut ürünün miktarını artırır
        public void AddToCart(CartItemViewModel item)
        {
            List<CartItemViewModel> cart = GetCartItems();
            CartItemViewModel? existingItem = cart.FirstOrDefault(i => i.ProductId == item.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                cart.Add(item);
            }
            SaveCart(cart);
        }

        // Sepetten belirtilen ProductId'ye sahip ürünü kaldırır
        public void RemoveFromCart(int productId)
        {
            List<CartItemViewModel> cart = GetCartItems();
            cart.RemoveAll(item => item.ProductId == productId);
            SaveCart(cart);
        }

        // Sepeti tamamen temizler
        public void ClearCart()
        {
            _session.Remove(CartSessionKey);
        }

        // Sepet verilerini oturuma kaydeder
        private void SaveCart(List<CartItemViewModel> cart)
        {
            string cartJson = JsonConvert.SerializeObject(cart);
            _session.SetString(CartSessionKey, cartJson);
        }

        // Sepetteki toplam ürün sayısını döner
        public int GetCartItemCount()
        {
            return GetCartItems().Sum(item => item.Quantity);
        }

        // Sepetteki toplam tutarı döner
        public decimal GetCartTotal()
        {
            return GetCartItems().Sum(item => item.Quantity * item.UnitPrice);
        }
    }
}