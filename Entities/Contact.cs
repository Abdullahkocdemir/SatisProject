using iText.Layout.Properties;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SatışProject.Entities
{
    public class Contact
    {
        public int ContactId { get; set; }

        [Required]
        [Column(TypeName = "VarChar")]
        public string? NameSurname { get; set; }

        [Required]
        [Column(TypeName = "VarChar")]
        public string? Email { get; set; }
        
        [Required]
        [Column(TypeName = "VarChar")]
        public string? Subject { get; set; }

        [Required]
        [Column(TypeName="VarChar")]
        [StringLength(500)]
        public string? Message { get; set; }
    }
}
