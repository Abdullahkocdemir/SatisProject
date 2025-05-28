// Models/AdminDashboardViewModel.cs
using SatışProject.Entities;

namespace SatışProject.Models
{
    public class AdminDashboardViewModel
    {
        // Add these two properties for the user's name
        public string UserFirstName { get; set; } = string.Empty;
        public string UserLastName { get; set; } = string.Empty;

        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }

        public List<Sale> RecentOrders { get; set; } = new List<Sale>();
        public Dictionary<string, int> TopSellingProducts { get; set; } = new Dictionary<string, int>(); // Ürün Adı, Satılan Miktar
        public List<Tuple<string, decimal>> SalesStatistics { get; set; } = new List<Tuple<string, decimal>>(); // Tarih/Ay, Satış Tutarı

        // YENİ EKLENECEK KISIM: Çalışan Bazlı Satış İstatistikleri
        // Her çalışan için bir Dictionary<Ay.Yıl, Toplam Satış> listesi
        public Dictionary<string, List<Tuple<string, decimal>>> EmployeeSalesStatistics { get; set; } = new Dictionary<string, List<Tuple<string, decimal>>>();

        public List<RecentActivityViewModel> RecentActivities { get; set; } = new List<RecentActivityViewModel>();
        public IEnumerable<ToDoItem> ToDoItems { get; set; } = new List<ToDoItem>();

    }

    public class RecentActivityViewModel
    {
        public string IconClass { get; set; } = string.Empty;
        public string BackgroundClass { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
    }
}