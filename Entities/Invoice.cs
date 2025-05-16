using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SatışProject.Entities.Base;

namespace SatışProject.Entities
{
    // Fatura sınıfı, FinancialEntity'den miras alarak finansal özellikleri kullanır
    public class Invoice : FinancialEntity
    {
        // Fatura numarası, zorunlu ve maksimum 50 karakter
        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = null!;

        // Fatura tarihi, zorunlu alan
        [Required]
        public DateTime InvoiceDate { get; set; }

        // Fatura vadesi, opsiyonel, belirtilmezse peşin satış
        public DateTime? DueDate { get; set; }

        // Fatura türü, varsayılan olarak satış faturası
        [Required]
        public InvoiceType Type { get; set; } = InvoiceType.Sales;

        // Fatura durumu, varsayılan olarak onaylanmış
        [Required]
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Approved;

        // İlişkili satış ID'si, satış faturalarında zorunlu değil
        public int? SaleId { get; set; }

        // Satış ile ilişki
        public virtual Sale? Sale { get; set; }

        // Faturanın ilişkili olduğu müşteri ID'si, zorunlu
        [Required]
        public int CustomerId { get; set; }

        // Müşteri ile ilişki
        public virtual Customer Customer { get; set; } = null!;

        // Ödeme şekli, opsiyonel
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        // Fatura notları, opsiyonel
        [Column(TypeName = "VarChar")]
        [StringLength(500)]
        public string? Notes { get; set; }

        // KDV matrahı (vergi öncesi toplam tutar)
        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal SubTotal { get; set; }

        // Fatura öğeleri koleksiyonu
        public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }

    // Fatura kalem detayları sınıfı
    public class InvoiceItem
    {
        // Birincil anahtar
        [Key]
        public int Id { get; set; }

        // İlişkili fatura ID'si, zorunlu
        [Required]
        public int InvoiceId { get; set; }

        // Fatura ile ilişki
        public virtual Invoice Invoice { get; set; } = null!;

        // İlişkili ürün ID'si, zorunlu
        [Required]
        public int ProductId { get; set; }

        // Ürün ile ilişki
        public virtual Product Product { get; set; } = null!;

        // Ürün adı, fatura anındaki değer saklanır
        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(100)]
        public string ProductName { get; set; } = null!;

        // Birim fiyat, fatura anındaki değer saklanır
        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // Ürün miktarı, zorunlu
        [Required]
        public int Quantity { get; set; }

        // Satır KDV oranı
        [Required]
        [Column(TypeName = "Decimal(5,2)")]
        public decimal TaxRate { get; set; }

        // Satır KDV tutarı
        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        // Satır indirim tutarı
        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal Discount { get; set; }

        // Satır toplam tutarı (vergi dahil)
        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal LineTotal { get; set; }
    }

    // Fatura türleri
    public enum InvoiceType
    {
        Sales,      // Satış faturası
        Purchase,   // Alış faturası
        Return,     // İade faturası
        Proforma    // Proforma (ön) fatura
    }

    // Fatura durumları
    public enum InvoiceStatus
    {
        Draft,      // Taslak
        Approved,   // Onaylanmış
        Paid,       // Ödenmiş
        Cancelled,  // İptal edilmiş
        Overdue     // Vadesi geçmiş
    }
}