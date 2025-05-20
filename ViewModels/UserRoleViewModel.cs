using System.ComponentModel.DataAnnotations;
using SatışProject.Entities;

namespace SatışProject.ViewModels
{
    public class UserRoleViewModel
    {
        public string UserId { get; set; } = null!;

        [Display(Name = "Kullanıcı Adı")]
        public string? UserName { get; set; }

        [Display(Name = "Mevcut Roller")]
        public List<string> UserRoles { get; set; } = new();

        [Display(Name = "Tüm Roller")]
        public List<AppRole> AllRoles { get; set; } = new();
    }
}
