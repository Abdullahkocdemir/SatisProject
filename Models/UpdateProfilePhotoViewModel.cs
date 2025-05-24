using System.ComponentModel.DataAnnotations;

namespace SatışProject.Models
{
    public class UpdateProfilePhotoViewModel
    {
        [Display(Name = "Profil Fotoğrafı Seç")]
        [DataType(DataType.Upload)]
        public IFormFile? ProfilePhotoFile { get; set; }

        [Display(Name = "Mevcut Profil Fotoğrafı")]
        public string? CurrentProfilePhotoUrl { get; set; }
    }
}
