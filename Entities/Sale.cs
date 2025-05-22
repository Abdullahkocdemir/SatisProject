using SatışProject.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatışProject.Entities
{
    public class Sale : FinancialEntity
    {
        [Required]
        [Display(Name = "Müşteri")]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        [Required]
        [Display(Name = "Satış Sorumlusu")]
        public int EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        [Required]
        [Display(Name = "Satış Tarihi")]
        public DateTime SaleDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Satış Numarası")]
        [Column(TypeName = "VarChar(50)")]
        [StringLength(50)]
        public string SaleNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Durum")]
        public SaleStatus Status { get; set; } = SaleStatus.Pending;

        [Display(Name = "Notlar")]
        [Column(TypeName = "VarChar(500)")]
        [StringLength(500)]
        public string? Notes { get; set; }
        public decimal SubTotal { get; set; }         // Ürünlerin toplamı (KDV hariç)
        public decimal TaxTotal { get; set; }         // KDV toplamı
        public decimal GrandTotal { get; set; }       // Genel toplam (KDV dahil)

        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual List<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    }


    public enum SaleStatus
    {
        [Display(Name = "Beklemede")]
        Pending,

        [Display(Name = "Tamamlandı")]
        Completed,

        [Display(Name = "İptal Edildi")]
        Cancelled,

        [Display(Name = "İade Edildi")]
        Returned,

        [Display(Name = "Askıda")]
        OnHold
    }
}