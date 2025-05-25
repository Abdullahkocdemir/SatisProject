using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Models; // ViewModel'iniz burada olmalı
using SatışProject.Entities; // Entity'leriniz burada olmalı
using SatışProject.Context; // DbContext'iniz burada olmalı
using Microsoft.AspNetCore.Identity; // Kullanıcı bilgilerine erişim için
using Microsoft.AspNetCore.Authorization; // Yetkilendirme için

namespace SatışProject.Controllers
{
    [Authorize] // Sadece giriş yapmış kullanıcılar erişebilir
    public class DashBoardController : Controller
    {
        private readonly SatısContext _context;
        private readonly UserManager<AppUser> _userManager; // Kullanıcı bilgilerini almak için

        public DashBoardController(SatısContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                // Kullanıcı bulunamazsa veya oturum açmamışsa giriş sayfasına yönlendir
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new AdminDashboardViewModel();
            // Assign user's first and last name
            viewModel.UserFirstName = currentUser.FirstName; // Assuming AppUser has a FirstName property
            viewModel.UserLastName = currentUser.LastName;   // Assuming AppUser has a LastName property


            // Kullanıcının kendi satış istatistikleri
            var userSales = _context.Sales
                .Where(s => s.Employee != null && s.Employee.AppUserId == currentUser.Id);

            viewModel.TotalOrders = await userSales.CountAsync(); // Kullanıcının toplam siparişleri
            viewModel.TotalRevenue = await userSales
                .Where(s => s.Status == SaleStatus.Completed)
                .SumAsync(s => s.GrandTotal); // Kullanıcının tamamlanmış satış geliri

            // Toplam Müşteriler ve Toplam Ürünler (bu değerler tüm sistem için gösterilebilir)
            viewModel.TotalCustomers = await _context.Customers.CountAsync(c => c.IsActive);
            viewModel.TotalProducts = await _context.Products.CountAsync(p => p.Status == ProductStatus.InStock);

            // Kullanıcının son 5 satışı
            viewModel.RecentOrders = await userSales
                .Include(s => s.Customer)
                .OrderByDescending(s => s.SaleDate)
                .Take(5)
                .ToListAsync();

            // Kullanıcının en çok satan ürünleri (sadece kendi satışları içinde)
            var userTopSellingProducts = await _context.SaleItems
                .Where(si => si.Sale != null && si.Sale.Employee != null && si.Sale.Employee.AppUserId == currentUser.Id && si.Sale.SaleDate >= DateTime.Now.AddDays(-30))
                .GroupBy(si => new { si.ProductId, si.Product!.Name })
                .Select(g => new { g.Key.Name, TotalQuantitySold = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.TotalQuantitySold)
                .Take(5)
                .ToListAsync();

            foreach (var item in userTopSellingProducts)
            {
                viewModel.TopSellingProducts.Add(item.Name, item.TotalQuantitySold);
            }

            // Kullanıcının kendi satış istatistikleri (grafik için)
            var salesStatsRaw = await userSales
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
            viewModel.EmployeeSalesStatistics = new Dictionary<string, List<Tuple<string, decimal>>>();
            viewModel.RecentActivities = new List<RecentActivityViewModel>(); 

            return View(viewModel);
        }
    }
}