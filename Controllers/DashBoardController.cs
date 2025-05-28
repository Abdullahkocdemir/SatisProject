using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Models;
using SatışProject.Entities;
using SatışProject.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; 

namespace SatışProject.Controllers
{
    [Authorize] 
    public class DashBoardController : Controller
    {
        private readonly SatısContext _context;
        private readonly UserManager<AppUser> _userManager;

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
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new AdminDashboardViewModel();
            viewModel.UserFirstName = currentUser.FirstName;
            viewModel.UserLastName = currentUser.LastName;

            var userSales = _context.Sales
                .Where(s => s.Employee != null && s.Employee.AppUserId == currentUser.Id);

            viewModel.TotalOrders = await userSales.CountAsync();
            viewModel.TotalRevenue = await userSales
                .Where(s => s.Status == SaleStatus.Completed)
                .SumAsync(s => s.GrandTotal);

            viewModel.TotalCustomers = await _context.Customers.CountAsync(c => c.IsActive);
            viewModel.TotalProducts = await _context.Products.CountAsync(p => p.Status == ProductStatus.InStock);

            viewModel.RecentOrders = await userSales
                .Include(s => s.Customer)
                .OrderByDescending(s => s.SaleDate)
                .Take(5)
                .ToListAsync();

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

            var employee = await _context.Employees
                                        .Where(e => e.AppUserId == currentUser.Id)
                                        .FirstOrDefaultAsync();

            if (employee != null)
            {
                viewModel.ToDoItems = await _context.ToDoItems
                                                    .Where(t => t.EmployeeId == employee.EmployeeID)
                                                    .OrderByDescending(t => t.CreatedDate)
                                                    .ToListAsync();
                ViewBag.EmployeeId = employee.EmployeeID;
            }
            else
            {
                viewModel.ToDoItems = new List<ToDoItem>(); 
            }

            return View(viewModel);
        }
    }
}