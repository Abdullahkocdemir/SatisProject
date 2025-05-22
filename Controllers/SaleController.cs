using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;
using SatışProject.Entities;
using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SatışProject.Controllers
{

    public class SaleController : Controller
    {
        private readonly SatısContext _context;
        private readonly UserManager<AppUser> _userManager;


        public SaleController(SatısContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        public async Task<IActionResult> Index(int? month, int? year) // month ve year int? (nullable int) olarak kalmalı
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employees
                                         .Include(e => e.AppUser)
                                         .FirstOrDefaultAsync(e => e.AppUserId == currentUser.Id);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Kullanıcınızla ilişkili bir çalışan kaydı bulunamadı.";
                return View(new List<Sale>()); // Boş bir liste döndür
            }

            // Veritabanından sadece oturum açmış çalışana ait satışları çeker.
            IQueryable<Sale> salesQuery = _context.Sales
                .Include(x => x.Customer)
                .Include(x => x.Employee)
                    .ThenInclude(e => e.AppUser)
                .Where(x => x.EmployeeId == employee.EmployeeID); // Sadece mevcut çalışanın satışlarını filtreler.

            // Ay ve yıl filtrelemesini uygula
            // Eğer month.HasValue false ise (yani value="" veya hiç gelmediyse), ay filtresi uygulanmaz.
            if (month.HasValue && month.Value > 0 && month.Value <= 12)
            {
                salesQuery = salesQuery.Where(s => s.SaleDate.Month == month.Value);
            }
            // Eğer year.HasValue false ise (yani value="" veya hiç gelmediyse), yıl filtresi uygulanmaz.
            if (year.HasValue && year.Value > 0) // Yıl için 0 veya negatif değer gelmeyeceğini varsayalım
            {
                salesQuery = salesQuery.Where(s => s.SaleDate.Year == year.Value);
            }

            // Seçilen ay ve yılı görünümde tekrar kullanmak için ViewBag'e kaydet
            // month ve year zaten int? tipinde olduğu için direkt atayabiliriz.
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;

            var sales = await salesQuery.OrderByDescending(x => x.SaleDate).ToListAsync();

            // Kullanıcının yönetici olup olmadığını ViewBag'e ekle
            ViewBag.IsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            return View(sales);
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employees
                                         .Include(e => e.AppUser)
                                         .FirstOrDefaultAsync(e => e.AppUserId == currentUser.Id);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Kullanıcınızla ilişkili bir çalışan kaydı bulunamadı. Satış oluşturulamaz.";
                return RedirectToAction("Index");
            }

            await LoadViewBagData(employee.EmployeeID);

            var newSale = new Sale
            {
                SaleNumber = GenerateSaleNumber(),
                SaleDate = DateTime.Now,
                EmployeeId = employee.EmployeeID
            };

            return View(newSale);
        }

        // Yeni satış formunun gönderimini işlemek için kullanılan eylem metodu (POST isteği).
        [HttpPost]
        public async Task<IActionResult> Create(Sale sale)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employees
                                         .Include(e => e.AppUser)
                                         .FirstOrDefaultAsync(e => e.AppUserId == currentUser.Id);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Kullanıcınızla ilişkili bir çalışan kaydı bulunamadı. Satış oluşturulamaz.";
                return RedirectToAction("Index");
            }

            sale.EmployeeId = employee.EmployeeID; // Satış görevlisini her zaman oturum açmış kullanıcıya ayarlar.

            await LoadViewBagData(employee.EmployeeID);

            if (sale.SaleItems == null || !sale.SaleItems.Any())
            {
                ModelState.AddModelError("", "En az bir ürün eklemelisiniz.");
                return View(sale);
            }

            foreach (var item in sale.SaleItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError("", $"Ürün bulunamadı (ID: {item.ProductId}).");
                    return View(sale);
                }
                if (product.StockQuantity < item.Quantity)
                {
                    ModelState.AddModelError("", $"'{product.Name}' ürünü için yetersiz stok. Mevcut: {product.StockQuantity}, İstenen: {item.Quantity}");
                    return View(sale);
                }
                item.UnitPrice = product.UnitPrice;
                item.SubTotal = item.UnitPrice * item.Quantity;
                item.TaxAmount = item.SubTotal * 0.18m;
                item.TotalAmount = item.SubTotal + item.TaxAmount;

                product.StockQuantity -= item.Quantity;
            }

            sale.SubTotal = sale.SaleItems.Sum(item => item.SubTotal);
            sale.TaxTotal = sale.SaleItems.Sum(item => item.TaxAmount);
            sale.GrandTotal = sale.SaleItems.Sum(item => item.TotalAmount);

            sale.SaleNumber = GenerateSaleNumber();
            sale.SaleDate = DateTime.Now;
            sale.Status = SaleStatus.Completed;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Satış başarıyla kaydedildi.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Hata: " + ex.Message);
                // İşlem geri alınırsa ürün stok değişikliklerini geri alır.
                foreach (var item in sale.SaleItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                    }
                }
                return View(sale);
            }
        }

        // Mevcut bir satış kalemini düzenleme formunu görüntülemek için kullanılan eylem metodu (GET isteği).
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.AppUserId == currentUser.Id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Kullanıcınızla ilişkili bir çalışan kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            var saleItem = await _context.SaleItems
                .Include(si => si.Sale)
                .FirstOrDefaultAsync(si => si.Id == id);

            if (saleItem == null) return NotFound();

            // Satış kaleminin mevcut kullanıcıya ait olup olmadığını kontrol et
            if (saleItem.Sale?.EmployeeId != employee.EmployeeID)
            {
                TempData["ErrorMessage"] = "Bu satış kalemini düzenleme yetkiniz bulunmamaktadır.";
                return RedirectToAction("Index");
            }

            ViewBag.Products = await _context.Products
                .Where(p => p.Status == ProductStatus.InStock || p.ProductId == saleItem.ProductId)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(saleItem);
        }

        // Düzenlenmiş satış kalemi formunun gönderimini işlemek için kullanılan eylem metodu (POST isteği).
        [HttpPost]
        public async Task<IActionResult> Edit(SaleItem saleItem)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.AppUserId == currentUser.Id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Kullanıcınızla ilişkili bir çalışan kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Products = await _context.Products
                    .Where(p => p.Status == ProductStatus.InStock || p.ProductId == saleItem.ProductId)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
                return View(saleItem);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var existingSaleItem = await _context.SaleItems
                        .Include(si => si.Sale)
                        .ThenInclude(s => s.SaleItems)
                        .FirstOrDefaultAsync(si => si.Id == saleItem.Id);

                    if (existingSaleItem == null)
                    {
                        await transaction.RollbackAsync();
                        return NotFound();
                    }

                    // Satış kaleminin mevcut kullanıcıya ait olup olmadığını kontrol et
                    if (existingSaleItem.Sale?.EmployeeId != employee.EmployeeID)
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = "Bu satış kalemini düzenleme yetkiniz bulunmamaktadır.";
                        return RedirectToAction("Index");
                    }

                    var oldProduct = await _context.Products.FindAsync(existingSaleItem.ProductId);
                    if (oldProduct != null)
                    {
                        oldProduct.StockQuantity += existingSaleItem.Quantity;
                        _context.Products.Update(oldProduct);
                    }

                    var newProduct = await _context.Products.FindAsync(saleItem.ProductId);
                    if (newProduct == null)
                    {
                        ModelState.AddModelError("", $"Yeni ürün bulunamadı (ID: {saleItem.ProductId}).");
                        await transaction.RollbackAsync();
                        ViewBag.Products = await _context.Products
                            .Where(p => p.Status == ProductStatus.InStock || p.ProductId == existingSaleItem.ProductId)
                            .ToListAsync();
                        return View(saleItem);
                    }

                    if (newProduct.StockQuantity < saleItem.Quantity && newProduct.ProductId != oldProduct?.ProductId)
                    {
                        ModelState.AddModelError("", $"'{newProduct.Name}' ürünü için yetersiz stok. Mevcut: {newProduct.StockQuantity}");
                        await transaction.RollbackAsync();
                        ViewBag.Products = await _context.Products
                            .Where(p => p.Status == ProductStatus.InStock || p.ProductId == existingSaleItem.ProductId)
                            .ToListAsync();
                        return View(saleItem);
                    }
                    else if (newProduct.ProductId == oldProduct?.ProductId && (newProduct.StockQuantity + existingSaleItem.Quantity) < saleItem.Quantity)
                    {
                        ModelState.AddModelError("", $"'{newProduct.Name}' ürünü için yetersiz stok. Mevcut: {newProduct.StockQuantity + existingSaleItem.Quantity}");
                        await transaction.RollbackAsync();
                        ViewBag.Products = await _context.Products
                            .Where(p => p.Status == ProductStatus.InStock || p.ProductId == existingSaleItem.ProductId)
                            .ToListAsync();
                        return View(saleItem);
                    }

                    newProduct.StockQuantity -= saleItem.Quantity;
                    _context.Products.Update(newProduct);

                    existingSaleItem.ProductId = saleItem.ProductId;
                    existingSaleItem.Quantity = saleItem.Quantity;
                    existingSaleItem.UnitPrice = newProduct.UnitPrice;
                    existingSaleItem.SubTotal = existingSaleItem.UnitPrice * existingSaleItem.Quantity;
                    // existingSaleItem.TaxAmount = existingSaleItem.SubTotal * 0.20m; // KDV oranı %20 olarak güncellendi -> KDV oranının View'dan alınması gerektiği için bu satır değişecektir.
                    // KDV oranı formdan alınmalı, SaleItem modelinde bir TaxRate propertysi varsa oradan, yoksa ViewBag'den ya da sabit bir değerden
                    // Buraya gelen SaleItem modelinin içindeki TaxAmount değeri, client tarafından hesaplanıp gönderilmiş olmalı.
                    // Eğer SaleItem modelinde TaxRate diye bir property yoksa, bu değeri client'tan direkt alıp kullanmak yerine
                    // ya SaleItem modeline eklemelisiniz, ya da burada sabit bir değeri kullanmaya devam etmelisiniz.
                    // Şu anki senaryoda, client tarafında hesaplanıp gönderildiğini varsayalım ve o değeri kullanalım.
                    existingSaleItem.TotalAmount = existingSaleItem.SubTotal + existingSaleItem.TaxAmount; // Eğer TaxAmount client'tan doğru geliyorsa

                    _context.SaleItems.Update(existingSaleItem);

                    var saleToUpdate = await _context.Sales
                                                     .Include(s => s.SaleItems)
                                                     .FirstOrDefaultAsync(s => s.Id == existingSaleItem.SaleId);

                    if (saleToUpdate != null)
                    {
                        saleToUpdate.SubTotal = saleToUpdate.SaleItems.Sum(item => item.SubTotal);
                        saleToUpdate.TaxTotal = saleToUpdate.SaleItems.Sum(item => item.TaxAmount);
                        saleToUpdate.GrandTotal = saleToUpdate.SaleItems.Sum(item => item.TotalAmount);
                        saleToUpdate.UpdatedDate = DateTime.Now;
                        _context.Sales.Update(saleToUpdate);
                    }
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Satış kalemi başarıyla güncellendi.";
                    return RedirectToAction("Details", new { id = existingSaleItem.SaleId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Satış kalemi güncellenemedi: " + ex.Message);
                    ViewBag.Products = await _context.Products
                        .Where(p => p.Status == ProductStatus.InStock || p.ProductId == saleItem.ProductId)
                        .OrderBy(p => p.Name) // Added OrderBy for consistency
                        .ToListAsync();
                    return View(saleItem);
                }
            }
        }

        // Belirli bir satışın detaylarını görüntülemek için kullanılan eylem metodu.
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.AppUserId == currentUser.Id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Kullanıcınızla ilişkili bir çalışan kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            var sale = await _context.Sales
                .Include(x => x.Customer)
                .Include(x => x.Employee)
                    .ThenInclude(e => e.AppUser)
                .Include(x => x.SaleItems)
                    .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (sale == null) return NotFound();

            // Satışın mevcut kullanıcıya ait olup olmadığını kontrol et
            if (sale.EmployeeId != employee.EmployeeID)
            {
                TempData["ErrorMessage"] = "Bu satışın detaylarını görüntüleme yetkiniz bulunmamaktadır.";
                return RedirectToAction("Index");
            }

            // Kullanıcının yönetici olup olmadığını ViewBag'e ekle
            ViewBag.IsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            return View(sale);
        }

        // Tüm satışı silmek için kullanılan eylem metodu (POST isteği, ana satış içindir, satış kalemi değil).
        // Sadece Admin rolündeki kullanıcılar bu işlemi yapabilir.
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // Yönetici rolünde değilse silme işlemine izin verme
            if (!await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                TempData["ErrorMessage"] = "Bu işlemi yapmaya yetkiniz bulunmamaktadır.";
                return RedirectToAction("Index");
            }

            var sale = await _context.Sales
                .Include(s => s.SaleItems)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null) return NotFound();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var item in sale.SaleItems)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity += item.Quantity;
                            _context.Products.Update(product);
                        }
                    }

                    _context.Sales.Remove(sale);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Satış başarıyla silindi ve ürün stokları güncellendi.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Satış silinemedi: " + ex.Message;
                    return RedirectToAction("Index");
                }
            }
        }

        // Bir satış içindeki tek bir satış kalemini silmek için kullanılan yardımcı eylem metodu.
        // Sadece Admin rolündeki kullanıcılar bu işlemi yapabilir.
        [HttpPost]
        public async Task<IActionResult> DeleteSaleItem(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Json(new { success = false, message = "Kullanıcı oturumu bulunamadı." });

            // Yönetici rolünde değilse silme işlemine izin verme
            if (!await _userManager.IsInRoleAsync(currentUser, "Admin"))
            {
                return Json(new { success = false, message = "Bu işlemi yapmaya yetkiniz bulunmamaktadır." });
            }

            var saleItem = await _context.SaleItems
                .Include(si => si.Sale)
                    .ThenInclude(s => s.SaleItems)
                .FirstOrDefaultAsync(si => si.Id == id);

            if (saleItem == null)
            {
                return Json(new { success = false, message = "Satış kalemi bulunamadı." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var product = await _context.Products.FindAsync(saleItem.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += saleItem.Quantity;
                        _context.Products.Update(product);
                    }

                    _context.SaleItems.Remove(saleItem);

                    var parentSale = saleItem.Sale;
                    if (parentSale != null)
                    {
                        parentSale.SaleItems.Remove(saleItem);

                        if (parentSale.SaleItems.Any())
                        {
                            parentSale.SubTotal = parentSale.SaleItems.Sum(item => item.SubTotal);
                            parentSale.TaxTotal = parentSale.SaleItems.Sum(item => item.TaxAmount);
                            parentSale.GrandTotal = parentSale.SaleItems.Sum(item => item.TotalAmount);
                            parentSale.UpdatedDate = DateTime.Now;
                            _context.Sales.Update(parentSale);
                        }
                        else
                        {
                            // Eğer satışta hiç kalem kalmazsa, tüm ana satışı sil
                            _context.Sales.Remove(parentSale);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Json(new { success = true, message = "Satış kalemi başarıyla silindi." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Satış kalemi silinirken hata oluştu: " + ex.Message });
                }
            }
        }

        // Açılır listeler için ortak ViewBag verilerini yüklemek için kullanılan yardımcı metot.
        private async Task LoadViewBagData(int currentEmployeeId)
        {
            ViewBag.Customers = await _context.Customers.Where(x => x.IsActive).ToListAsync();

            var products = await _context.Products
                .Select(p => new
                {
                    p.ProductId,
                    p.Name,
                    p.UnitPrice,
                    p.StockQuantity,
                    p.Status
                }).ToListAsync();

            ViewBag.Products = products;

            // Sadece mevcut oturum açmış çalışanı ViewBag'e yükler.
            ViewBag.Employees = await _context.Employees
                                             .Include(e => e.AppUser)
                                             .Where(e => e.EmployeeID == currentEmployeeId)
                                             .ToListAsync();

            ViewBag.SaleStatuses = Enum.GetValues(typeof(SaleStatus)).Cast<SaleStatus>();
        }

        // Benzersiz bir satış numarası oluşturmak için kullanılan yardımcı metot.
        private string GenerateSaleNumber()
        {
            string prefix = "SL";
            string dateCode = DateTime.Now.ToString("yyyyMMdd");

            var lastSaleOfToday = _context.Sales
                .Where(s => s.SaleNumber.StartsWith(prefix + dateCode))
                .OrderByDescending(s => s.SaleNumber)
                .Select(s => s.SaleNumber)
                .FirstOrDefault();

            int sequence = 1;
            if (!string.IsNullOrEmpty(lastSaleOfToday))
            {
                string sequencePart = lastSaleOfToday.Substring((prefix + dateCode).Length);
                if (int.TryParse(sequencePart, out int lastSequence))
                {
                    sequence = lastSequence + 1;
                }
            }

            return $"{prefix}{dateCode}{sequence:D3}";
        }
    }
}