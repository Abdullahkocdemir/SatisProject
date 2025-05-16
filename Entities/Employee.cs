using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatışProject.Entities
{
    public class Employee 
    {
        public int EmployeeID { get; set; }
        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(200)]
        public string Address { get; set; } =string.Empty;

        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string City { get; set; } = null!;

        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string Country { get; set; } = null!;

        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string Title { get; set; } = null!;

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        public virtual Department Department { get; set; } = null!;

        [Required]
        public DateTime BirthDate { get; set; }

        [Column(TypeName = "VarChar")]
        [StringLength(1000)]
        public string? Notes { get; set; }

        [Column(TypeName = "Decimal(18,2)")]
        public decimal Salary { get; set; }

        // 🔁 AppUser ile zorunlu birebir ilişki
        [Required]
        public string AppUserId { get; set; } = null!;

        [ForeignKey("AppUserId")]
        public virtual AppUser AppUser { get; set; } = null!;

        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}
