namespace SatışProject.Models
{
    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; } // Ürünün görselini de sepet sayfasında göstermek isterseniz
    }
}
