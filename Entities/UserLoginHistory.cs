using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SatışProject.Entities
{
    // UserLoginHistory sınıfı, kullanıcıların giriş ve çıkış işlemlerinin kayıtlarını tutar.
    // BaseEntity'den miras alarak ortak alanları (Id, CreatedDate vb.) kullanır.
    public class UserLoginHistory 
    {
        public int UserLoginHistoryID { get; set; }
        // Kullanıcıya ait Id, zorunlu alan.
        [Required]
        public string UserId { get; set; } = null!;

        // Kullanıcının giriş yaptığı tarih ve saat, zorunlu alan.
        [Required]
        public DateTime LoginTime { get; set; }

        // Kullanıcının çıkış yaptığı tarih ve saat (opsiyonel).
        public DateTime? LogoutTime { get; set; }

        // Kullanıcının giriş yaptığı cihazın IP adresi, 
        // Veritabanında VarChar tipinde ve maksimum 50 karakter uzunluğunda.
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string? IpAddress { get; set; }

        // Kullanıcının kullandığı tarayıcı bilgisi veya cihaz bilgisi (User Agent),
        // VarChar tipi ve 500 karaktere kadar uzunlukta olabilir.
        [Column(TypeName = "VarChar")]
        [StringLength(500)]
        public string? UserAgent { get; set; }

        // Kullanıcıyla ilişkilendirilmiş AppUser nesnesi.
        // 'virtual' ile işaretlenerek lazy loading veya proxy oluşturulması sağlanabilir.
        public virtual AppUser User { get; set; } = null!;
    }
}
