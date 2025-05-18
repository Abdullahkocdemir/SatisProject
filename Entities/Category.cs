using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatışProject.Entities
{
    // Kategori varlıklarını temsil eden sınıf, BaseEntity'den kalıtım alır.
    public class Category
    {
        public int CategoryId { get; set; }
        // Kategori adı, zorunlu, VarChar(100) tipinde.
        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(100)]
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }

        // Kategoriye ait ürünler koleksiyonu, virtual.
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
