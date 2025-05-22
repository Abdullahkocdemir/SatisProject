using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;
using SatışProject.Entities;

namespace SatışProject.Controllers
{
    public class CurrentController : Controller
    {
        private readonly SatısContext _context;

        public CurrentController(SatısContext context)
        {
            _context = context;
        }

        // Müşteri Listeleme
        public IActionResult Index()
        {
            var values = _context.Customers.ToList();
            return View(values);
        }

        // Müşteri Ekleme (GET)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Müşteri Ekleme (POST)
        [HttpPost]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // Müşteri Güncelleme (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // Müşteri Güncelleme (POST)
        [HttpPost]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return Json(new { success = false, message = "Müşteri bulunamadı." });

            customer.IsActive = false;
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Müşteri pasif hale getirildi." });
        }



        // 🔍 Müşteri Detayları
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Sales)
                .Include(c => c.Invoices)
                .FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null)
                return NotFound();

            return View(customer);
        }
    }
}
