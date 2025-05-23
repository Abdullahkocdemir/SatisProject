// SatışProject.Entities/Customer.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatışProject.Entities
{
    public class Customer
    {
        public int CustomerID { get; set; }

        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(100)]
        public string CompanyName { get; set; } = null!;

        // Eğer müşteriler için bireysel bir iletişim adı tutmak istiyorsan, bunu ekle:
        [Column(TypeName = "VarChar")]
        [StringLength(100)]
        public string? ContactName { get; set; } // Bu alanı ekledim

        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(20)]
        [Phone]
        public string PhoneNumber { get; set; } = null!; // Burası zaten 'PhoneNumber' olarak tanımlıydı

        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(400)]
        public string Address { get; set; } = null!;

        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string City { get; set; } = null!;

        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string Country { get; set; } = null!;

        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(20)]
        public string TaxNumber { get; set; } = null!;

        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string? TaxOffice { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}