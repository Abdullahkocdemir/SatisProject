using Microsoft.AspNetCore.Identity;

namespace SatışProject.Entities
{
    // AppUser sınıfı, IdentityUser sınıfından türetilmiştir.
    // ASP.NET Core Identity sistemindeki kullanıcı yapısını genişletir.
    public class AppUser : IdentityUser
    {
        // Kullanıcının adı
        public string FirstName { get; set; } = string.Empty;

        // Kullanıcının soyadı
        public string LastName { get; set; } = string.Empty;

        // FullName, FirstName ve LastName'in birleşimidir.
        public string FullName => $"{FirstName} {LastName}";

        // Kullanıcının aktiflik durumu
        public bool IsActive { get; set; }

        // Kullanıcının oluşturulma tarihi
        public DateTime CreatedAt { get; set; }

        // Kullanıcının son güncellenme tarihi (null olabilir)
        public DateTime? UpdatedAt { get; set; }

        public DateTime?ClosedAt { get; set; }

        // Profil fotoğrafının URL'si (null olabilir)
        public string? ProfilePhotoUrl { get; set; }

        // Kullanıcıya bağlı çalışan bilgisi (ilişkisel, null olabilir)
        // virtual sayesinde Lazy Loading etkin olur
        public virtual Employee? Employee { get; set; }

        // Kullanıcının giriş geçmişleri (1 kullanıcı - n giriş kaydı)
        // virtual ile EF Core ilişkileri gerektiğinde yükleyebilir
        public virtual ICollection<UserLoginHistory> LoginHistories { get; set; } = new List<UserLoginHistory>();

        // Kullanıcının rollerinin listesi (1 kullanıcı - n rol ilişkisi)
        // virtual ile Lazy Loading aktif olur
        public virtual ICollection<AppRole> UserRoles { get; set; } = new List<AppRole>();
    }
}
