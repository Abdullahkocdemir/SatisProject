namespace SatışProject.Models
{
    public class BasketItemViewModel
    {
        public int BasketItemId { get; set; } // Sepet öğesinin ID'si (güncelleme/silme için)
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; } // Sepete eklendiği anki birim fiyat
        public string? ImageUrl { get; set; }
        public decimal Subtotal => Quantity * UnitPrice; // Bu öğenin alt toplamı
    }
}
