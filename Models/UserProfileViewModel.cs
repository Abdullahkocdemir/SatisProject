using System.ComponentModel.DataAnnotations;

namespace SatışProject.Models
{
    public class UserProfileViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Display(Name = "Kullanıcı Adı")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Telefon Numarası")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Aktif Mi?")]
        public bool IsActive { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Profil Fotoğrafı")]
        public string? ProfilePhotoUrl { get; set; }

        [Display(Name = "Roller")]
        public IList<string> Roles { get; set; } = new List<string>();

        // Optional: Basic Employee Details for display
        public EmployeeDetailsViewModel? EmployeeDetails { get; set; }
    }
}
