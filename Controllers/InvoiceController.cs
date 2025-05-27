using Microsoft.AspNetCore.Mvc; // ASP.NET Core MVC framework'ünü kullanmak için gerekli temel sınıfı içerir. Controller, ActionResult gibi sınıflar buradan gelir.
using Microsoft.EntityFrameworkCore; // Entity Framework Core ORM (Object-Relational Mapper) kütüphanesini kullanmak için gerekli sınıfları içerir. Veritabanı işlemleri için kullanılır.
using SatışProject.Context; // Projenin veritabanı bağlam sınıfını (DbContext) tanımladığı namespace. Veritabanı ile etkileşim bu bağlam üzerinden sağlanır.
using SatışProject.Entities; // Projenin veritabanı tablolarını temsil eden varlık (entity) sınıflarını tanımladığı namespace.
using System; // Temel sistem fonksiyonları ve sınıfları (örneğin, DateTime, Console) için gerekli namespace.
using System.Linq; // LINQ (Language Integrated Query) sorgularını kullanmak için gerekli extension metotları içerir.
using System.Threading.Tasks; // Asenkron programlama için gerekli sınıfları ve metotları (örneğin, Task) içerir.
using System.IO; // Dosya sistemi işlemleri (dosya okuma/yazma, dizin oluşturma) için gerekli sınıfları içerir.
using System.Globalization; // Kültüre özgü biçimlendirme (örneğin, para birimi, tarih) için gerekli sınıfları içerir.
using iText.Kernel.Pdf; // iText kütüphanesinin PDF belgesi oluşturmak için temel sınıfını içerir.
using iText.Layout; // iText kütüphanesinin PDF belgesi düzeni (paragraf, tablo vb.) oluşturmak için gerekli sınıfları içerir.
using iText.Layout.Element; // iText kütüphanesinin PDF belgesine eklenebilecek öğeleri (Text, Paragraph, Table) içerir.
using iText.Layout.Properties; // iText kütüphanesinin düzen özelliklerini (hizalama, genişlik) içerir.
using iText.Kernel.Colors; // iText kütüphanesinin renk tanımlamalarını (DeviceRgb) içerir.
using iText.Kernel.Font; // iText kütüphanesinin font işlemlerini içerir.
using iText.Kernel.Geom; // iText kütüphanesinin geometrik şekiller ve sayfa boyutları (PageSize) gibi tanımlamalarını içerir.
using iText.Layout.Borders; // iText kütüphanesinin tablo ve hücre kenarlıkları için sınıfları içerir.
using iText.IO.Font.Constants; // iText kütüphanesinin standart font sabitlerini (Times Roman, Times Bold) içerir.
using iText.IO.Font; // iText kütüphanesinin font giriş/çıkış işlemlerini (font dosyası yükleme) içerir.

using Microsoft.AspNetCore.Hosting; // Web sunucusunun barındırma ortamı bilgilerini (örneğin, wwwroot yolu) almak için gerekli sınıfı içerir.

namespace SatışProject.Controllers // Controller sınıflarının bulunduğu namespace.
{
    public class InvoiceController : Controller // InvoiceController sınıfı, ASP.NET Core'da bir denetleyici (controller) olarak tanımlanır ve Controller sınıfından miras alır.
    {
        private readonly SatısContext _context; // Veritabanı bağlamını (SatısContext) tutacak özel, sadece okunabilir alan. Dependency Injection ile dışarıdan enjekte edilir.
        private readonly IWebHostEnvironment _webHostEnvironment; // Web barındırma ortamı bilgilerini (wwwroot yolu gibi) tutacak özel, sadece okunabilir alan. Dependency Injection ile dışarıdan enjekte edilir.

        public InvoiceController(SatısContext context, IWebHostEnvironment webHostEnvironment) // InvoiceController'ın yapıcı metodu. Veritabanı bağlamı ve web barındırma ortamı bağımlılık enjeksiyonu ile alınır.
        {
            _context = context; // Enjekte edilen veritabanı bağlamını _context alanına atar.
            _webHostEnvironment = webHostEnvironment; // Enjekte edilen web barındırma ortamını _webHostEnvironment alanına atar.
        }

