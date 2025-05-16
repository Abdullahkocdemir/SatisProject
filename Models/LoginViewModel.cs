using System.ComponentModel.DataAnnotations;

namespace SatışProject.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta gereklidir.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Beni Hatırla")]
        public bool RememberMe { get; set; }
    }
}
