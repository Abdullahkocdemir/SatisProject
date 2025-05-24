using System.ComponentModel.DataAnnotations;

namespace SatışProject.Models
{
    public class EmployeeDetailsViewModel
    {
        [Display(Name = "Adres")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Şehir")]
        public string City { get; set; } = string.Empty;

        [Display(Name = "Ülke")]
        public string Country { get; set; } = string.Empty;

        [Display(Name = "Unvan")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Departman")]
        public string DepartmentName { get; set; } = string.Empty;

        [Display(Name = "Doğum Tarihi")]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        [Display(Name = "Maaş")]
        [DataType(DataType.Currency)]
        public decimal Salary { get; set; }
    }
}
