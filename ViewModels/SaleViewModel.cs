using SatışProject.Entities;

namespace SatışProject.ViewModels
{
    public class SaleViewModel
    {
        public Sale? Sale { get; set; }
        public List<SaleDetailViewModel> SaleDetails { get; set; } = new List<SaleDetailViewModel>();
    }

    public class SaleDetailViewModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

}
