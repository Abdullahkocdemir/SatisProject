using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatışProject.Entities
{
    // Ürünleri temsil eden sınıf
    public class Product
    {
        public int ProductId { get; set; }

        // Oluşturulma tarihi
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Güncellenme tarihi (opsiyonel)
        public DateTime? UpdatedDate { get; set; }

        // Silindi mi? (Soft delete için)
        public bool IsDeleted { get; set; } = false;

        // Ürün adı, zorunlu, maksimum 100 karakter
        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        // Ürün stok kodu (SKU), zorunlu, maksimum 50 karakter
        [BindNever]
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string SKU { get; set; } = null!;

        // Ürün barkodu, zorunlu, maksimum 50 karakter
        [BindNever]
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string Barcode { get; set; } = null!;

        // Ürün birim satış fiyatı, zorunlu
        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // Ürün maliyet fiyatı, zorunlu
        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal CostPrice { get; set; }

        // Stoktaki ürün miktarı, zorunlu
        [Required]
        public int StockQuantity { get; set; }

        // Kategori Id'si, zorunlu, foreign key olarak tanımlanır
        [Required]
        [ForeignKey("Category")]
        public int CategoryId { get; set; }

        // Kategori nesnesi
        public virtual Category? Category { get; set; }

        // Marka Id'si, zorunlu, foreign key olarak tanımlanır
        [Required]
        [ForeignKey("Brand")]
        public int BrandId { get; set; }



        // Marka nesnesi
        public virtual Brand? Brand { get; set; } 

        // Ürün görsel URL'si, opsiyonel, maksimum 350 karakter
        [Column(TypeName = "VarChar")]
        [StringLength(350)]
        public string? ImageUrl { get; set; }

        // Ürün vergi oranı, default %18.0
        [Column(TypeName = "Decimal(5,2)")]
        public decimal TaxRate { get; set; } = 18.0m;

        // Ürün durumu, default Available (mevcut)
        public ProductStatus Status { get; set; } = ProductStatus.InStock;

        public bool popularProduct { get; set; }

        // Satış detayları koleksiyonu
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

        // Fatura detayları koleksiyonu
        public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
    }

    // Ürün durumu enum'u
    public enum ProductStatus
    {
        [Display(Name = "Stokta")]
        InStock = 0,

        [Display(Name = "Tükendi")]
        OutOfStock = 1,

        [Display(Name = "Pasif")]
        Passive = 2
    }

}