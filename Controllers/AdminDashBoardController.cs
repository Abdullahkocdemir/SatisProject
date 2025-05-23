// Controllers/AdminDashBoardController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Models;
using SatışProject.Entities;
using SatışProject.Context;

namespace SatışProject.Controllers
{
    public class AdminDashBoardController : Controller
    {
        private readonly SatısContext _context;

        public AdminDashBoardController(SatısContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel();

            // Mevcut diğer istatistikler aynı kalacak...
            viewModel.TotalOrders = await _context.Sales.CountAsync();
            viewModel.TotalRevenue = await _context.Sales
                .Where(s => s.Status == SaleStatus.Completed)
                .SumAsync(s => s.GrandTotal);
            viewModel.TotalCustomers = await _context.Customers.CountAsync(c => c.IsActive);
            viewModel.TotalProducts = await _context.Products.CountAsync(p => p.Status == ProductStatus.InStock);
            viewModel.RecentOrders = await _context.Sales
                .Include(s => s.Customer)
                .OrderByDescending(s => s.SaleDate)
                .Take(5)
                .ToListAsync();

            var topSellingProducts = await _context.SaleItems
                .Where(si => si.Sale != null && si.Sale.SaleDate >= DateTime.Now.AddDays(-30))
                .GroupBy(si => new { si.ProductId, si.Product!.Name })
                .Select(g => new { g.Key.Name, TotalQuantitySold = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.TotalQuantitySold)
                .Take(5)
                .ToListAsync();

            foreach (var item in topSellingProducts)
            {
                viewModel.TopSellingProducts.Add(item.Name, item.TotalQuantitySold);
            }

            var salesStatsRaw = await _context.Sales
                .Where(s => s.SaleDate >= DateTime.Now.AddYears(-1))
                .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    TotalSales = g.Sum(s => s.GrandTotal)
                })
                .ToListAsync();

            var salesStats = salesStatsRaw
                .OrderBy(s => s.Year)
                .ThenBy(s => s.Month)
                .Select(s => new
                {
                    Date = $"{s.Month:D2}.{s.Year}",
                    s.TotalSales
                })
                .ToList();

            foreach (var stat in salesStats)
            {
                viewModel.SalesStatistics.Add(Tuple.Create(stat.Date, stat.TotalSales));
            }

            // YENİ EKLENECEK KISIM: Çalışan Bazlı Satış İstatistikleri
            var employeeSalesStatsRaw = await _context.Sales
                .Include(s => s.Employee) // Çalışan bilgilerini dahil et
                .Where(s => s.SaleDate >= DateTime.Now.AddYears(-1) && s.Employee != null) // Son 1 yıl ve çalışan bilgisi olanlar
                .GroupBy(s => new { s.Employee!.AppUserId, s.Employee.AppUser.FirstName, s.Employee.AppUser.LastName, s.SaleDate.Year, s.SaleDate.Month })
                .Select(g => new
                {
                    EmployeeName = $"{g.Key.FirstName} {g.Key.LastName}",
                    g.Key.Year,
                    g.Key.Month,
                    TotalSales = g.Sum(s => s.GrandTotal)
                })
                .ToListAsync(); // Veriyi buraya kadar veritabanından çekiyoruz

            // Bellek üzerinde çalışan bazında verileri gruplandırıp formatlıyoruz
            var employeeSalesGrouped = employeeSalesStatsRaw
                .GroupBy(s => s.EmployeeName)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.Year).ThenBy(x => x.Month)
                          .Select(x => Tuple.Create($"{x.Month:D2}.{x.Year}", x.TotalSales))
                          .ToList()
                );

            viewModel.EmployeeSalesStatistics = employeeSalesGrouped;


            viewModel.RecentActivities = new List<RecentActivityViewModel>
            {
                new RecentActivityViewModel { IconClass = "fas fa-shopping-cart", BackgroundClass = "bg-light-primary", Title = "Yeni sipariş alındı", Description = "Ahmet Yılmaz tarafından #ORD-7865", TimeAgo = "10 dk önce" },
                new RecentActivityViewModel { IconClass = "fas fa-box", BackgroundClass = "bg-light-success", Title = "Ürün stoku güncellendi", Description = "Laptop Acer - Stok: 24", TimeAgo = "45 dk önce" },
                new RecentActivityViewModel { IconClass = "fas fa-user", BackgroundClass = "bg-light-warning", Title = "Yeni kullanıcı kaydoldu", Description = "Zeynep Şahin - zeynep@example.com", TimeAgo = "2 saat önce" },
                new RecentActivityViewModel { IconClass = "fas fa-exclamation-triangle", BackgroundClass = "bg-light-danger", Title = "Stok uyarısı", Description = "Samsung Galaxy S22 - Kritik stok seviyesi", TimeAgo = "3 saat önce" },
                new RecentActivityViewModel { IconClass = "fas fa-credit-card", BackgroundClass = "bg-light-info", Title = "Ödeme alındı", Description = "Mehmet Demir - 1.875₺", TimeAgo = "5 saat önce" }
            };

            return View(viewModel);
        }
    }
}