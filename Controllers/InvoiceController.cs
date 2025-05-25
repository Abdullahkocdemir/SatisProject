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

        // Existing Index method (Invoice List)
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

        // NEW: Invoice History Page
        public async Task<IActionResult> InvoiceHistory()
        {
            // Retrieve only sales where an invoice file path is saved
            var invoices = await _context.Sales
                .Where(s => s.InvoiceFilePath != null && s.InvoiceFilePath != "") // Filter sales with a path
                .Include(s => s.Customer) // Include customer details
                .OrderByDescending(s => s.SaleDate) // Sort by date
                .ToListAsync();

            return View(invoices); // Send the filtered list to the view
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

            // Set culture for currency formatting
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            // Define colors
            var primaryColor = new DeviceRgb(0, 102, 204); // Dark Blue (More vibrant)
            var secondaryColor = new DeviceRgb(240, 248, 255); // AliceBlue (For background)
            var accentColor = new DeviceRgb(255, 140, 0); // Dark Orange (For accent)
            var greyColor = new DeviceRgb(80, 80, 80);    // Dark Grey
            var blackColor = new DeviceRgb(0, 0, 0);      // Black
            var lightGreyColor = new DeviceRgb(245, 245, 245); // Very Light Grey (For table rows)


            // Font file names (ensure they are in your wwwroot/fonts folder)
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
                // Load fonts
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
                    boldFont = font; // Fallback to regular font if bold is not found
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
                    semiBoldFont = font; // Fallback to regular font if semi-bold is not found
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
                    italicFont = font; // Fallback to regular font if italic is not found
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"ERROR: I/O error while loading font: {ex.Message}. Using default iText fonts.");
                font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                boldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
                semiBoldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN); // No direct semi-bold in StandardFonts
                italicFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ITALIC);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Unexpected error while loading font: {ex.Message}. Using default iText fonts.");
                font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                boldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
                semiBoldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                italicFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ITALIC);
            }


            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                // Reduce page margins to gain more space
                var document = new Document(pdf, PageSize.A4);
                document.SetMargins(25, 25, 25, 25); // Reduced margins

                // --- Top Section: Company Name/Logo and Invoice Title ---
                var topHeaderTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20); // Reduced margin

                // Left side: Company Name or Logo Area
                topHeaderTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph("KOÇDEMİR YAZILIM") // Your company name or logo can go here
                        .SetFontSize(24).SetFont(boldFont).SetFontColor(primaryColor).SetTextAlignment(TextAlignment.LEFT)) // Reduced font size
                    .Add(new Paragraph("")
                        .SetFontSize(9).SetFont(italicFont).SetFontColor(greyColor).SetTextAlignment(TextAlignment.LEFT))); // Reduced font size

                // Right side: Invoice Title and No
                topHeaderTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph("FATURA")
                        .SetFontSize(40).SetFont(boldFont).SetFontColor(primaryColor).SetMarginBottom(2)) // Reduced font size and margin
                    .Add(new Paragraph($"No: {sale.SaleNumber}")
                        .SetFontSize(14).SetFont(semiBoldFont).SetFontColor(greyColor))); // Reduced font size

                document.Add(topHeaderTable);

                // Invoice Date
                document.Add(new Paragraph($"Tarih: {sale.SaleDate:dd MMMM yyyy}") // Full date format
                    .SetFontSize(10).SetFont(semiBoldFont).SetTextAlignment(TextAlignment.RIGHT).SetMarginBottom(15)); // Reduced font size and margin

                // --- Customer and Company Information Section ---
                var infoSectionTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(20); // Reduced margin

                // Customer Info Box
                var customerInfoCell = new Cell()
                    .SetBorder(new SolidBorder(primaryColor, 1)) // Add border
                    .SetBackgroundColor(secondaryColor) // Background color
                    .SetPadding(10); // Reduced padding

                customerInfoCell.Add(new Paragraph("MÜŞTERİ BİLGİLERİ")
                    .SetFont(boldFont).SetFontSize(12).SetFontColor(primaryColor).SetUnderline().SetMarginBottom(8)); // Reduced font size and margin
                customerInfoCell.Add(new Paragraph().Add(new Text("Şirket Adı: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.CompanyName}").SetFont(font)).SetFontSize(9)); // Reduced font size
                if (!string.IsNullOrWhiteSpace(sale.Customer?.ContactName))
                    customerInfoCell.Add(new Paragraph().Add(new Text("İlgili Kişi: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer.ContactName}").SetFont(font)).SetFontSize(9)); // Reduced font size
                customerInfoCell.Add(new Paragraph().Add(new Text("Adres: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.Address}, {sale.Customer?.City} / {sale.Customer?.Country}").SetFont(font)).SetFontSize(9)); // Reduced font size
                customerInfoCell.Add(new Paragraph().Add(new Text("Telefon: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.PhoneNumber}").SetFont(font)).SetFontSize(9)); // Reduced font size
                customerInfoCell.Add(new Paragraph().Add(new Text("E-posta: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.Email}").SetFont(font)).SetFontSize(9)); // Reduced font size
                customerInfoCell.Add(new Paragraph().Add(new Text("Vergi No: ").SetFont(semiBoldFont)).Add(new Text($"{sale.Customer?.TaxNumber}").SetFont(font)).SetFontSize(9)); // Reduced font size

                infoSectionTable.AddCell(customerInfoCell);

                // Company Info Box
                var companyInfoCell = new Cell()
                    .SetBorder(new SolidBorder(primaryColor, 1)) // Add border
                    .SetBackgroundColor(secondaryColor) // Background color
                    .SetPadding(10); // Reduced padding

                companyInfoCell.Add(new Paragraph("ŞİRKET BİLGİLERİ")
                    .SetFont(boldFont).SetFontSize(12).SetFontColor(primaryColor).SetUnderline().SetMarginBottom(8)); // Reduced font size and margin
                companyInfoCell.Add(new Paragraph("Koçdemir Yazılım ve Danışmanlık Ltd. Şti.").SetFont(boldFont).SetFontSize(9).SetMarginTop(6)); // Reduced font size and margin
                companyInfoCell.Add(new Paragraph("Demirciler Sanayi Sitesi No:15").SetFont(font).SetFontSize(9)); // Reduced font size
                companyInfoCell.Add(new Paragraph("Bursa, Türkiye").SetFont(font).SetFontSize(9)); // Reduced font size
                companyInfoCell.Add(new Paragraph("Tel: +90 123 456 78 90").SetFont(font).SetFontSize(9)); // Reduced font size
                companyInfoCell.Add(new Paragraph("E-posta: kcdmirapo96@gmail.com").SetFont(font).SetFontSize(9)); // Reduced font size
                companyInfoCell.Add(new Paragraph("Vergi Dairesi: Osmangazi V.D.").SetFont(font).SetFontSize(9)); // Reduced font size
                companyInfoCell.Add(new Paragraph("Vergi No: 1234567890").SetFont(font).SetFontSize(9)); // Reduced font size

                infoSectionTable.AddCell(companyInfoCell);

                document.Add(infoSectionTable);

                // --- Product List Table ---
                // Adjusted column widths to be slightly more compact
                var productTable = new Table(UnitValue.CreatePercentArray(new float[] { 3.5f, 1, 1.2f, 1.2f, 1.2f, 1.5f }))
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetMarginBottom(15) // Reduced margin
                    .SetBorder(new SolidBorder(lightGreyColor, 0.5f)); // Thin border around the table

                string[] headers = { "ÜRÜN ADI", "MİKTAR", "BİRİM FİYAT", "KDV (%)", "KDV TUTARI", "TOPLAM TUTAR" };
                foreach (var headerText in headers)
                {
                    productTable.AddHeaderCell(
                        new Cell().Add(new Paragraph(headerText))
                            .SetBackgroundColor(primaryColor).SetFontColor(DeviceRgb.WHITE).SetFont(semiBoldFont).SetFontSize(9) // Reduced font size
                            .SetPadding(5) // Reduced padding for header cells
                            .SetTextAlignment(
                                headerText == "ÜRÜN ADI" ? TextAlignment.LEFT : TextAlignment.RIGHT
                            )
                    );
                }

                bool isEvenRow = false;
                foreach (var item in sale.SaleItems)
                {
                    var rowColor = isEvenRow ? lightGreyColor : DeviceRgb.WHITE; // Alternate row color
                    productTable.AddCell(new Cell().Add(new Paragraph(item.Product?.Name ?? "Bilinmeyen Ürün")).SetFont(font).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.LEFT).SetBackgroundColor(rowColor)); // Reduced font size and padding
                    productTable.AddCell(new Cell().Add(new Paragraph(item.Quantity.ToString())).SetFont(font).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor)); // Reduced font size and padding
                    productTable.AddCell(new Cell().Add(new Paragraph(item.UnitPrice.ToString("C2"))).SetFont(font).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor)); // Reduced font size and padding
                    productTable.AddCell(new Cell().Add(new Paragraph($"{item.Product?.TaxRate ?? 0m}%")).SetFont(font).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor)); // Reduced font size and padding
                    productTable.AddCell(new Cell().Add(new Paragraph(item.TaxAmount.ToString("C2"))).SetFont(font).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor)); // Reduced font size and padding
                    productTable.AddCell(new Cell().Add(new Paragraph(item.TotalAmount.ToString("C2"))).SetFont(semiBoldFont).SetFontSize(8).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(rowColor)); // Reduced font size and padding
                    isEvenRow = !isEvenRow; // Toggle row color
                }

                document.Add(productTable);

                // --- Totals Section ---
                var totalsTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1.5f }))
                    .SetWidth(UnitValue.CreatePercentValue(45)) // Slightly wider for totals
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                    .SetMarginTop(10); // Reduced top margin for spacing

                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph("Ara Toplam (KDV Hariç):").SetFont(semiBoldFont).SetFontSize(9).SetFontColor(greyColor))); // Reduced font size
                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph(sale.SubTotal.ToString("C2")).SetFont(font).SetFontSize(9).SetFontColor(blackColor))); // Reduced font size

                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph("Toplam KDV:").SetFont(semiBoldFont).SetFontSize(9).SetFontColor(greyColor))); // Reduced font size
                totalsTable.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph(sale.TaxTotal.ToString("C2")).SetFont(font).SetFontSize(9).SetFontColor(blackColor))); // Reduced font size

                // Grand Total Row
                totalsTable.AddCell(new Cell()
                    .SetBorderTop(new SolidBorder(primaryColor, 2f)) // Thicker border
                    .SetPaddingTop(6) // Reduced padding
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph("GENEL TOPLAM:").SetFont(boldFont).SetFontSize(14).SetFontColor(primaryColor))); // Reduced font size
                totalsTable.AddCell(new Cell()
                    .SetBorderTop(new SolidBorder(primaryColor, 2f)) // Thicker border
                    .SetPaddingTop(6) // Reduced padding
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .Add(new Paragraph(sale.GrandTotal.ToString("C2")).SetFont(boldFont).SetFontSize(14).SetFontColor(primaryColor))); // Reduced font size

                document.Add(totalsTable);

                // --- Notes and Footer ---
                document.Add(new Paragraph().SetMarginTop(20) // Reduced margin
                    .Add(new Text("Notlar: ").SetFont(semiBoldFont).SetFontColor(primaryColor))
                    .Add(new Text($"{sale.Notes ?? "İlginiz için teşekkür ederiz."}").SetFont(italicFont).SetFontColor(greyColor).SetFontSize(9))); // Reduced font size

                document.Add(new Paragraph("Bizi tercih ettiğiniz için teşekkür ederiz!").SetMarginTop(15) // Reduced margin
                    .SetFont(italicFont).SetFontSize(11).SetFontColor(accentColor).SetTextAlignment(TextAlignment.CENTER)); // Centered thank you message

                // Footer Line and Page Number
                document.Add(new Div().SetHeight(1).SetBackgroundColor(lightGreyColor).SetMarginTop(15).SetMarginBottom(8)); // Separator line, reduced margins
                document.Add(new Paragraph($"Sayfa {pdf.GetPageNumber(pdf.GetLastPage())}")
                    .SetFontSize(8).SetTextAlignment(TextAlignment.CENTER).SetFontColor(greyColor)); // Reduced font size

                document.Close();

                // Save PDF to wwwroot/Fatura folder and update DB
                var pdfBytes = stream.ToArray();
                var safeFileName = $"Fatura_{sale.SaleNumber.Replace("/", "-")}_{DateTime.Now:yyyyMMddHHmmss}.pdf"; // Safe file name for URL/path

                string invoiceFolder = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "Fatura");

                if (!Directory.Exists(invoiceFolder))
                {
                    Directory.CreateDirectory(invoiceFolder);
                }

                string fullFilePath = System.IO.Path.Combine(invoiceFolder, safeFileName);

                await System.IO.File.WriteAllBytesAsync(fullFilePath, pdfBytes);
                Console.WriteLine($"{fullFilePath}");

                string relativePathForDb = System.IO.Path.Combine("Fatura", safeFileName).Replace("\\", "/"); // Use forward slashes for web paths

                sale.InvoiceFilePath = relativePathForDb;
                _context.Sales.Update(sale);
                await _context.SaveChangesAsync();
                Console.WriteLine($"{sale.Id}: {relativePathForDb}");

                return File(pdfBytes, "application/pdf", safeFileName);
            }
        }
    }
}