using SatışProject.Entities;

namespace SatışProject.Models
{
    public class ProductListViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();

        // Sepetteki toplam ürün adedini tutacak yeni özellik
        public int BasketItemCount { get; set; }
    }
}