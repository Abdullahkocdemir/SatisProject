using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;
using SatışProject.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;


namespace SatışProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSaleController : Controller
    {
        private readonly SatısContext _context;
        public AdminSaleController(SatısContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index(int? employeeId, int? month, int? year)
        {
            IQueryable<Sale> salesQuery = _context.Sales
                .Include(x => x.Customer) 
                .Include(x => x.Employee) 
                    .ThenInclude(e => e!.AppUser); 

            if (employeeId.HasValue && employeeId.Value > 0)
            {
                salesQuery = salesQuery.Where(s => s.EmployeeId == employeeId.Value);
            }

            if (month.HasValue && month.Value >= 1 && month.Value <= 12)
            {
                salesQuery = salesQuery.Where(s => s.SaleDate.Month == month.Value);
            }

            if (year.HasValue && year.Value >= 1900 && year.Value <= DateTime.Now.Year + 5)
            {
                salesQuery = salesQuery.Where(s => s.SaleDate.Year == year.Value);
            }

            ViewBag.SelectedEmployeeId = employeeId;
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;


            ViewBag.Employees = (await _context.Employees
                                        .Include(e => e.AppUser)
                                        .Where(e => e.IsActive)
                                        .ToListAsync()) 
                                        .OrderBy(e => e.AppUser?.FullName) 
                                        .ToList();

            var sales = await salesQuery.OrderByDescending(x => x.SaleDate).ToListAsync();

            return View(sales);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadViewBagData(); 

            var newSale = new Sale
            {
                SaleNumber = GenerateSaleNumber(),
                SaleDate = DateTime.Now 
            };

            return View(newSale); 
        }

        [HttpPost]
        public async Task<IActionResult> Create(Sale sale)
        {
            await LoadViewBagData(); 

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
                    ModelState.AddModelError("", $"'{product.Name}' ürünü için yetersiz stok. Mevcut: {product.StockQuantity}, İstenen: {item.Quantity}");   return View(sale); 
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
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var saleItem = await _context.SaleItems
                .Include(si => si.Sale)
                .FirstOrDefaultAsync(si => si.Id == id);

            if (saleItem == null) return NotFound();

            ViewBag.Products = await _context.Products
                .Where(p => p.Status == ProductStatus.InStock || p.ProductId == saleItem.ProductId)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(saleItem);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(SaleItem saleItem)
        {
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
                    existingSaleItem.TaxAmount = saleItem.TaxAmount; 
                    existingSaleItem.TotalAmount = existingSaleItem.SubTotal + existingSaleItem.TaxAmount;

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
                        .OrderBy(p => p.Name) 
                        .ToListAsync();
                    return View(saleItem); 
                }
            }
        }
        public async Task<IActionResult> Details(int id)
        {
            var sale = await _context.Sales
                .Include(x => x.Customer) 
                .Include(x => x.Employee) 
                    .ThenInclude(e => e!.AppUser)
                .Include(x => x.SaleItems) 
                    .ThenInclude(si => si.Product) 
                .FirstOrDefaultAsync(x => x.Id == id);

            if (sale == null) return NotFound(); 
            return View(sale); 
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
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
        [HttpPost]
        public async Task<IActionResult> DeleteSaleItem(int id)
        {
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
        private async Task LoadViewBagData()
        {
            ViewBag.Customers = await _context.Customers.Where(x => x.IsActive).ToListAsync();

            var products = await _context.Products
                .Select(p => new
                {
                    p.ProductId,
                    p.Name,
                    p.UnitPrice,
                    p.StockQuantity
                }).ToListAsync();

            ViewBag.Products = products; 

            ViewBag.Employees = await _context.Employees.Include(e => e.AppUser).Where(e => e.IsActive).ToListAsync();

            ViewBag.SaleStatuses = Enum.GetValues(typeof(SaleStatus)).Cast<SaleStatus>();
        }
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