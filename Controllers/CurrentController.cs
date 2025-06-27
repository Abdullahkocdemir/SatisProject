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

        public IActionResult Index()
        {
            var values = _context.Customers.ToList();
            return View(values);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

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

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

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



        // CustomerController.cs

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // Müşteriyi, ilişkili satışlarını ve her satışın ürünlerini getiren sorgu
            var customer = await _context.Customers
                // 1. Gelen 'id' parametresine eşit olan MÜŞTERİYİ BUL.
                .Where(c => c.CustomerID == id)

                // 2. Bulunan müşterinin 'Sales' koleksiyonunu YÜKLE.
                .Include(c => c.Sales)
                    // 3. Yüklenen HER BİR 'Sale' kaydının içindeki 'SaleItems' (ürünlerini) YÜKLE.
                    .ThenInclude(s => s.SaleItems)

                // 5. Sorguyu çalıştır ve sonucu al.
                .FirstOrDefaultAsync();

            // Müşteri veritabanında bulunamazsa, NotFound (404) sayfası döndür.
            if (customer == null)
            {
                return NotFound();
            }

            // Müşteri nesnesini (ilişkili verileriyle birlikte) View'a gönder.
            return View(customer);
        }
    }
}
