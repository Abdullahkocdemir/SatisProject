// SatışProject.Entities/Invoice.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SatışProject.Entities.Base;

namespace SatışProject.Entities
{
    public class Invoice : FinancialEntity
    {
        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = null!;

        [Required]
        public DateTime InvoiceDate { get; set; }

        public DateTime? DueDate { get; set; }

        [Required]
        public InvoiceType Type { get; set; } = InvoiceType.Sales;

        [Required]
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Approved;

        public int? SaleId { get; set; }

        public virtual Sale? Sale { get; set; }

        [Required]
        public int CustomerId { get; set; }

        public virtual Customer Customer { get; set; } = null!;

        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        [Column(TypeName = "VarChar")]
        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal SubTotal { get; set; }

        // Yeni eklenecek alan: PDF dosyasının yolu
        [Column(TypeName = "VarChar")]
        [StringLength(255)] // Yeterli uzunlukta bir string olmalı
        public string? FilePath { get; set; } // PDF dosyasının kaydedileceği yol

        public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }

    public class InvoiceItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        public virtual Invoice Invoice { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }

        public virtual Product Product { get; set; } = null!;

        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(100)]
        public string ProductName { get; set; } = null!;

        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "Decimal(5,2)")]
        public decimal TaxRate { get; set; }

        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal Discount { get; set; }

        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal LineTotal { get; set; }
    }

    public enum InvoiceType
    {
        Sales,
        Purchase,
        Return,
        Proforma
    }

    public enum InvoiceStatus
    {
        Draft,
        Approved,
        Paid,
        Cancelled,
        Overdue
    }
}