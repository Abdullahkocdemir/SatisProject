using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SatışProject.Entities
{
    public class SaleItem
    {
        public int Id { get; set; }

        [Required]
        public int SaleId { get; set; }
        public virtual Sale? Sale { get; set; }

        [Required]
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        [Required]
        public decimal SubTotal { get; set; }

        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}

