using System.ComponentModel.DataAnnotations;

namespace SatışProject.ViewModels
{
    public class EmployeeCreateViewModel
    {
        // AppUser bilgileri
        [Required(ErrorMessage = "İsim alanı zorunludur.")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyisim alanı zorunludur.")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Şifre en az {2} karakter uzunluğunda olmalıdır.", MinimumLength = 6)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        // Employee bilgileri
        [Required(ErrorMessage = "Adres alanı zorunludur.")]
        [Display(Name = "Adres")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şehir alanı zorunludur.")]
        [Display(Name = "Şehir")]
        public string City { get; set; } = null!;

        [Required(ErrorMessage = "Ülke alanı zorunludur.")]
        [Display(Name = "Ülke")]
        public string Country { get; set; } = null!;

        [Required(ErrorMessage = "Unvan alanı zorunludur.")]
        [Display(Name = "Unvan")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Departman seçimi zorunludur.")]
        [Display(Name = "Departman")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Doğum tarihi zorunludur.")]
        [DataType(DataType.Date)]
        [Display(Name = "Doğum Tarihi")]
        public DateTime BirthDate { get; set; }

        [Display(Name = "Notlar")]
        public string? Notes { get; set; }

        [Display(Name = "Maaş")]
        [DataType(DataType.Currency)]
        [Range(0, double.MaxValue, ErrorMessage = "Maaş değeri pozitif olmalıdır.")]
        public decimal Salary { get; set; }

        [Display(Name = "Profil Fotoğrafı")]
        public IFormFile? ProfilePhoto { get; set; }
        public string? SelectedRole { get; set; }
    }
}
