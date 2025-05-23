using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;
using SatışProject.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Layout.Borders;
using iText.IO.Font.Constants;
using iText.IO.Font;

using Microsoft.AspNetCore.Hosting;

namespace SatışProject.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly SatısContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public InvoiceController(SatısContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // Mevcut Index metodu (Fatura Listesi)
        public async Task<IActionResult> Index()
        {
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Employee)
                    .ThenInclude(e => e!.AppUser)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return View(sales);
        }

        // YENİ: Fatura Geçmişi Sayfası
        public async Task<IActionResult> InvoiceHistory()
        {
            // Sadece fatura dosya yolu kaydedilmiş satışları getir
            var invoices = await _context.Sales
                .Where(s => s.InvoiceFilePath != null && s.InvoiceFilePath != "") // Yolu olan satışları filtrele
                .Include(s => s.Customer) // Müşteri detaylarını dahil et
                .OrderByDescending(s => s.SaleDate) // Tarihe göre sırala
                .ToListAsync();

            return View(invoices); // Filtrelenmiş listeyi view'e gönder
        }


        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
        public async Task<IActionResult> GenerateInvoicePdf(int saleId)
        {
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
            {
                return NotFound($"Satış bulunamadı. ID: {saleId}");
            }

            // Para birimi formatı için kültürü ayarla
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            // Renkleri tanımla
            var primaryColor = new DeviceRgb(0, 102, 204); // Koyu Mavi (Daha Canlı)
            var secondaryColor = new DeviceRgb(240, 248, 255); // AliceBlue (Arka Plan için)
            var accentColor = new DeviceRgb(255, 140, 0); // Koyu Turuncu (Vurgu için)
            var greyColor = new DeviceRgb(80, 80, 80);    // Koyu Gri
            var blackColor = new DeviceRgb(0, 0, 0);      // Siyah
            var lightGreyColor = new DeviceRgb(245, 245, 245); // Çok Açık Gri (Tablo Satırları için)


            // Font dosyalarının adları (wwwroot/fonts klasörünüzde olduğundan emin olun)
            string regularFontFileName = "OpenSans-VariableFont_wdth,wght.ttf";
            string semiBoldFontFileName = "OpenSans_Condensed-SemiBold.ttf";
            string boldFontFileName = "OpenSans_Condensed-ExtraBold.ttf";
            string italicFontFileName = "OpenSans-Italic-VariableFont_wdth,wght.ttf";

            PdfFont font;
            PdfFont boldFont;
            PdfFont semiBoldFont;
            PdfFont italicFont = null!;

            try
            {
                // Fontları yükle
                string regularFontPath = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "fonts", regularFontFileName);
                if (System.IO.File.Exists(regularFontPath))
                {
                    font = PdfFontFactory.CreateFont(regularFontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    Console.WriteLine($"DEBUG: Normal font loaded: {regularFontPath}");
                }
                else
                {
                    Console.WriteLine($"WARNING: Normal font file not found: {regularFontPath}. Using default Times Roman.");
                    font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                }

                string boldFontPath = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "fonts", boldFontFileName);
                if (System.IO.File.Exists(boldFontPath))
                {
                    boldFont = PdfFontFactory.CreateFont(boldFontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    Console.WriteLine($"DEBUG: Bold font loaded: {boldFontPath}");
                }
                else
                {
                    Console.WriteLine($"WARNING: Bold font file not found: {boldFontPath}. Using regular font (as bold).");
                    boldFont = font; // Kalın font bulunamazsa normal fonta geri dön
                }

                string semiBoldFontPath = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "fonts", semiBoldFontFileName);
                if (System.IO.File.Exists(semiBoldFontPath))
                {
                    semiBoldFont = PdfFontFactory.CreateFont(semiBoldFontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    Console.WriteLine($"DEBUG: Semi-bold font loaded: {semiBoldFontPath}");
                }
                else
                {
                    Console.WriteLine($"WARNING: Semi-bold font file not found: {semiBoldFontPath}. Using regular font (as semi-bold).");
                    semiBoldFont = font; // Yarı-kalın font bulunamazsa normal fonta geri dön
                }

                string italicFontPath = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "fonts", italicFontFileName);
                if (System.IO.File.Exists(italicFontPath))
                {
                    italicFont = PdfFontFactory.CreateFont(italicFontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    Console.WriteLine($"DEBUG: Italic font loaded: {italicFontPath}");
                }
                else
                {
                    Console.WriteLine($"WARNING: Italic font file not found: {italicFontPath}. Using regular font (as italic).");
                    italicFont = font; // İtalik font bulunamazsa normal fonta geri dön
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"ERROR: Font yüklenirken I/O hatası: {ex.Message}. Varsayılan iText fontları kullanılıyor.");
                font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                boldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
                semiBoldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN); // StandardFonts'ta doğrudan yarı-kalın yok
                italicFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ITALIC);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Font yüklenirken beklenmeyen hata: {ex.Message}. Varsayılan iText fontları kullanılıyor.");
                font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                boldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
                semiBoldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                italicFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ITALIC);
            }


            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf, PageSize.A4);

                document.SetMargins(40, 40, 40, 40); // Standart kenar boşlukları

                // --- Üst Kısım: Şirket Adı/Logo ve Fatura Başlığı ---
                var topHeaderTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(30);

                // Sol taraf: Şirket Adı veya Logo Alanı
                topHeaderTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph("KOÇDEMİR YAZILIM") // Buraya şirket logonuz veya daha büyük şirket adınız gelebilir
                        .SetFontSize(28).SetFont(boldFont).SetFontColor(primaryColor).SetTextAlignment(TextAlignment.LEFT))
                    .Add(new Paragraph("")
                        .SetFontSize(10).SetFont(italicFont).SetFontColor(greyColor).SetTextAlignment(TextAlignment.LEFT)));

                // Sağ taraf: Fatura Başlığı ve No
                topHeaderTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph("FATURA")
                        .SetFontSize(48).SetFont(boldFont).SetFontColor(primaryColor).SetMarginBottom(5))
                    .Add(new Paragraph($"No: {sale.SaleNumber}")
                        .SetFontSize(16).SetFont(semiBoldFont).SetFontColor(greyColor)));

                document.Add(topHeaderTable);

                // Fatura Tarihi
                document.Add(new Paragraph($"Tarih: {sale.SaleDate:dd MMMM yyyy}") // Tam tarih formatı
                    .SetFontSize(11).SetFont(semiBoldFont).SetTextAlignment(TextAlignment.RIGHT).SetMarginBottom(20));

                // --- Müşteri ve Şirket Bilgileri Bölümü ---
                var infoSectionTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(30);

                // Müşteri Bilgileri Kutusu
                var customerInfoCell = new Cell()
                    .SetBorder(new SolidBorder(primaryColor, 1)) // Kenarlık ekle
                    .SetBackgroundColor(secondaryColor) // Arka plan rengi
                    .SetPadding(15);

                customerInfoCell.Add(new Paragraph("MÜŞTERİ BİLGİLERİ")
                    .SetFont(boldFont).SetFontSize(14).SetFontColor(primaryColor).SetUnderline().SetMarginBottom(10));
                customerInfoCell.Add(new Paragraph().Add(new Text("Şirket Adı: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.CompanyName}").SetFont(font)).SetFontSize(10));
                if (!string.IsNullOrWhiteSpace(sale.Customer?.ContactName))
                    customerInfoCell.Add(new Paragraph().Add(new Text("İlgili Kişi: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer.ContactName}").SetFont(font)).SetFontSize(10));
                customerInfoCell.Add(new Paragraph().Add(new Text("Adres: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.Address}, {sale.Customer?.City} / {sale.Customer?.Country}").SetFont(font)).SetFontSize(10));
                customerInfoCell.Add(new Paragraph().Add(new Text("Telefon: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.PhoneNumber}").SetFont(font)).SetFontSize(10));
                customerInfoCell.Add(new Paragraph().Add(new Text("E-posta: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.Email}").SetFont(font)).SetFontSize(10));
                customerInfoCell.Add(new Paragraph().Add(new Text("Vergi No: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.TaxNumber}").SetFont(font)).SetFontSize(10));

                infoSectionTable.AddCell(customerInfoCell);

                // Şirket Bilgileri Kutusu
                var companyInfoCell = new Cell()
                    .SetBorder(new SolidBorder(primaryColor, 1)) // Kenarlık ekle
                    .SetBackgroundColor(secondaryColor) // Arka plan rengi
                    .SetPadding(15);

                companyInfoCell.Add(new Paragraph("ŞİRKET BİLGİLERİ")
                    .SetFont(boldFont).SetFontSize(14).SetFontColor(primaryColor).SetUnderline().SetMarginBottom(10));
                companyInfoCell.Add(new Paragraph("Koçdemir Yazılım ve Danışmanlık Ltd. Şti.").SetFont(boldFont).SetFontSize(10).SetMarginTop(8));
                companyInfoCell.Add(new Paragraph("Demirciler Sanayi Sitesi No:15").SetFont(font).SetFontSize(10));
                companyInfoCell.Add(new Paragraph("Bursa, Türkiye").SetFont(font).SetFontSize(10));
                companyInfoCell.Add(new Paragraph("Tel: +90 123 456 78 90").SetFont(font).SetFontSize(10));
                companyInfoCell.Add(new Paragraph("E-posta: kcdmirapo96@gmail.com").SetFont(font).SetFontSize(10));
                companyInfoCell.Add(new Paragraph("Vergi Dairesi: Osmangazi V.D.").SetFont(font).SetFontSize(10));
                companyInfoCell.Add(new Paragraph("Vergi No: 1234567890").SetFont(font).SetFontSize(10));

                infoSectionTable.AddCell(companyInfoCell);

                document.Add(infoSectionTable);

                // --- Ürün Listesi Tablosu ---
                var productTable = new Table(UnitValue.CreatePercentArray(new float[] { 4, 1, 1.5f, 1.2f, 1.5f, 2 }))
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(25)
                    .SetBorder(new SolidBorder(lightGreyColor, 0.5f)); // Tablo etrafında ince kenarlık

                string[] headers = { "ÜRÜN ADI", "MİKTAR", "BİRİM FİYAT", "KDV (%)", "KDV TUTARI", "TOPLAM TUTAR" };
                foreach (var headerText in headers)
                {
                    productTable.AddHeaderCell(
                        new Cell().Add(new Paragraph(headerText))
                            .SetBackgroundColor(primaryColor).SetFontColor(DeviceRgb.WHITE).SetFont(semiBoldFont).SetFontSize(10)
                            .SetPadding(8) // Başlık hücreleri için artırılmış dolgu
                            .SetTextAlignment(
                                headerText == "ÜRÜN ADI" ? TextAlignment.LEFT : TextAlignment.RIGHT
                            )
                    );
                }

                bool isEvenRow = false;
                foreach (var item in sale.SaleItems)
                {
                    var rowColor = isEvenRow ? lightGreyColor : DeviceRgb.WHITE; // Alternatif satır rengi
                    productTable.AddCell(new Cell().Add(new Paragraph(item.Product?.Name ?? "Bilinmeyen Ürün")).SetFont(font).SetFontSize(9).SetPadding(7).SetTextAlignment(TextAlignment.LEFT).SetBackgroundColor(rowColor));
                    productTable.AddCell(new Cell().Add(new Paragraph(item.Quantity.ToString())).SetFont(font).SetFontSize(9).SetPadding(7).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor));
                    productTable.AddCell(new Cell().Add(new Paragraph(item.UnitPrice.ToString("C2"))).SetFont(font).SetFontSize(9).SetPadding(7).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor));
                    productTable.AddCell(new Cell().Add(new Paragraph($"{item.Product?.TaxRate ?? 0m}%")).SetFont(font).SetFontSize(9).SetPadding(7).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor));
                    productTable.AddCell(new Cell().Add(new Paragraph(item.TaxAmount.ToString("C2"))).SetFont(font).SetFontSize(9).SetPadding(7).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor));
                    productTable.AddCell(new Cell().Add(new Paragraph(item.TotalAmount.ToString("C2"))).SetFont(semiBoldFont).SetFontSize(9).SetPadding(7).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor));
                    isEvenRow = !isEvenRow; // Satır rengini değiştir
                }

                document.Add(productTable);

                // --- Toplamlar Bölümü ---
                var totalsTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1.5f }))
                    .SetWidth(UnitValue.CreatePercentValue(45)) // Toplamlar için biraz daha geniş
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                    .SetMarginTop(15); // Boşluk için üst kenar boşluğu eklendi

                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph("Ara Toplam (KDV Hariç):").SetFont(semiBoldFont).SetFontSize(10).SetFontColor(greyColor)));
                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph(sale.SubTotal.ToString("C2")).SetFont(font).SetFontSize(10).SetFontColor(blackColor)));

                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph("Toplam KDV:").SetFont(semiBoldFont).SetFontSize(10).SetFontColor(greyColor)));
                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph(sale.TaxTotal.ToString("C2")).SetFont(font).SetFontSize(10).SetFontColor(blackColor)));

                // Genel Toplam Satırı
                totalsTable.AddCell(new Cell()
                    .SetBorderTop(new SolidBorder(primaryColor, 2f)) // Kalın kenarlık
                    .SetPaddingTop(8)
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph("GENEL TOPLAM:").SetFont(boldFont).SetFontSize(16).SetFontColor(primaryColor))); // Daha büyük, kalın, ana renk
                totalsTable.AddCell(new Cell()
                    .SetBorderTop(new SolidBorder(primaryColor, 2f)) // Kalın kenarlık
                    .SetPaddingTop(8)
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph(sale.GrandTotal.ToString("C2")).SetFont(boldFont).SetFontSize(16).SetFontColor(primaryColor)));

                document.Add(totalsTable);

                // --- Notlar ve Alt Bilgi ---
                document.Add(new Paragraph().SetMarginTop(30)
                    .Add(new Text("Notlar: ").SetFont(semiBoldFont).SetFontColor(primaryColor))
                    .Add(new Text($"{sale.Notes ?? "İlginiz için teşekkür ederiz."}").SetFont(italicFont).SetFontColor(greyColor).SetFontSize(10)));

                document.Add(new Paragraph("Bizi tercih ettiğiniz için teşekkür ederiz!").SetMarginTop(20)
                    .SetFont(italicFont).SetFontSize(12).SetFontColor(accentColor).SetTextAlignment(TextAlignment.CENTER)); // Ortalanmış teşekkür mesajı

                // Alt Bilgi Çizgisi ve Sayfa Numarası
                document.Add(new Div().SetHeight(1).SetBackgroundColor(lightGreyColor).SetMarginTop(20).SetMarginBottom(10)); // Ayırıcı çizgi
                document.Add(new Paragraph($"Sayfa {pdf.GetPageNumber(pdf.GetLastPage())}")
                    .SetFontSize(9).SetTextAlignment(TextAlignment.CENTER).SetFontColor(greyColor));

                document.Close();

                // PDF'i wwwroot/Fatura klasörüne kaydet ve DB'yi güncelle
                var pdfBytes = stream.ToArray();
                var safeFileName = $"Fatura_{sale.SaleNumber.Replace("/", "-")}_{DateTime.Now:yyyyMMddHHmmss}.pdf"; // URL/path için güvenli dosya adı

                string invoiceFolder = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "Fatura");

                if (!Directory.Exists(invoiceFolder))
                {
                    Directory.CreateDirectory(invoiceFolder);
                }

                string fullFilePath = System.IO.Path.Combine(invoiceFolder, safeFileName);

                await System.IO.File.WriteAllBytesAsync(fullFilePath, pdfBytes);
                Console.WriteLine($"DEBUG: Invoice saved to: {fullFilePath}");

                string relativePathForDb = System.IO.Path.Combine("Fatura", safeFileName).Replace("\\", "/"); // Web yolları için ileri eğik çizgi kullan

                sale.InvoiceFilePath = relativePathForDb;
                _context.Sales.Update(sale);
                await _context.SaveChangesAsync();
                Console.WriteLine($"DEBUG: Invoice path saved to database for Sale ID {sale.Id}: {relativePathForDb}");

                return File(pdfBytes, "application/pdf", safeFileName);
            }
        }
    }
}