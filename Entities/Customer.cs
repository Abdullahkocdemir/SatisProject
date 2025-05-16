using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatışProject.Entities
{
    // Müşteri varlıklarını temsil eden sınıf, BaseEntity'den kalıtım alır.
    public class Customer
    {
        public int CustomerID { get; set; }
        // Şirket adı, zorunlu, VarChar(100) tipinde.
        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(100)]
        public string CompanyName { get; set; } = null!;

        // E-posta adresi, zorunlu, VarChar(100) tipinde, email formatında.
        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = null!;

        // Telefon numarası, zorunlu, VarChar(20) tipinde, telefon formatında.
        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(20)]
        [Phone]
        public string PhoneNumber { get; set; } = null!;

        // Adres, zorunlu, VarChar(100) tipinde.
        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(400)]
        public string Address { get; set; } = null!;

        // Şehir, zorunlu, VarChar(50) tipinde.
        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string City { get; set; } = null!;

        // Ülke, zorunlu, VarChar(50) tipinde.
        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string Country { get; set; } = null!;

        // Vergi numarası, zorunlu, VarChar(20) tipinde.
        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(20)]
        public string TaxNumber { get; set; } = null!;

        // Vergi dairesi, opsiyonel, VarChar(50) tipinde.
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string? TaxOffice { get; set; }

        // Müşterinin yaptığı satışlar koleksiyonu, virtual.
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

        // Müşteriye ait faturalar koleksiyonu, virtual.
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }

}
