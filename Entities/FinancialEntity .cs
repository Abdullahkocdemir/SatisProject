using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatışProject.Entities.Base
{
    // Ortak finansal özellikleri içeren soyut temel sınıf
    public abstract class FinancialEntity
    {
        // Birincil anahtar
        [Key]
        public int Id { get; set; }

        // Oluşturulma tarihi
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Güncellenme tarihi (opsiyonel)
        public DateTime? UpdatedDate { get; set; }

        // Silindi mi? (Soft delete için)
        public bool IsDeleted { get; set; } = false;

        // Silme tarihi (opsiyonel)
        public DateTime? DeletedDate { get; set; }

        // Vergi miktarı
        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        // İndirim miktarı
        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal Discount { get; set; } = 0;

        // Toplam fiyat/tutar (vergi dahil)
        [Required]
        [Column(TypeName = "Decimal(18,2)")]
        public decimal TotalAmount { get; set; }
    }
}