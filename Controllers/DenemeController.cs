using Microsoft.AspNetCore.Mvc;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace SatışProject.Controllers
{
    public class DenemeController : Controller
    {
        private static List<SatışProject.Entities.Product> _products = new List<SatışProject.Entities.Product>
    {
        // Category ve Brand nesnelerini Product'a ekleyebilmeniz için
        // onları da tanımlamanız gerekir. Bu bir örnektir.
        new SatışProject.Entities.Product { ProductId = 1, Name = "Bosch Profesyonel Akülü Matkap", UnitPrice = 1250.00M, ImageUrl = "https://via.placeholder.com/300x240?text=Akulu+Matkap", StockQuantity = 10, CategoryId = 1, SKU = "BOSCH001", Barcode = "1234567890123", CostPrice = 1000.00M,
             Category = new SatışProject.Entities.Category { CategoryId = 1, Name = "Elektrikli El Aletleri" } }, // Category ekledik
        new SatışProject.Entities.Product { ProductId = 2, Name = "Dewalt Darbeli Somun Sıkma", UnitPrice = 980.00M, ImageUrl = "https://via.placeholder.com/300x240?text=Darbeli+Somun+Sikma", StockQuantity = 5, CategoryId = 1, SKU = "DEWALT001", Barcode = "1234567890124", CostPrice = 800.00M,
             Category = new SatışProject.Entities.Category { CategoryId = 1, Name = "Elektrikli El Aletleri" } },
        // ... diğer ürünler (Category ve Brand nesnelerini eklemeyi unutmayın)
        new SatışProject.Entities.Product { ProductId = 3, Name = "Stanley 52 Parça Lokma Takım Seti", UnitPrice = 450.00M, ImageUrl = "https://via.placeholder.com/300x240?text=Lokma+Takim+Seti", StockQuantity = 20, CategoryId = 2, SKU = "STANLEY001", Barcode = "1234567890125", CostPrice = 350.00M,
             Category = new SatışProject.Entities.Category { CategoryId = 2, Name = "Manuel El Aletleri" } },
        new SatışProject.Entities.Product { ProductId = 4, Name = "Makita Daire Testere", UnitPrice = 1800.00M, ImageUrl = "https://via.placeholder.com/300x240?text=Daire+Testere", StockQuantity = 8, CategoryId = 1, SKU = "MAKITA001", Barcode = "1234567890126", CostPrice = 1500.00M,
             Category = new SatışProject.Entities.Category { CategoryId = 1, Name = "Elektrikli El Aletleri" } },
        new SatışProject.Entities.Product { ProductId = 5, Name = "Fenix Taktik El Feneri", UnitPrice = 280.00M, ImageUrl = "https://via.placeholder.com/300x240?text=El+Feneri", StockQuantity = 15, CategoryId = 3, SKU = "FENIX001", Barcode = "1234567890127", CostPrice = 200.00M,
             Category = new SatışProject.Entities.Category { CategoryId = 3, Name = "İş Güvenlik Ekipmanları" } },
        new SatışProject.Entities.Product { ProductId = 6, Name = "Gedore Ayarlı İngiliz Anahtarı", UnitPrice = 120.00M, ImageUrl = "https://via.placeholder.com/300x240?text=Ingiliz+Anahtari", StockQuantity = 30, CategoryId = 2, SKU = "GEDORE001", Barcode = "1234567890128", CostPrice = 80.00M,
             Category = new SatışProject.Entities.Category { CategoryId = 2, Name = "Manuel El Aletleri" } },
        new SatışProject.Entities.Product { ProductId = 7, Name = "Lincoln Elektrik Kaynak Makinesi", UnitPrice = 3500.00M, ImageUrl = "https://via.placeholder.com/300x240?text=Kaynak+Makinesi", StockQuantity = 3, CategoryId = 4, SKU = "LINCOLN001", Barcode = "1234567890129", CostPrice = 3000.00M,
             Category = new SatışProject.Entities.Category { CategoryId = 4, Name = "Kaynak Makineleri" } },
        new SatışProject.Entities.Product { ProductId = 8, Name = "İş Güvenlik Ayakkabısı", UnitPrice = 380.00M, ImageUrl = "https://via.placeholder.com/300x240?text=Guvenlik+Ayakkabisi", StockQuantity = 25, CategoryId = 3, SKU = "ISG001", Barcode = "1234567890130", CostPrice = 250.00M,
             Category = new SatışProject.Entities.Category { CategoryId = 3, Name = "İş Güvenlik Ekipmanları" } }
    };

        // Category modeliniz olmadığı için basit bir liste oluşturdum
        // Normalde bu da Entities klasöründe olmalıydı.
        private static List<SatışProject.Entities.Category> _categories = new List<SatışProject.Entities.Category>
    {
        new SatışProject.Entities.Category { CategoryId = 1, Name = "Elektrikli El Aletleri" },
        new SatışProject.Entities.Category { CategoryId = 2, Name = "Manuel El Aletleri" },
        new SatışProject.Entities.Category { CategoryId = 3, Name = "İş Güvenlik Ekipmanları" },
        new SatışProject.Entities.Category { CategoryId = 4, Name = "Kaynak Makineleri" },
        new SatışProject.Entities.Category { CategoryId = 5, Name = "Yapı Malzemeleri" }
    };
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult UserManager()
        {
            return View();
        }
        public IActionResult Index2()
        {
            return View();
        }
        public IActionResult Index3()
        {
            return View();
        }
        public IActionResult Details(int id)
        {
            var product = _products.FirstOrDefault(p => p.ProductId == id);

            // Eğer kategori bilgisi Product nesnesi üzerinde otomatik yüklenmiyorsa (lazy loading veya eager loading ayarlanmadıysa),
            // Category nesnesini manuel olarak atamanız gerekebilir.
            if (product != null && product.Category == null)
            {
                product.Category = _categories.FirstOrDefault(c => c.CategoryId == product.CategoryId);
            }

            if (product == null)
            {
                return NotFound(); // Ürün bulunamazsa 404 sayfası
            }
            return View(product); // Ürünü View'a aktar
        }

    }
}