        // Mevcut Index metodu (Fatura Listesi)
        public async Task<IActionResult> Index() // Satışların listelendiği ana sayfa (Index) metodudur. Asenkron bir işlem olduğu için 'async Task<IActionResult>' kullanılır.
        {
            var sales = await _context.Sales // Veritabanındaki 'Sales' (Satışlar) tablosuna erişir.
                .Include(s => s.Customer) // Her satışa ait müşteri bilgilerini de dahil eder (ilişkili veriyi yükler).
                .Include(s => s.Employee) // Her satışa ait çalışan bilgilerini de dahil eder.
                    .ThenInclude(e => e!.AppUser) // Çalışan bilgileriyle birlikte ilişkili AppUser (uygulama kullanıcısı) bilgilerini de dahil eder.
                .OrderByDescending(s => s.SaleDate) // Satışları satış tarihine göre azalan sırada sıralar (en yeniden en eskiye).
                .ToListAsync(); // Sorgu sonucunu bir liste olarak asenkron bir şekilde veritabanından çeker.

            return View(sales); // Çekilen satış listesini View'e (görünüme) gönderir ve View'i döndürür.
        }

        // YENİ: Fatura Geçmişi Sayfası
        public async Task<IActionResult> InvoiceHistory() // Fatura geçmişini gösteren yeni bir metot. Asenkron bir işlem olduğu için 'async Task<IActionResult>' kullanılır.
        {
            // Yalnızca fatura dosya yolu kaydedilmiş satışları getir
            var invoices = await _context.Sales // Veritabanındaki 'Sales' (Satışlar) tablosuna erişir.
                .Where(s => s.InvoiceFilePath != null && s.InvoiceFilePath != "") // Yalnızca 'InvoiceFilePath' alanı boş olmayan veya null olmayan satışları filtreler. Yani faturası oluşturulmuş satışları getirir.
                .Include(s => s.Customer) // Her faturaya ait müşteri bilgilerini de dahil eder.
                .OrderByDescending(s => s.SaleDate) // Faturaları satış tarihine göre azalan sırada sıralar.
                .ToListAsync(); // Filtrelenmiş fatura listesini asenkron bir şekilde veritabanından çeker.

            return View(invoices); // Filtrelenmiş fatura listesini View'e gönderir ve View'i döndürür.
        }


