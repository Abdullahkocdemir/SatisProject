using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SatışProject.Entities.Base;

namespace SatışProject.Entities
{
    // Satış Yönetimi sınıfı, satış işlemlerini temsil eder.
    // FinancialEntity'den miras alarak ortak finansal alanları kullanır.
    public class Sale : FinancialEntity
    {

        // Satışın yapıldığı müşteri Id'si, zorunlu alan.
        [Required]
        public int CustomerId { get; set; }

        // Müşteri ile ilişki kurar.
        public virtual Customer Customer { get; set; } = null!;

        // Satışı yapan çalışanın Id'si, zorunlu alan.
        [Required]
        public int EmployeeId { get; set; }

        // Çalışan ile ilişki kurar.
        public virtual Employee Employee { get; set; } = null!;

        // Satılan ürünün Id'si, zorunlu alan.
        [Required]
        public int ProductId { get; set; }

        // Ürün ile ilişki kurar.
        public virtual Product Product { get; set; } = null!;

        // Satılan ürün miktarı - EKLENEN
        [Required]
        public int Quantity { get; set; } = 1;

        // Birim fiyat (ürün fiyatını saklamak için, sonradan değişmeyi önlemek için)
        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // KDV matrahı (vergi öncesi toplam tutar)
        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal SubTotal { get; set; }

        // Satış tarihi, zorunlu alan.
        [Required]
        public DateTime SaleDate { get; set; }

        // Satış numarası, zorunlu ve maksimum 50 karakter.
        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string SaleNumber { get; set; } = null!;

        // Satış durumu, varsayılan olarak Completed.
        [Required]
        public SaleStatus Status { get; set; } = SaleStatus.Completed;

        // Satışa dair notlar, opsiyonel ve maksimum 500 karakter.
        [Column(TypeName = "VarChar")]
        [StringLength(500)]
        public string? Notes { get; set; }

        // Faturalar koleksiyonu.
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }

    // Satış durumlarını belirten enum.
    public enum SaleStatus
    {
        Pending,    // Beklemede
        Completed,  // Tamamlandı
        Cancelled,  // İptal edildi
        Returned,   // İade edildi
        OnHold      // Beklemede (askıda)
    }
}