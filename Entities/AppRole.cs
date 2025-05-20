using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SatışProject.Entities
{
    public class AppRole : IdentityRole
    {
        [Required]
        [Display(Name = "Rol Açıklaması")]
        [StringLength(256)]
        public string Description { get; set; } = string.Empty;

        // Kullanıcılar (Çoka-çok ilişki için, bu bir kolaylık özelliğidir)
        // Bu koleksiyonu doğrudan sorgulama yerine UserManager üzerinden roller sorgulanmalıdır
        // Bu özellik genellikle Include ile veriler yüklendiğinde kolaylık sağlar
        public virtual ICollection<IdentityUserRole<string>> UserRoles { get; set; } = new List<IdentityUserRole<string>>();
    }
}