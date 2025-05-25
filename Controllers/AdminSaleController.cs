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

        /// <summary>
        /// AdminSaleController sınıfının yeni bir örneğini başlatır.
        /// Bağımlılık enjeksiyonu aracılığıyla SatısContext'i alır.
        /// </summary>
        /// <param name="context">Veritabanı bağlamı.</param>
        public AdminSaleController(SatısContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tüm satışların listesini personel, ay ve yıla göre filtreleyerek görüntüler.
        /// </summary>
        /// <param name="employeeId">Filtrelenecek personel ID'si (isteğe bağlı).</param>
        /// <param name="month">Filtrelenecek ay (isteğe bağlı).</param>
        /// <param name="year">Filtrelenecek yıl (isteğe bağlı).</param>
        /// <returns>Satış listesi görünümü.</returns>
        public async Task<IActionResult> Index(int? employeeId, int? month, int? year)
        {
            // Veritabanından tüm satışları çeker ve ilişkili verileri önyükler.
            IQueryable<Sale> salesQuery = _context.Sales
                .Include(x => x.Customer) // Her satış için ilişkili Müşteri verilerini önyükler.
                .Include(x => x.Employee) // Her satış için ilişkili Çalışan verilerini önyükler.
                    .ThenInclude(e => e.AppUser); // Çalışan içindeki AppUser verilerini de önyükler.

            // Personel filtresini uygula: Eğer 'employeeId' değeri varsa filtrele.
            if (employeeId.HasValue && employeeId.Value > 0)
            {
                salesQuery = salesQuery.Where(s => s.EmployeeId == employeeId.Value);
            }

            // Ay filtresini uygula: Eğer 'month' değeri varsa ve geçerliyse filtrele.
            if (month.HasValue && month.Value >= 1 && month.Value <= 12)
            {
                salesQuery = salesQuery.Where(s => s.SaleDate.Month == month.Value);
            }

            // Yıl filtresini uygula: Eğer 'year' değeri varsa ve geçerliyse filtrele.
            // Yıl için makul bir aralık (örn: 1900'den mevcut yıla + 5 yıl) kullanıldı.
            if (year.HasValue && year.Value >= 1900 && year.Value <= DateTime.Now.Year + 5)
            {
                salesQuery = salesQuery.Where(s => s.SaleDate.Year == year.Value);
            }

            // Seçilen filtre değerlerini görünümde tekrar kullanmak için ViewBag'e kaydet.
            ViewBag.SelectedEmployeeId = employeeId;
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;

            // Tüm aktif çalışanları (personel) filtreleme dropdown'ı için yükle.
            // FullName veritabanında maplenmediği için, listeyi önce belleğe çekip sonra sıralama yapıyoruz.
            // Hata düzeltme: ViewBag.Employees'i doldururken FirstOrDefault() yerine ToListAsync() ile çekip sonra sırala.
            ViewBag.Employees = (await _context.Employees
                                        .Include(e => e.AppUser)
                                        .Where(e => e.IsActive) // Sadece aktif çalışanları listele.
                                        .ToListAsync()) // Önce ToListAsync() ile veriyi belleğe çek.
                                        .OrderBy(e => e.AppUser?.FullName) // Sonra bellekte sırala.
                                        .ToList();

            // Sorguyu asenkron olarak çalıştır ve sonuçları tarihe göre azalan sırada sırala.
            var sales = await salesQuery.OrderByDescending(x => x.SaleDate).ToListAsync();

            // Satış listesini Index görünümüne gönderir.
            return View(sales);
        }

        /// <summary>
        /// Yeni bir satış oluşturma formunu görüntüler (GET isteği).
        /// </summary>
        /// <returns>Yeni satış oluşturma görünümü.</returns>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadViewBagData(); // Açılır listeler için ViewBag verilerini dolduran yardımcı metodu çağırır.

            // Yeni bir Sale (Satış) nesnesi oluşturur ve varsayılan değerleri atar.
            var newSale = new Sale
            {
                SaleNumber = GenerateSaleNumber(), // Benzersiz bir satış numarası oluşturur.
                SaleDate = DateTime.Now // Satış tarihini o anki tarih ve saate ayarlar.
            };

            return View(newSale); // Yeni Sale nesnesini Create görünümüne gönderir.
        }

        /// <summary>
        /// Yeni satış formunun gönderimini işler (POST isteği).
        /// Satış kalemlerinin doğrulaması, stok kontrolü ve veritabanı işlemlerini yönetir.
        /// </summary>
        /// <param name="sale">Oluşturulacak satış nesnesi.</param>
        /// <returns>Başarılı olursa Index sayfasına yönlendirir, aksi takdirde Create görünümünü hata mesajlarıyla birlikte döndürür.</returns>
        [HttpPost]
        public async Task<IActionResult> Create(Sale sale)
        {
            await LoadViewBagData(); // Doğrulama hataları durumunda ViewBag verilerini yeniden doldurmak için yardımcı metodu çağırır.

            // Satışa herhangi bir satış kaleminin eklenip eklenmediğini kontrol eder.
            if (sale.SaleItems == null || !sale.SaleItems.Any())
            {
                ModelState.AddModelError("", "En az bir ürün eklemelisiniz."); // Hiçbir öğe yoksa bir model hatası ekler.
                return View(sale); // Geçerli satış nesnesi ve hata mesajıyla birlikte görünümü döndürür.
            }

            // Her bir satış kalemini döngüye alarak doğrulama ve stok işlemlerini gerçekleştirir.
            foreach (var item in sale.SaleItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId); // Asenkron olarak ürünü bulur.
                if (product == null) // Ürünün var olup olmadığını kontrol eder.
                {
                    ModelState.AddModelError("", $"Ürün bulunamadı (ID: {item.ProductId})."); // Ürün bulunamazsa hata ekler.
                    return View(sale); // Hata ile birlikte görünümü döndürür.
                }
                if (product.StockQuantity < item.Quantity) // Ürün için yeterli stok olup olmadığını kontrol eder.
                {
                    ModelState.AddModelError("", $"'{product.Name}' ürünü için yetersiz stok. Mevcut: {product.StockQuantity}, İstenen: {item.Quantity}"); // Yetersiz stok varsa hata ekler.
                    return View(sale); // Hata ile birlikte görünümü döndürür.
                }
                item.UnitPrice = product.UnitPrice; // Satış kaleminin Birim Fiyatını, ürünün Birim Fiyatından ayarlar.
                item.SubTotal = item.UnitPrice * item.Quantity; // Satış kalemi için Ara Toplamı (KDV hariç) hesaplar.
                item.TaxAmount = item.SubTotal * 0.18m; // KDV Tutarını hesaplar (varsayılan KDV oranı %18).
                item.TotalAmount = item.SubTotal + item.TaxAmount; // Satış kalemi için Toplam Tutarı (KDV dahil) hesaplar.

                product.StockQuantity -= item.Quantity; // Ürünün stok miktarını azaltır.
            }

            // Ana Satış nesnesi için toplamları (ara toplam, KDV toplamı, genel toplam) satış kalemlerine göre hesaplar.
            sale.SubTotal = sale.SaleItems.Sum(item => item.SubTotal); // Tüm satış kalemlerinin Ara Toplamlarını toplar.
            sale.TaxTotal = sale.SaleItems.Sum(item => item.TaxAmount); // Tüm satış kalemlerinin KDV Tutarlarını toplar.
            sale.GrandTotal = sale.SaleItems.Sum(item => item.TotalAmount); // Tüm satış kalemlerinin Toplam Tutarlarını toplar.

            sale.SaleNumber = GenerateSaleNumber(); // Satış numarasını oluşturur.
            sale.SaleDate = DateTime.Now; // Satış tarihini o anki tarih ve saate ayarlar.
            sale.Status = SaleStatus.Completed; // Yeni satış için varsayılan bir durum (örneğin, Tamamlandı) ayarlar.

            using var transaction = await _context.Database.BeginTransactionAsync(); // Atomikliği sağlamak için bir veritabanı işlemi (transaction) başlatır.
            try
            {
                _context.Sales.Add(sale); // Yeni satışı Sales DbSet'ine ekler.
                await _context.SaveChangesAsync(); // Tüm değişiklikleri (yeni satış ve güncellenmiş ürün stokları) veritabanına kaydeder.
                await transaction.CommitAsync(); // Tüm işlemler başarılı olursa işlemi onaylar (commit).
                TempData["SuccessMessage"] = "Satış başarıyla kaydedildi."; // TempData için bir başarı mesajı ayarlar.
                return RedirectToAction("Index"); // Başarılı oluşturma işleminden sonra Index eylemine yönlendirir.
            }
            catch (Exception ex) // İşlem sırasında meydana gelen herhangi bir istisnayı yakalar.
            {
                await transaction.RollbackAsync(); // Bir hata oluşursa işlemi geri alır (rollback).
                ModelState.AddModelError("", "Hata: " + ex.Message); // Model durumuna bir hata mesajı ekler.
                // İşlem geri alınırsa ürün stok değişikliklerini geri alır, böylece veri tutarlılığı sağlanır.
                foreach (var item in sale.SaleItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity; // Kayıt başarısız olursa stoğu orijinaline geri yükseltir.
                    }
                }
                return View(sale); // Satış nesnesi ve hata mesajıyla birlikte görünümü döndürür.
            }
        }

        /// <summary>
        /// Mevcut bir satış kalemini düzenleme formunu görüntüler (GET isteği).
        /// </summary>
        /// <param name="id">Düzenlenecek satış kaleminin ID'si.</param>
        /// <returns>Satış kalemi düzenleme görünümü.</returns>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Satış kalemini ID'sine göre bulur ve ilişkili Satış nesnesini önyükler.
            var saleItem = await _context.SaleItems
                .Include(si => si.Sale)
                .FirstOrDefaultAsync(si => si.Id == id);

            if (saleItem == null) return NotFound(); // Satış kalemi bulunamazsa 404 Not Found döndürür.

            // Açılır liste için ürünleri yükler; stokta olan tüm ürünleri ve şu an seçili olan ürünü (stokta olmasa bile) dahil eder.
            ViewBag.Products = await _context.Products
                .Where(p => p.Status == ProductStatus.InStock || p.ProductId == saleItem.ProductId)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(saleItem); // Satış kalemini Edit görünümüne gönderir.
        }

        /// <summary>
        /// Düzenlenmiş satış kalemi formunun gönderimini işler (POST isteği).
        /// Stok kontrolü ve ana satış toplamlarının güncellenmesi gibi işlemleri yönetir.
        /// </summary>
        /// <param name="saleItem">Güncellenecek satış kalemi nesnesi.</param>
        /// <returns>Başarılı olursa ana satışın Detaylar sayfasına yönlendirir, aksi takdirde Edit görünümünü hata mesajlarıyla birlikte döndürür.</returns>
        [HttpPost]
        public async Task<IActionResult> Edit(SaleItem saleItem)
        {
            // Model durumunun geçerli olup olmadığını kontrol eder (örneğin, tüm gerekli alanların dolu olup olmadığını).
            if (!ModelState.IsValid)
            {
                // Geçersizse, görünümü döndürmeden önce açılır liste için ürün verilerini yeniden yükler.
                ViewBag.Products = await _context.Products
                    .Where(p => p.Status == ProductStatus.InStock || p.ProductId == saleItem.ProductId)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
                return View(saleItem); // Doğrulama hatalarıyla birlikte görünümü döndürür.
            }

            using (var transaction = await _context.Database.BeginTransactionAsync()) // Asenkron olarak işlem başlatır.
            {
                try
                {
                    // Mevcut SaleItem'ı veritabanından çeker; ilişkili ana Satış nesnesini ve o Satışın diğer tüm SaleItem'larını dahil eder.
                    var existingSaleItem = await _context.SaleItems
                        .Include(si => si.Sale)
                            .ThenInclude(s => s.SaleItems) // Ana satışın toplamlarını yeniden hesaplamak için kritik öneme sahiptir.
                        .FirstOrDefaultAsync(si => si.Id == saleItem.Id);

                    if (existingSaleItem == null) // Mevcut satış kaleminin bulunup bulunmadığını kontrol eder.
                    {
                        await transaction.RollbackAsync(); // Bulunamazsa işlemi geri alır.
                        return NotFound(); // 404 Not Found döndürür.
                    }

                    // Eski ürünün stok miktarını geri alır.
                    var oldProduct = await _context.Products.FindAsync(existingSaleItem.ProductId);
                    if (oldProduct != null)
                    {
                        oldProduct.StockQuantity += existingSaleItem.Quantity; // Eski miktarı stoğa geri ekler.
                        _context.Products.Update(oldProduct); // Eski ürünü güncelleme için işaretler.
                    }

                    // Güncellenmiş satış kalemiyle ilişkili yeni ürünü bulur.
                    var newProduct = await _context.Products.FindAsync(saleItem.ProductId);
                    if (newProduct == null) // Yeni ürünün var olup olmadığını kontrol eder.
                    {
                        ModelState.AddModelError("", $"Yeni ürün bulunamadı (ID: {saleItem.ProductId})."); // Yeni ürün bulunamazsa hata ekler.
                        await transaction.RollbackAsync(); // İşlemi geri alır.
                                                           // ViewBag verilerini yeniden yükler.
                        ViewBag.Products = await _context.Products
                            .Where(p => p.Status == ProductStatus.InStock || p.ProductId == existingSaleItem.ProductId)
                            .ToListAsync();
                        return View(saleItem); // Hata ile birlikte görünümü döndürür.
                    }

                    // Yeni ürün için stok durumunu kontrol eder.
                    // Eğer ürün değişmişse veya aynı ürünse ama yeni miktar mevcut stoğu aşarsa.
                    if (newProduct.StockQuantity < saleItem.Quantity && newProduct.ProductId != oldProduct?.ProductId)
                    {
                        ModelState.AddModelError("", $"'{newProduct.Name}' ürünü için yetersiz stok. Mevcut: {newProduct.StockQuantity}"); // Yeni bir ürün için yetersiz stok hatası.
                        await transaction.RollbackAsync(); // İşlemi geri alır.
                                                           // ViewBag verilerini yeniden yükler.
                        ViewBag.Products = await _context.Products
                            .Where(p => p.Status == ProductStatus.InStock || p.ProductId == existingSaleItem.ProductId)
                            .ToListAsync();
                        return View(saleItem); // Hata ile birlikte görünümü döndürür.
                    }
                    else if (newProduct.ProductId == oldProduct?.ProductId && (newProduct.StockQuantity + existingSaleItem.Quantity) < saleItem.Quantity)
                    {
                        // Eğer ürün aynıysa, eski miktar geri eklendikten sonra stoğun yeterli olup olmadığını kontrol eder.
                        ModelState.AddModelError("", $"'{newProduct.Name}' ürünü için yetersiz stok. Mevcut: {newProduct.StockQuantity + existingSaleItem.Quantity}");
                        await transaction.RollbackAsync(); // İşlemi geri alır.
                                                           // ViewBag verilerini yeniden yükler.
                        ViewBag.Products = await _context.Products
                            .Where(p => p.Status == ProductStatus.InStock || p.ProductId == existingSaleItem.ProductId)
                            .ToListAsync();
                        return View(saleItem); // Hata ile birlikte görünümü döndürür.
                    }

                    // Yeni ürünün stok miktarını azaltır.
                    newProduct.StockQuantity -= saleItem.Quantity;
                    _context.Products.Update(newProduct); // Yeni ürünü güncelleme için işaretler.

                    // Mevcut SaleItem'ın özelliklerini yeni değerlerle günceller.
                    existingSaleItem.ProductId = saleItem.ProductId;
                    existingSaleItem.Quantity = saleItem.Quantity;
                    existingSaleItem.UnitPrice = newProduct.UnitPrice; // Ürünün güncel Birim Fiyatını kullanır.
                    existingSaleItem.SubTotal = existingSaleItem.UnitPrice * existingSaleItem.Quantity; // Ara Toplamı yeniden hesaplar.
                    existingSaleItem.TaxAmount = saleItem.TaxAmount; // KDV Tutarını client'tan gelen değerle günceller.
                    existingSaleItem.TotalAmount = existingSaleItem.SubTotal + existingSaleItem.TaxAmount; // Toplam Tutarı yeniden hesaplar.

                    _context.SaleItems.Update(existingSaleItem); // Mevcut satış kalemini güncelleme için işaretler.

                    // Ana Satış nesnesini toplamlarını güncellemek için bulur.
                    var saleToUpdate = await _context.Sales
                                                     .Include(s => s.SaleItems) // Toplamları yeniden hesaplamak için SaleItems'ı dahil eder.
                                                     .FirstOrDefaultAsync(s => s.Id == existingSaleItem.SaleId);

                    if (saleToUpdate != null) // Ana satışın var olup olmadığını kontrol eder.
                    {
                        // Ana satış için toplamları, güncellenmiş satış kalemlerine göre yeniden hesaplar.
                        saleToUpdate.SubTotal = saleToUpdate.SaleItems.Sum(item => item.SubTotal);
                        saleToUpdate.TaxTotal = saleToUpdate.SaleItems.Sum(item => item.TaxAmount);
                        saleToUpdate.GrandTotal = saleToUpdate.SaleItems.Sum(item => item.TotalAmount);
                        saleToUpdate.UpdatedDate = DateTime.Now; // Ana satışın güncellenme tarihini ayarlar.
                        _context.Sales.Update(saleToUpdate); // Ana satışı güncelleme için işaretler.
                    }
                    await _context.SaveChangesAsync(); // Tüm değişiklikleri (satış kalemi, ürünler, ana satış) veritabanına kaydeder.
                    await transaction.CommitAsync(); // İşlemi onaylar.

                    TempData["SuccessMessage"] = "Satış kalemi başarıyla güncellendi."; // Bir başarı mesajı ayarlar.
                    return RedirectToAction("Details", new { id = existingSaleItem.SaleId }); // Ana satışın Detaylar sayfasına yönlendirir.
                }
                catch (Exception ex) // İşlem sırasında herhangi bir istisnayı yakalar.
                {
                    await transaction.RollbackAsync(); // İşlemi geri alır.
                    ModelState.AddModelError("", "Satış kalemi güncellenemedi: " + ex.Message); // Bir hata mesajı ekler.
                                                                                                // Bir hata oluşursa ViewBag verilerini yeniden yükler.
                    ViewBag.Products = await _context.Products
                        .Where(p => p.Status == ProductStatus.InStock || p.ProductId == saleItem.ProductId)
                        .OrderBy(p => p.Name) // Added OrderBy for consistency
                        .ToListAsync();
                    return View(saleItem); // Hata ile birlikte görünümü döndürür.
                }
            }
        }
        public async Task<IActionResult> Details(int id)
        {
            // Veritabanından Satış nesnesini çeker ve ilişkili tüm verileri önyükler.
            var sale = await _context.Sales
                .Include(x => x.Customer) // Müşteri verilerini önyükler.
                .Include(x => x.Employee) // Çalışan verilerini önyükler.
                    .ThenInclude(e => e.AppUser) // Ardından Çalışan içindeki AppUser verilerini dahil eder.
                .Include(x => x.SaleItems) // Satışla ilişkili tüm SaleItem'ları dahil eder.
                    .ThenInclude(si => si.Product) // Ardından her bir SaleItem için Ürün verilerini dahil eder.
                .FirstOrDefaultAsync(x => x.Id == id); // Asenkron olarak sorgular.

            if (sale == null) return NotFound(); // Satış bulunamazsa 404 Not Found döndürür.
            return View(sale); // Satış nesnesini Details görünümüne gönderir.
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            // Satışı veritabanından çeker, ürün miktarlarını geri almak için SaleItems'ı da dahil eder.
            var sale = await _context.Sales
                .Include(s => s.SaleItems)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null) return NotFound(); // Satış bulunamazsa 404 Not Found döndürür.

            using (var transaction = await _context.Database.BeginTransactionAsync()) // Asenkron olarak işlem başlatır.
            {
                try
                {
                    // Satıştaki her bir satış kalemini döngüye alarak ürün miktarlarını geri alır.
                    foreach (var item in sale.SaleItems)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity += item.Quantity; // Satılan miktarı stoğa geri ekler.
                            _context.Products.Update(product); // Ürünü güncelleme için işaretler.
                        }
                    }

                    _context.Sales.Remove(sale); // Satışı veritabanından silinmek üzere işaretler.
                    await _context.SaveChangesAsync(); // Tüm değişiklikleri (satışın silinmesi ve ürün stok güncellemeleri) kaydeder.
                    await transaction.CommitAsync(); // İşlemi onaylar.

                    TempData["SuccessMessage"] = "Satış başarıyla silindi ve ürün stokları güncellendi."; // Bir başarı mesajı ayarlar.
                    return RedirectToAction("Index"); // Index eylemine yönlendirir.
                }
                catch (Exception ex) // Herhangi bir istisnayı yakalar.
                {
                    await transaction.RollbackAsync(); // İşlemi geri alır.
                    TempData["ErrorMessage"] = "Satış silinemedi: " + ex.Message; // Bir hata mesajı ayarlar.
                    return RedirectToAction("Index"); // Index eylemine yönlendirir.
                }
            }
        }


        /// <summary>
        /// Bir satış içindeki tek bir satış kalemini siler (POST isteği).
        /// Silme işlemi sırasında ürün stoğunu geri yükler ve ana satışın toplamlarını yeniden hesaplar.
        /// </summary>
        /// <param name="id">Silinecek satış kaleminin ID'si.</param>
        /// <returns>Başarı veya hata durumu belirten bir JSON yanıtı.</returns>
        [HttpPost]
        public async Task<IActionResult> DeleteSaleItem(int id)
        {
            // Satış kalemini bulur ve ana satışını ve ana satışın tüm diğer satış kalemlerini dahil eder.
            var saleItem = await _context.SaleItems
                .Include(si => si.Sale)
                    .ThenInclude(s => s.SaleItems) // Önemli: Toplamların yeniden hesaplanması için ana satışın tüm SaleItems'larını yükle.
                .FirstOrDefaultAsync(si => si.Id == id);

            if (saleItem == null) // Satış kalemi bulunamazsa, başarısızlık belirten bir JSON yanıtı döndürür.
            {
                return Json(new { success = false, message = "Satış kalemi bulunamadı." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync()) // Atomiklik için bir veritabanı işlemi başlatır.
            {
                try
                {
                    // Ürün stoğunu geri alır: İlişkili ürünü bulur ve miktarı geri ekler.
                    var product = await _context.Products.FindAsync(saleItem.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += saleItem.Quantity; // Stok miktarını artırır.
                        _context.Products.Update(product); // Ürünü güncelleme için işaretler.
                    }

                    // Satış kalemini veritabanı bağlamından kaldırır.
                    _context.SaleItems.Remove(saleItem);

                    // Ana satış toplamlarını yeniden hesaplar.
                    var parentSale = saleItem.Sale; // Ana satış nesnesini alır.
                    if (parentSale != null) // Ana satışın var olup olmadığını kontrol eder.
                    {
                        // Mevcut saleItem'ı ana satışın SaleItems koleksiyonundan bellekten kaldırır.
                        // Bu, Sum() metodunun silme işleminden sonra doğru öğeleri yansıtmasını sağlar.
                        parentSale.SaleItems.Remove(saleItem);

                        if (parentSale.SaleItems.Any()) // Ana satışta hala başka satış kalemleri varsa.
                        {
                            // Ana satış için tüm finansal toplamları yeniden hesaplar.
                            parentSale.SubTotal = parentSale.SaleItems.Sum(item => item.SubTotal);
                            parentSale.TaxTotal = parentSale.SaleItems.Sum(item => item.TaxAmount);
                            parentSale.GrandTotal = parentSale.SaleItems.Sum(item => item.TotalAmount);
                            parentSale.UpdatedDate = DateTime.Now; // Ana satışın değiştirme tarihini günceller.
                            _context.Sales.Update(parentSale); // Ana satışı güncelleme için işaretler.
                        }
                        else // Bu, satıştaki son satış kalemi ise.
                        {
                            // Eğer hiç kalem kalmazsa, tüm ana satışı silmeyi düşünebilirsiniz.
                            // Bu durumda, ParentSale'ı da veritabanından silmek gerekir.
                            _context.Sales.Remove(parentSale);
                        }
                    }

                    await _context.SaveChangesAsync(); // Tüm değişiklikleri veritabanına kaydeder.
                    await transaction.CommitAsync(); // Başarılı olursa işlemi onaylar.
                    return Json(new { success = true, message = "Satış kalemi başarıyla silindi." }); // Başarılı JSON yanıtı döndürür.
                }
                catch (Exception ex) // İşlem sırasında herhangi bir istisnayı yakalar.
                {
                    await transaction.RollbackAsync(); // Hata durumunda işlemi geri alır.
                    return Json(new { success = false, message = "Satış kalemi silinirken hata oluştu: " + ex.Message }); // Hata JSON yanıtı döndürür.
                }
            }
        }


        /// <summary>
        /// Açılır listeler için ortak ViewBag verilerini yükler.
        /// </summary>
        private async Task LoadViewBagData()
        {
            // Aktif müşterileri açılır listeler için ViewBag'e yükler.
            ViewBag.Customers = await _context.Customers.Where(x => x.IsActive).ToListAsync();

            // Verimlilik için ürünlerin belirli özelliklerini seçer ve ViewBag'e yükler.
            var products = await _context.Products
                .Select(p => new
                {
                    p.ProductId,
                    p.Name,
                    p.UnitPrice,
                    p.StockQuantity
                }).ToListAsync();

            ViewBag.Products = products; // Ürünlerin anonim tip listesini ViewBag'e atar.

            // Tüm aktif çalışanları (AppUser detaylarıyla birlikte) ViewBag'e yükler.
            ViewBag.Employees = await _context.Employees.Include(e => e.AppUser).Where(e => e.IsActive).ToListAsync();

            // Tüm SaleStatus (Satış Durumu) enum değerlerini açılır listeler için ViewBag'e yükler.
            ViewBag.SaleStatuses = Enum.GetValues(typeof(SaleStatus)).Cast<SaleStatus>();
        }
        private string GenerateSaleNumber()
        {
            string prefix = "SL"; // Satış numarası için önek tanımlar.
            string dateCode = DateTime.Now.ToString("yyyyMMdd"); // Bir tarih kodu oluşturur (örn: 20250522).

            // Bugün oluşturulan son satış numarasını bulmaya çalışır ve sırasını artırır.
            // FirstOrDefault() ile doğrudan sorgu yapılıyor, FindAsync() primary key için kullanılır.
            var lastSaleOfToday = _context.Sales
                .Where(s => s.SaleNumber.StartsWith(prefix + dateCode)) // Satışları bugünün tarih önekiyle filtreler.
                .OrderByDescending(s => s.SaleNumber) // En sonuncuyu almak için satış numarasını azalan sırada sıralar.
                .Select(s => s.SaleNumber) // Yalnızca SaleNumber dizesini seçer.
                .FirstOrDefault(); // İlk (en son) olanı alır veya hiçbiri yoksa null döndürür.

            int sequence = 1; // Sıra numarasını başlatır.
            if (!string.IsNullOrEmpty(lastSaleOfToday)) // Bugün için bir satış numarası bulunup bulunmadığını kontrol eder.
            {
                // Son satış numarasının önek ve tarih kodundan sonraki sayısal kısmını çıkarır.
                string sequencePart = lastSaleOfToday.Substring((prefix + dateCode).Length);
                if (int.TryParse(sequencePart, out int lastSequence)) // Çıkarılan kısmı bir tamsayıya dönüştürmeye çalışır.
                {
                    sequence = lastSequence + 1; // Sıra numarasını artırır.
                }
            }

            return $"{prefix}{dateCode}{sequence:D3}"; // Nihai satış numarasını biçimlendirir (örn: SL20250522001).
        }
    }
}