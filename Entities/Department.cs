using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatışProject.Entities
{
    public class Department
    {
        public int DepartmentID { get; set; }
        // Departman adı, zorunlu, VarChar(50) tipinde.
        [Required]
        [Column(TypeName = "VarChar")]
        [StringLength(50)]
        public string Name { get; set; } = null!;

        // Departman açıklaması, opsiyonel, VarChar(500) tipinde.
        [Column(TypeName = "VarChar")]
        [StringLength(500)]
        public string Description { get; set; } = null!;
        // Bu departmana bağlı çalışanların koleksiyonu, virtual.
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
