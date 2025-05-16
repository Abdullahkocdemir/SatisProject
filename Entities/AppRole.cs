using Microsoft.AspNetCore.Identity;

namespace SatışProject.Entities
{
    // AppRole sınıfı, IdentityRole sınıfından türetilmiştir.
    // ASP.NET Core Identity sistemine entegre bir rol yapısı sunar.
    public class AppRole : IdentityRole
    {
        // Rolün açıklaması: Bu rol ne işe yarıyor? (örn: "Yönetici", "Satış Sorumlusu" gibi)
        public string Description { get; set; } = string.Empty;

        // Bu rolde bulunan kullanıcılar.
        // ICollection ile ilişki tipi tanımlanır (1 role - n kullanıcı ilişkisi).
        // virtual anahtar kelimesi Lazy Loading (tembel yükleme) özelliği için kullanılır.
        public virtual ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    }
}
