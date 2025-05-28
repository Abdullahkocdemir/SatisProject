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

        public async Task<IActionResult> InvoiceHistory()
        {
            var invoices = await _context.Sales
                .Where(s => s.InvoiceFilePath != null && s.InvoiceFilePath != "")
                .Include(s => s.Customer)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return View(invoices);
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

            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            var primaryColor = new DeviceRgb(0, 102, 204);
            var secondaryColor = new DeviceRgb(240, 248, 255);
            var accentColor = new DeviceRgb(255, 140, 0);
            var greyColor = new DeviceRgb(80, 80, 80);
            var blackColor = new DeviceRgb(0, 0, 0);
            var lightGreyColor = new DeviceRgb(245, 245, 245);


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
                string regularFontPath = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "fonts", regularFontFileName);
                if (System.IO.File.Exists(regularFontPath))
                {
                    font = PdfFontFactory.CreateFont(regularFontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    Console.WriteLine($"DEBUG: Normal font yüklendi: {regularFontPath}");
                }
                else
                {
                    Console.WriteLine($"WARNING: Normal font dosyası bulunamadı: {regularFontPath}. Varsayılan Times Roman kullanılıyor.");
                    font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                }

                string boldFontPath = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "fonts", boldFontFileName);
                if (System.IO.File.Exists(boldFontPath))
                {
                    boldFont = PdfFontFactory.CreateFont(boldFontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    Console.WriteLine($"DEBUG: Kalın font yüklendi: {boldFontPath}");
                }
                else
                {
                    Console.WriteLine($"WARNING: Kalın font dosyası bulunamadı: {boldFontPath}. Normal font kullanılıyor (kalın olarak).");
                    boldFont = font;
                }

                string semiBoldFontPath = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "fonts", semiBoldFontFileName);
                if (System.IO.File.Exists(semiBoldFontPath))
                {
                    semiBoldFont = PdfFontFactory.CreateFont(semiBoldFontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    Console.WriteLine($"DEBUG: Yarı kalın font yüklendi: {semiBoldFontPath}");
                }
                else
                {
                    Console.WriteLine($"WARNING: Yarı kalın font dosyası bulunamadı: {semiBoldFontPath}. Normal font kullanılıyor (yarı kalın olarak).");
                    semiBoldFont = font;
                }

                string italicFontPath = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "fonts", italicFontFileName);
                if (System.IO.File.Exists(italicFontPath))
                {
                    italicFont = PdfFontFactory.CreateFont(italicFontPath, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    Console.WriteLine($"DEBUG: İtalik font yüklendi: {italicFontPath}");
                }
                else
                {
                    Console.WriteLine($"WARNING: İtalik font dosyası bulunamadı: {italicFontPath}. Normal font kullanılıyor (italik olarak).");
                    italicFont = font;
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"HATA: Font yüklenirken G/Ç hatası oluştu: {ex.Message}. Varsayılan iText fontları kullanılıyor.");
                font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                boldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
                semiBoldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                italicFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ITALIC);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HATA: Font yüklenirken beklenmedik bir hata oluştu: {ex.Message}. Varsayılan iText fontları kullanılıyor.");
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
                document.SetMargins(25, 25, 25, 25);

                var topHeaderTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20);

                topHeaderTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph("KOÇDEMİR YAZILIM")
                        .SetFontSize(24).SetFont(boldFont).SetFontColor(primaryColor).SetTextAlignment(TextAlignment.LEFT))
                    .Add(new Paragraph("")
                        .SetFontSize(9).SetFont(italicFont).SetFontColor(greyColor).SetTextAlignment(TextAlignment.LEFT)));

                topHeaderTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph("FATURA")
                        .SetFontSize(40).SetFont(boldFont).SetFontColor(primaryColor).SetMarginBottom(2))
                    .Add(new Paragraph($"No: {sale.SaleNumber}")
                        .SetFontSize(14).SetFont(semiBoldFont).SetFontColor(greyColor)));

                document.Add(topHeaderTable);

                document.Add(new Paragraph($"Tarih: {sale.SaleDate:dd MMMM yyyy}")
                    .SetFontSize(10).SetFont(semiBoldFont).SetTextAlignment(TextAlignment.RIGHT).SetMarginBottom(15));

                var infoSectionTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20);

                var customerInfoCell = new Cell()
                    .SetBorder(new SolidBorder(primaryColor, 1))
                    .SetBackgroundColor(secondaryColor)
                    .SetPadding(10);

                customerInfoCell.Add(new Paragraph("MÜŞTERİ BİLGİLERİ")
                    .SetFont(boldFont).SetFontSize(12).SetFontColor(primaryColor).SetUnderline().SetMarginBottom(8));
                customerInfoCell.Add(new Paragraph().Add(new Text("Şirket Adı: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.CompanyName}").SetFont(font)).SetFontSize(9));
                if (!string.IsNullOrWhiteSpace(sale.Customer?.ContactName))
                    customerInfoCell.Add(new Paragraph().Add(new Text("İlgili Kişi: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer.ContactName}").SetFont(font)).SetFontSize(9));
                customerInfoCell.Add(new Paragraph().Add(new Text("Adres: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.Address}, {sale.Customer?.City} / {sale.Customer?.Country}").SetFont(font)).SetFontSize(9));
                customerInfoCell.Add(new Paragraph().Add(new Text("Telefon: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.PhoneNumber}").SetFont(font)).SetFontSize(9));
                customerInfoCell.Add(new Paragraph().Add(new Text("E-posta: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.Email}").SetFont(font)).SetFontSize(9));
                customerInfoCell.Add(new Paragraph().Add(new Text("Vergi No: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.TaxNumber}").SetFont(font)).SetFontSize(9));

                infoSectionTable.AddCell(customerInfoCell);

                var companyInfoCell = new Cell()
                    .SetBorder(new SolidBorder(primaryColor, 1))
                    .SetBackgroundColor(secondaryColor)
                    .SetPadding(10);

                companyInfoCell.Add(new Paragraph("ŞİRKET BİLGİLERİ")
                    .SetFont(boldFont).SetFontSize(12).SetFontColor(primaryColor).SetUnderline().SetMarginBottom(8));
                companyInfoCell.Add(new Paragraph("Koçdemir Yazılım ve Danışmanlık Ltd. Şti.").SetFont(boldFont).SetFontSize(9).SetMarginTop(6));
                companyInfoCell.Add(new Paragraph("Demirciler Sanayi Sitesi No:15").SetFont(font).SetFontSize(9));
                companyInfoCell.Add(new Paragraph("Bursa, Türkiye").SetFont(font).SetFontSize(9));
                companyInfoCell.Add(new Paragraph("Tel: +90 123 456 78 90").SetFont(font).SetFontSize(9));
                companyInfoCell.Add(new Paragraph("E-posta: kcdmirapo96@gmail.com").SetFont(font).SetFontSize(9));
                companyInfoCell.Add(new Paragraph("Vergi Dairesi: Osmangazi V.D.").SetFont(font).SetFontSize(9));
                companyInfoCell.Add(new Paragraph("Vergi No: 1234567890").SetFont(font).SetFontSize(9));

                infoSectionTable.AddCell(companyInfoCell);

                document.Add(infoSectionTable);

                var productTable = new Table(UnitValue.CreatePercentArray(new float[] { 3.5f, 1, 1.2f, 1.2f, 1.2f, 1.5f }))
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(15)
                    .SetBorder(new SolidBorder(lightGreyColor, 0.5f));

                string[] headers = { "ÜRÜN ADI", "MİKTAR", "BİRİM FİYAT", "KDV (%)", "KDV TUTARI", "TOPLAM TUTAR" };
                foreach (var headerText in headers)
                {
                    productTable.AddHeaderCell(
                        new Cell().Add(new Paragraph(headerText))
                            .SetBackgroundColor(primaryColor).SetFontColor(DeviceRgb.WHITE).SetFont(semiBoldFont).SetFontSize(9)
                            .SetPadding(5)
                            .SetTextAlignment(
                                headerText == "ÜRÜN ADI" ? TextAlignment.LEFT : TextAlignment.RIGHT
                            )
                    );
                }

                bool isEvenRow = false;
                foreach (var item in sale.SaleItems)
                {
                    var rowColor = isEvenRow ? lightGreyColor : DeviceRgb.WHITE;
                    productTable.AddCell(new Cell().Add(new Paragraph(item.Product?.Name ?? "Bilinmeyen Ürün")).SetFont(font).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.LEFT).SetBackgroundColor(rowColor));
                    productTable.AddCell(new Cell().Add(new Paragraph(item.Quantity.ToString())).SetFont(font).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor));
                    productTable.AddCell(new Cell().Add(new Paragraph(item.UnitPrice.ToString("C2"))).SetFont(font).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor));
                    productTable.AddCell(new Cell().Add(new Paragraph($"{item.Product?.TaxRate ?? 0m}%")).SetFont(font).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor));
                    productTable.AddCell(new Cell().Add(new Paragraph(item.TaxAmount.ToString("C2"))).SetFont(font).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor));
                    productTable.AddCell(new Cell().Add(new Paragraph(item.TotalAmount.ToString("C2"))).SetFont(semiBoldFont).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor));
                    isEvenRow = !isEvenRow;
                }

                document.Add(productTable);

                var totalsTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1.5f }))
                    .SetWidth(UnitValue.CreatePercentValue(45))
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                    .SetMarginTop(10);

                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph("Ara Toplam (KDV Hariç):").SetFont(semiBoldFont).SetFontSize(9).SetFontColor(greyColor)));
                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph(sale.SubTotal.ToString("C2")).SetFont(font).SetFontSize(9).SetFontColor(blackColor)));

                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph("Toplam KDV:").SetFont(semiBoldFont).SetFontSize(9).SetFontColor(greyColor)));
                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph(sale.TaxTotal.ToString("C2")).SetFont(font).SetFontSize(9).SetFontColor(blackColor)));

                totalsTable.AddCell(new Cell()
                    .SetBorderTop(new SolidBorder(primaryColor, 2f))
                    .SetPaddingTop(6)
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph("GENEL TOPLAM:").SetFont(boldFont).SetFontSize(14).SetFontColor(primaryColor)));
                totalsTable.AddCell(new Cell()
                    .SetBorderTop(new SolidBorder(primaryColor, 2f))
                    .SetPaddingTop(6)
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph(sale.GrandTotal.ToString("C2")).SetFont(boldFont).SetFontSize(14).SetFontColor(primaryColor)));

                document.Add(totalsTable);

                document.Add(new Paragraph().SetMarginTop(20)
                    .Add(new Text("Notlar: ").SetFont(semiBoldFont).SetFontColor(primaryColor))
                    .Add(new Text($"{sale.Notes ?? "İlginiz için teşekkür ederiz."}").SetFont(italicFont).SetFontColor(greyColor).SetFontSize(9)));

                document.Add(new Paragraph("Bizi tercih ettiğiniz için teşekkür ederiz!").SetMarginTop(15)
                    .SetFont(italicFont).SetFontSize(11).SetFontColor(accentColor).SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Div().SetHeight(1).SetBackgroundColor(lightGreyColor).SetMarginTop(15).SetMarginBottom(8));
                document.Add(new Paragraph($"Sayfa {pdf.GetPageNumber(pdf.GetLastPage())}")
                    .SetFontSize(8).SetTextAlignment(TextAlignment.CENTER).SetFontColor(greyColor));

                document.Close();

                var pdfBytes = stream.ToArray();
                var safeFileName = $"Fatura_{sale.SaleNumber.Replace("/", "-")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

                string invoiceFolder = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "Fatura");

                if (!Directory.Exists(invoiceFolder))
                {
                    Directory.CreateDirectory(invoiceFolder);
                }

                string fullFilePath = System.IO.Path.Combine(invoiceFolder, safeFileName);

                await System.IO.File.WriteAllBytesAsync(fullFilePath, pdfBytes);
                Console.WriteLine($"{fullFilePath}");

                string relativePathForDb = System.IO.Path.Combine("Fatura", safeFileName).Replace("\\", "/");

                sale.InvoiceFilePath = relativePathForDb;
                _context.Sales.Update(sale);
                await _context.SaveChangesAsync();
                Console.WriteLine($"{sale.Id}: {relativePathForDb}");

                return File(pdfBytes, "application/pdf", safeFileName);
            }
        }
    }
}