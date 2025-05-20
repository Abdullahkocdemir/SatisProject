using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SatışProject.Entities
{
    // AppUser sınıfı, IdentityUser sınıfından türetilmiştir
    public class AppUser : IdentityUser
    {
        [Required]
        [Display(Name = "Ad")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Soyad")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Ad Soyad")]
        public string FullName => $"{FirstName} {LastName}";

        [Display(Name = "Aktif Mi?")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Güncelleme Tarihi")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Kapanış Tarihi")]
        public DateTime? ClosedAt { get; set; }

        [Display(Name = "Profil Fotoğrafı")]
        public string? ProfilePhotoUrl { get; set; }

        // Her kullanıcının mutlaka bir Employee kaydı olmalı (birebir ilişki)
        public virtual Employee Employee { get; set; } = null!;

        // Kullanıcının giriş geçmişleri (1 kullanıcı - n giriş kaydı)
        public virtual ICollection<UserLoginHistory> LoginHistories { get; set; } = new List<UserLoginHistory>();

        // Kullanıcının rolleri aracılığıyla ilişki (çoka-çok ilişki)
        // Not: Bu, Identity sisteminin kendi rollerinden farklı bir kolleksiyon
        // Doğrudan erişmek için kullanışlı olabilir
        public virtual ICollection<IdentityUserRole<string>> UserRoles { get; set; } = new List<IdentityUserRole<string>>();
    }
}