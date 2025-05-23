namespace SatışProject.ViewModels
{
    public class InvoiceViewModel
    {
        public string? InvoiceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxTotal { get; set; } // Bu alanı Invoice sınıfına eklemen gerekebilir
        public decimal GrandTotal { get; set; } // Bu alanı Invoice sınıfına eklemen gerekebilir
        public List<InvoiceItemViewModel> Items { get; set; } = new List<InvoiceItemViewModel>();
    }
}
