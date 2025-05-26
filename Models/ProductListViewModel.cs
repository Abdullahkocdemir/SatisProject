using SatışProject.Entities;

namespace SatışProject.Models
{
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; } = new List<Product>();
        public List<Category> Categories { get; set; } = new List<Category>();
    }
}