        [HttpGet] // Bu aksiyon metodunun sadece HTTP GET isteklerine yanıt vereceğini belirtir.
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)] // Bu metodun çıktısının hiçbir yerde önbelleğe alınmamasını sağlar. Her zaman taze veri döndürülür.
        public async Task<IActionResult> GenerateInvoicePdf(int saleId) // Belirli bir satış ID'sine göre fatura PDF'i oluşturan metot. Asenkron bir işlemdir.
        {
            var sale = await _context.Sales // Veritabanındaki 'Sales' tablosuna erişir.
                .Include(s => s.Customer) // Satışla birlikte müşteri bilgilerini dahil eder.
                .Include(s => s.SaleItems) // Satışla birlikte satış kalemlerini (ürünleri) dahil eder.
                    .ThenInclude(si => si.Product) // Satış kalemleriyle birlikte ilişkili ürün bilgilerini de dahil eder.
                .FirstOrDefaultAsync(s => s.Id == saleId); // Verilen 'saleId'ye sahip ilk satış kaydını asenkron olarak bulur.

            if (sale == null) // Eğer belirtilen ID'ye sahip satış bulunamazsa
            {
                return NotFound($"Satış bulunamadı. ID: {saleId}"); // 404 Not Found (Bulunamadı) hatası döndürür ve bir mesaj gösterir.
            }

            // Para birimi biçimlendirmesi için kültürü ayarla
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR"); // Para birimi (₺) ve tarih (Gün Ay Yıl) gibi yerel biçimlendirmeler için geçerli kültürü Türkçe (Türkiye) olarak ayarlar.

            // Renkleri tanımla
            var primaryColor = new DeviceRgb(0, 102, 204); // Ana renk: Koyu Mavi (RGB değerleriyle tanımlanır).
            var secondaryColor = new DeviceRgb(240, 248, 255); // İkincil renk: Alice Mavisi (arka plan için).
            var accentColor = new DeviceRgb(255, 140, 0); // Vurgu rengi: Koyu Turuncu.
            var greyColor = new DeviceRgb(80, 80, 80);    // Koyu Gri.
            var blackColor = new DeviceRgb(0, 0, 0);      // Siyah.
            var lightGreyColor = new DeviceRgb(245, 245, 245); // Çok Açık Gri (tablo satırları için).


            // Font dosya adları (wwwroot/fonts klasörünüzde olduğundan emin olun)
            string regularFontFileName = "OpenSans-VariableFont_wdth,wght.ttf"; // Normal font dosya adı.
            string semiBoldFontFileName = "OpenSans_Condensed-SemiBold.ttf"; // Yarı kalın font dosya adı.
            string boldFontFileName = "OpenSans_Condensed-ExtraBold.ttf"; // Kalın font dosya adı.
            string italicFontFileName = "OpenSans-Italic-VariableFont_wdth,wght.ttf"; // İtalik font dosya adı.

            PdfFont font; // Normal fontu tutacak değişken.
            PdfFont boldFont; // Kalın fontu tutacak değişken.
            PdfFont semiBoldFont; // Yarı kalın fontu tutacak değişken.
            PdfFont italicFont = null!; // İtalik fontu tutacak değişken. (Null-forgiving operator ile başlatılır, çünkü Try-Catch içinde atanacak.)

            try // Font yükleme işlemleri sırasında oluşabilecek hataları yakalamak için try bloğu.
            {
                // Fontları yükle
                string regularFontPath = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "fonts", regularFontFileName); // Normal font dosyasının tam fiziksel yolunu oluşturur (wwwroot/fonts/OpenSans-VariableFont_wdth,wght.ttf).
                if (System.IO.File.Exists(regularFontPath)) // Eğer font dosyası belirtilen yolda mevcutsa
                {
                    font = PdfFontFactory.CreateFont(regularFontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED); // Fontu yükler. IDENTITY_H Türkçe karakterleri destekler. PREFER_EMBEDDED fontu PDF'e gömer.
                    Console.WriteLine($"DEBUG: Normal font yüklendi: {regularFontPath}"); // Konsola hata ayıklama mesajı yazar.
                }
                else // Font dosyası bulunamazsa
                {
                    Console.WriteLine($"WARNING: Normal font dosyası bulunamadı: {regularFontPath}. Varsayılan Times Roman kullanılıyor."); // Konsola uyarı mesajı yazar.
                    font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN); // Varsayılan Times Roman fontunu kullanır.
                }

                string boldFontPath = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "fonts", boldFontFileName); // Kalın font dosyasının tam fiziksel yolunu oluşturur.
                if (System.IO.File.Exists(boldFontPath)) // Eğer kalın font dosyası mevcutsa
                {
                    boldFont = PdfFontFactory.CreateFont(boldFontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED); // Kalın fontu yükler.
                    Console.WriteLine($"DEBUG: Kalın font yüklendi: {boldFontPath}"); // Konsola hata ayıklama mesajı yazar.
                }
                else // Kalın font dosyası bulunamazsa
                {
                    Console.WriteLine($"WARNING: Kalın font dosyası bulunamadı: {boldFontPath}. Normal font kullanılıyor (kalın olarak)."); // Konsola uyarı mesajı yazar.
                    boldFont = font; // Kalın font yerine normal fontu kullanır (bir tür geri dönüş).
                }

                string semiBoldFontPath = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "fonts", semiBoldFontFileName); // Yarı kalın font dosyasının tam fiziksel yolunu oluşturur.
                if (System.IO.File.Exists(semiBoldFontPath)) // Eğer yarı kalın font dosyası mevcutsa
                {
                    semiBoldFont = PdfFontFactory.CreateFont(semiBoldFontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED); // Yarı kalın fontu yükler.
                    Console.WriteLine($"DEBUG: Yarı kalın font yüklendi: {semiBoldFontPath}"); // Konsola hata ayıklama mesajı yazar.
                }
                else // Yarı kalın font dosyası bulunamazsa
                {
                    Console.WriteLine($"WARNING: Yarı kalın font dosyası bulunamadı: {semiBoldFontPath}. Normal font kullanılıyor (yarı kalın olarak)."); // Konsola uyarı mesajı yazar.
                    semiBoldFont = font; // Yarı kalın font yerine normal fontu kullanır.
                }

                string italicFontPath = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "fonts", italicFontFileName); // İtalik font dosyasının tam fiziksel yolunu oluşturur.
                if (System.IO.File.Exists(italicFontPath)) // Eğer italik font dosyası mevcutsa
                {
                    italicFont = PdfFontFactory.CreateFont(italicFontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED); // İtalik fontu yükler.
                    Console.WriteLine($"DEBUG: İtalik font yüklendi: {italicFontPath}"); // Konsola hata ayıklama mesajı yazar.
                }
                else // İtalik font dosyası bulunamazsa
                {
                    Console.WriteLine($"WARNING: İtalik font dosyası bulunamadı: {italicFontPath}. Normal font kullanılıyor (italik olarak)."); // Konsola uyarı mesajı yazar.
                    italicFont = font; // İtalik font yerine normal fontu kullanır.
                }
            }
            catch (IOException ex) // Font yükleme sırasında bir G/Ç hatası oluşursa (dosya erişim sorunları gibi).
            {
                Console.WriteLine($"HATA: Font yüklenirken G/Ç hatası oluştu: {ex.Message}. Varsayılan iText fontları kullanılıyor."); // Konsola hata mesajı yazar.
                font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN); // Varsayılan Times Roman fontunu kullanır.
                boldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD); // Varsayılan Times Bold fontunu kullanır.
                semiBoldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN); // Yarı kalın için varsayılan Times Roman'ı kullanır (iText'te doğrudan semi-bold yok).
                italicFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ITALIC); // Varsayılan Times Italic fontunu kullanır.
            }
            catch (Exception ex) // Diğer tüm beklenmedik hatalar oluşursa.
            {
                Console.WriteLine($"HATA: Font yüklenirken beklenmedik bir hata oluştu: {ex.Message}. Varsayılan iText fontları kullanılıyor."); // Konsola hata mesajı yazar.
                font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN); // Varsayılan Times Roman fontunu kullanır.
                boldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD); // Varsayılan Times Bold fontunu kullanır.
                semiBoldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN); // Yarı kalın için varsayılan Times Roman'ı kullanır.
                italicFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ITALIC); // Varsayılan Times Italic fontunu kullanır.
            }


            using (var stream = new MemoryStream()) // PDF belgesini bellekte oluşturmak için bir MemoryStream kullanır. 'using' bloğu, stream'in işi bittiğinde doğru şekilde kapatılmasını sağlar.
            {
                var writer = new PdfWriter(stream); // MemoryStream'e yazacak bir PdfWriter oluşturur.
                var pdf = new PdfDocument(writer); // PdfWriter'ı kullanarak yeni bir PDF belgesi oluşturur.
                // Daha fazla alan kazanmak için sayfa kenar boşluklarını azalt
                var document = new Document(pdf, PageSize.A4); // A4 boyutunda bir belge oluşturur.
                document.SetMargins(25, 25, 25, 25); // Belgenin tüm kenar boşluklarını 25 birime (varsayılan 36'dan azaltır) ayarlar.

                // --- Üst Bölüm: Şirket Adı/Logosu ve Fatura Başlığı ---
                var topHeaderTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 })) // İki eşit sütuna sahip bir tablo oluşturur.
                    .SetWidth(UnitValue.CreatePercentValue(100)) // Tablonun genişliğini sayfanın %100'ü olarak ayarlar.
                    .SetMarginBottom(20); // Tablonun alt kenar boşluğunu 20 birime ayarlar (azaltılmış).

                // Sol taraf: Şirket Adı veya Logo Alanı
                topHeaderTable.AddCell(new Cell().SetBorder(Border.NO_BORDER) // Kenarlıksız bir hücre oluşturur.
                    .Add(new Paragraph("KOÇDEMİR YAZILIM") // Şirket adını içeren bir paragraf ekler.
                        .SetFontSize(24).SetFont(boldFont).SetFontColor(primaryColor).SetTextAlignment(TextAlignment.LEFT)) // Yazı tipi boyutunu 24, kalın font, ana renk ve sola hizalı ayarlar.
                    .Add(new Paragraph("") // Boş bir paragraf ekler (belki logo veya ek bilgi için).
                        .SetFontSize(9).SetFont(italicFont).SetFontColor(greyColor).SetTextAlignment(TextAlignment.LEFT))); // Yazı tipi boyutunu 9, italik font, gri renk ve sola hizalı ayarlar.

                // Sağ taraf: Fatura Başlığı ve No
                topHeaderTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT) // Kenarlıksız, sağa hizalı bir hücre oluşturur.
                    .Add(new Paragraph("FATURA") // "FATURA" başlığını içeren bir paragraf ekler.
                        .SetFontSize(40).SetFont(boldFont).SetFontColor(primaryColor).SetMarginBottom(2)) // Yazı tipi boyutunu 40, kalın font, ana renk ve alt kenar boşluğunu 2 ayarlar.
                    .Add(new Paragraph($"No: {sale.SaleNumber}") // Fatura numarasını içeren bir paragraf ekler.
                        .SetFontSize(14).SetFont(semiBoldFont).SetFontColor(greyColor))); // Yazı tipi boyutunu 14, yarı kalın font ve gri renk ayarlar.

                document.Add(topHeaderTable); // Oluşturulan üst başlık tablosunu belgeye ekler.

                // Fatura Tarihi
                document.Add(new Paragraph($"Tarih: {sale.SaleDate:dd MMMM yyyy}") // Satış tarihini "gg Ay YYYY" formatında gösteren bir paragraf ekler.
                    .SetFontSize(10).SetFont(semiBoldFont).SetTextAlignment(TextAlignment.RIGHT).SetMarginBottom(15)); // Yazı tipi boyutunu 10, yarı kalın font, sağa hizalı ve alt kenar boşluğunu 15 ayarlar.

                // --- Müşteri ve Şirket Bilgileri Bölümü ---
                var infoSectionTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 })) // İki eşit sütuna sahip başka bir tablo oluşturur.
                    .SetWidth(UnitValue.CreatePercentValue(100)) // Tablonun genişliğini sayfanın %100'ü olarak ayarlar.
                    .SetMarginBottom(20); // Tablonun alt kenar boşluğunu 20 birime ayarlar (azaltılmış).

                // Müşteri Bilgi Kutusu
                var customerInfoCell = new Cell() // Yeni bir hücre oluşturur.
                    .SetBorder(new SolidBorder(primaryColor, 1)) // Ana renkte, 1 birim kalınlığında düz bir kenarlık ekler.
                    .SetBackgroundColor(secondaryColor) // Arka plan rengini ikincil renk olarak ayarlar.
                    .SetPadding(10); // Hücre içindeki dolguyu 10 birime ayarlar (azaltılmış).

                customerInfoCell.Add(new Paragraph("MÜŞTERİ BİLGİLERİ") // "MÜŞTERİ BİLGİLERİ" başlığını ekler.
                    .SetFont(boldFont).SetFontSize(12).SetFontColor(primaryColor).SetUnderline().SetMarginBottom(8)); // Kalın font, 12pt boyut, ana renk, altı çizili ve alt kenar boşluğunu 8 ayarlar.
                customerInfoCell.Add(new Paragraph().Add(new Text("Şirket Adı: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.CompanyName}").SetFont(font)).SetFontSize(9)); // Şirket adını ekler (yarı kalın başlık, normal font değer).
                if (!string.IsNullOrWhiteSpace(sale.Customer?.ContactName)) // Eğer ilgili kişi adı boş değilse veya sadece boşluklardan oluşmuyorsa
                    customerInfoCell.Add(new Paragraph().Add(new Text("İlgili Kişi: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer.ContactName}").SetFont(font)).SetFontSize(9)); // İlgili kişi adını ekler.
                customerInfoCell.Add(new Paragraph().Add(new Text("Adres: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.Address}, {sale.Customer?.City} / {sale.Customer?.Country}").SetFont(font)).SetFontSize(9)); // Adres bilgilerini ekler.
                customerInfoCell.Add(new Paragraph().Add(new Text("Telefon: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.PhoneNumber}").SetFont(font)).SetFontSize(9)); // Telefon numarasını ekler.
                customerInfoCell.Add(new Paragraph().Add(new Text("E-posta: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.Email}").SetFont(font)).SetFontSize(9)); // E-posta adresini ekler.
                customerInfoCell.Add(new Paragraph().Add(new Text("Vergi No: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.TaxNumber}").SetFont(font)).SetFontSize(9)); // Vergi numarasını ekler.

                infoSectionTable.AddCell(customerInfoCell); // Müşteri bilgi hücresini bilgi bölümü tablosuna ekler.

                // Şirket Bilgi Kutusu
                var companyInfoCell = new Cell() // Yeni bir hücre oluşturur.
                    .SetBorder(new SolidBorder(primaryColor, 1)) // Ana renkte, 1 birim kalınlığında düz bir kenarlık ekler.
                    .SetBackgroundColor(secondaryColor) // Arka plan rengini ikincil renk olarak ayarlar.
                    .SetPadding(10); // Hücre içindeki dolguyu 10 birime ayarlar (azaltılmış).

                companyInfoCell.Add(new Paragraph("ŞİRKET BİLGİLERİ") // "ŞİRKET BİLGİLERİ" başlığını ekler.
                    .SetFont(boldFont).SetFontSize(12).SetFontColor(primaryColor).SetUnderline().SetMarginBottom(8)); // Kalın font, 12pt boyut, ana renk, altı çizili ve alt kenar boşluğunu 8 ayarlar.
                companyInfoCell.Add(new Paragraph("Koçdemir Yazılım ve Danışmanlık Ltd. Şti.").SetFont(boldFont).SetFontSize(9).SetMarginTop(6)); // Şirket adını ekler.
                companyInfoCell.Add(new Paragraph("Demirciler Sanayi Sitesi No:15").SetFont(font).SetFontSize(9)); // Adres satırı 1'i ekler.
                companyInfoCell.Add(new Paragraph("Bursa, Türkiye").SetFont(font).SetFontSize(9)); // Adres satırı 2'yi ekler.
                companyInfoCell.Add(new Paragraph("Tel: +90 123 456 78 90").SetFont(font).SetFontSize(9)); // Telefon numarasını ekler.
                companyInfoCell.Add(new Paragraph("E-posta: kcdmirapo96@gmail.com").SetFont(font).SetFontSize(9)); // E-posta adresini ekler.
                companyInfoCell.Add(new Paragraph("Vergi Dairesi: Osmangazi V.D.").SetFont(font).SetFontSize(9)); // Vergi dairesini ekler.
                companyInfoCell.Add(new Paragraph("Vergi No: 1234567890").SetFont(font).SetFontSize(9)); // Vergi numarasını ekler.

                infoSectionTable.AddCell(companyInfoCell); // Şirket bilgi hücresini bilgi bölümü tablosuna ekler.

                document.Add(infoSectionTable); // Oluşturulan bilgi bölümü tablosunu belgeye ekler.

                // --- Ürün Listesi Tablosu ---
                // Sütun genişlikleri daha kompakt olacak şekilde ayarlandı
                var productTable = new Table(UnitValue.CreatePercentArray(new float[] { 3.5f, 1, 1.2f, 1.2f, 1.2f, 1.5f })) // 6 sütunlu, belirli oranlarda genişliğe sahip bir tablo oluşturur.
                    .SetWidth(UnitValue.CreatePercentValue(100)) // Tablonun genişliğini sayfanın %100'ü olarak ayarlar.
                    .SetMarginBottom(15) // Tablonun alt kenar boşluğunu 15 birime ayarlar (azaltılmış).
                    .SetBorder(new SolidBorder(lightGreyColor, 0.5f)); // Tabloya açık gri renkte, 0.5 birim kalınlığında ince bir kenarlık ekler.

                string[] headers = { "ÜRÜN ADI", "MİKTAR", "BİRİM FİYAT", "KDV (%)", "KDV TUTARI", "TOPLAM TUTAR" }; // Tablo başlıkları dizisi.
                foreach (var headerText in headers) // Her bir başlık metni için döngü.
                {
                    productTable.AddHeaderCell( // Tabloya bir başlık hücresi ekler.
                        new Cell().Add(new Paragraph(headerText)) // Başlık metnini içeren bir paragraf ekler.
                            .SetBackgroundColor(primaryColor).SetFontColor(DeviceRgb.WHITE).SetFont(semiBoldFont).SetFontSize(9) // Arka plan rengi, yazı tipi rengi, fontu ve boyutunu ayarlar.
                            .SetPadding(5) // Başlık hücrelerinin dolgusunu 5 birime ayarlar (azaltılmış).
                            .SetTextAlignment( // Metin hizalamasını ayarlar.
                                headerText == "ÜRÜN ADI" ? TextAlignment.LEFT : TextAlignment.RIGHT // "ÜRÜN ADI" ise sola, diğerleri sağa hizalı.
                            )
                    );
                }

                bool isEvenRow = false; // Satır rengini değiştirmek için kullanılan bir bayrak.
                foreach (var item in sale.SaleItems) // Her bir satış kalemi için döngü.
                {
                    var rowColor = isEvenRow ? lightGreyColor : DeviceRgb.WHITE; // Satırın rengini çift/tek duruma göre belirler (açık gri veya beyaz).
                    productTable.AddCell(new Cell().Add(new Paragraph(item.Product?.Name ?? "Bilinmeyen Ürün")).SetFont(font).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.LEFT).SetBackgroundColor(rowColor)); // Ürün adını ekler (varsa ürün adı, yoksa "Bilinmeyen Ürün").
                    productTable.AddCell(new Cell().Add(new Paragraph(item.Quantity.ToString())).SetFont(font).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor)); // Miktarı ekler.
                    productTable.AddCell(new Cell().Add(new Paragraph(item.UnitPrice.ToString("C2"))).SetFont(font).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor)); // Birim fiyatı para birimi formatında ekler.
                    productTable.AddCell(new Cell().Add(new Paragraph($"{item.Product?.TaxRate ?? 0m}%")).SetFont(font).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor)); // KDV oranını yüzde olarak ekler.
                    productTable.AddCell(new Cell().Add(new Paragraph(item.TaxAmount.ToString("C2"))).SetFont(font).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor)); // KDV tutarını para birimi formatında ekler.
                    productTable.AddCell(new Cell().Add(new Paragraph(item.TotalAmount.ToString("C2"))).SetFont(semiBoldFont).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor)); // Toplam tutarı para birimi formatında ekler (yarı kalın font).
                    isEvenRow = !isEvenRow; // Satır rengi bayrağını tersine çevirir.
                }

                document.Add(productTable); // Oluşturulan ürün listesi tablosunu belgeye ekler.

                // --- Toplamlar Bölümü ---
                var totalsTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1.5f })) // İki sütunlu, belirli oranlarda genişliğe sahip bir tablo oluşturur.
                    .SetWidth(UnitValue.CreatePercentValue(45)) // Tablonun genişliğini sayfanın %45'i olarak ayarlar (toplamlar için daha geniş).
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT) // Tablonun sağa hizalanmasını sağlar.
                    .SetMarginTop(10); // Tablonun üst kenar boşluğunu 10 birime ayarlar (azaltılmış).

                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT) // Kenarlıksız, sağa hizalı bir hücre ekler.
                    .Add(new Paragraph("Ara Toplam (KDV Hariç):").SetFont(semiBoldFont).SetFontSize(9).SetFontColor(greyColor))); // "Ara Toplam (KDV Hariç):" metnini ekler.
                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT) // Kenarlıksız, sağa hizalı bir hücre ekler.
                    .Add(new Paragraph(sale.SubTotal.ToString("C2")).SetFont(font).SetFontSize(9).SetFontColor(blackColor))); // Ara toplam değerini para birimi formatında ekler.

                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT) // Kenarlıksız, sağa hizalı bir hücre ekler.
                    .Add(new Paragraph("Toplam KDV:").SetFont(semiBoldFont).SetFontSize(9).SetFontColor(greyColor))); // "Toplam KDV:" metnini ekler.
                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT) // Kenarlıksız, sağa hizalı bir hücre ekler.
                    .Add(new Paragraph(sale.TaxTotal.ToString("C2")).SetFont(font).SetFontSize(9).SetFontColor(blackColor))); // Toplam KDV değerini para birimi formatında ekler.

                // Genel Toplam Satırı
                totalsTable.AddCell(new Cell() // Yeni bir hücre oluşturur.
                    .SetBorderTop(new SolidBorder(primaryColor, 2f)) // Üst kenarlığı ana renkte, 2 birim kalınlığında düz bir çizgi olarak ayarlar (daha kalın).
                    .SetPaddingTop(6) // Üst dolguyu 6 birime ayarlar (azaltılmış).
                    .SetBorder(Border.NO_BORDER) // Kenarlığı yok sayar (üst kenarlık hariç).
                    .SetTextAlignment(TextAlignment.RIGHT) // Metni sağa hizalar.
                    .Add(new Paragraph("GENEL TOPLAM:").SetFont(boldFont).SetFontSize(14).SetFontColor(primaryColor))); // "GENEL TOPLAM:" metnini ekler (kalın font, 14pt, ana renk).
                totalsTable.AddCell(new Cell() // Yeni bir hücre oluşturur.
                    .SetBorderTop(new SolidBorder(primaryColor, 2f)) // Üst kenarlığı ana renkte, 2 birim kalınlığında düz bir çizgi olarak ayarlar.
                    .SetPaddingTop(6) // Üst dolguyu 6 birime ayarlar.
                    .SetBorder(Border.NO_BORDER) // Kenarlığı yok sayar.
                    .SetTextAlignment(TextAlignment.RIGHT) // Metni sağa hizalar.
                    .Add(new Paragraph(sale.GrandTotal.ToString("C2")).SetFont(boldFont).SetFontSize(14).SetFontColor(primaryColor))); // Genel toplam değerini para birimi formatında ekler (kalın font, 14pt, ana renk).

                document.Add(totalsTable); // Oluşturulan toplamlar tablosunu belgeye ekler.

                // --- Notlar ve Altbilgi ---
                document.Add(new Paragraph().SetMarginTop(20) // Üst kenar boşluğu 20 olan yeni bir paragraf ekler (azaltılmış).
                    .Add(new Text("Notlar: ").SetFont(semiBoldFont).SetFontColor(primaryColor)) // "Notlar: " başlığını ekler.
                    .Add(new Text($"{sale.Notes ?? "İlginiz için teşekkür ederiz."}").SetFont(italicFont).SetFontColor(greyColor).SetFontSize(9))); // Satış notlarını veya varsayılan mesajı ekler (italik font, gri renk, 9pt).

                document.Add(new Paragraph("Bizi tercih ettiğiniz için teşekkür ederiz!").SetMarginTop(15) // Üst kenar boşluğu 15 olan teşekkür mesajı ekler (azaltılmış).
                    .SetFont(italicFont).SetFontSize(11).SetFontColor(accentColor).SetTextAlignment(TextAlignment.CENTER)); // İtalik font, 11pt, vurgu rengi ve ortaya hizalı olarak ayarlar.

                // Altbilgi Çizgisi ve Sayfa Numarası
                document.Add(new Div().SetHeight(1).SetBackgroundColor(lightGreyColor).SetMarginTop(15).SetMarginBottom(8)); // Açık gri renkte, 1 birim yüksekliğinde bir ayırıcı çizgi (Div) ekler. Üst ve alt kenar boşluklarını ayarlar.
                document.Add(new Paragraph($"Sayfa {pdf.GetPageNumber(pdf.GetLastPage())}") // "Sayfa X" şeklinde sayfa numarası ekler.
                    .SetFontSize(8).SetTextAlignment(TextAlignment.CENTER).SetFontColor(greyColor)); // Yazı tipi boyutunu 8, ortaya hizalı ve gri renk olarak ayarlar.

                document.Close(); // PDF belgesini kapatır ve tüm içeriğin yazılmasını tamamlar.

                // PDF'i wwwroot/Fatura klasörüne kaydet ve DB'yi güncelle
                var pdfBytes = stream.ToArray(); // MemoryStream'deki PDF içeriğini byte dizisine dönüştürür.
                var safeFileName = $"Fatura_{sale.SaleNumber.Replace("/", "-")}_{DateTime.Now:yyyyMMddHHmmss}.pdf"; // Güvenli bir dosya adı oluşturur (URL ve dosya yolları için eğik çizgiyi '-' ile değiştirir).

                string invoiceFolder = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "Fatura"); // Faturaların kaydedileceği wwwroot içindeki "Fatura" klasörünün tam yolunu oluşturur.

                if (!Directory.Exists(invoiceFolder)) // Eğer fatura klasörü mevcut değilse
                {
                    Directory.CreateDirectory(invoiceFolder); // Klasörü oluşturur.
                }

                string fullFilePath = System.IO.Path.Combine(invoiceFolder, safeFileName); // Tam dosya yolunu oluşturur (klasör yolu + dosya adı).

                await System.IO.File.WriteAllBytesAsync(fullFilePath, pdfBytes); // PDF byte dizisini belirtilen tam dosya yoluna asenkron olarak yazar.
                Console.WriteLine($"{fullFilePath}"); // Oluşturulan dosyanın tam yolunu konsola yazar (hata ayıklama için).

                string relativePathForDb = System.IO.Path.Combine("Fatura", safeFileName).Replace("\\", "/"); // Veritabanına kaydedilecek göreli yolu oluşturur (web yolları için ters eğik çizgiyi düz eğik çizgiyle değiştirir).

                sale.InvoiceFilePath = relativePathForDb; // Satış nesnesinin InvoiceFilePath özelliğini oluşturulan göreli dosya yolu ile günceller.
                _context.Sales.Update(sale); // Satış nesnesini veritabanında güncellenmek üzere işaretler.
                await _context.SaveChangesAsync(); // Değişiklikleri veritabanına asenkron olarak kaydeder.
                Console.WriteLine($"{sale.Id}: {relativePathForDb}"); // Satış ID'si ve kaydedilen göreli yolu konsola yazar (hata ayıklama için).

                return File(pdfBytes, "application/pdf", safeFileName); // Oluşturulan PDF byte dizisini "application/pdf" MIME tipiyle ve belirlenen dosya adıyla bir dosya olarak döndürür. Bu, tarayıcının PDF'i açmasını veya indirmesini sağlar.
            }
        }
    }
}