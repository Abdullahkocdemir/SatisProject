using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SatışProject.Entities;

namespace SatışProject.Context
{
    // Identity ile genişletilmiş DbContext sınıfı
    public class SatısContext : IdentityDbContext<AppUser, AppRole, string>
    {
        // Constructor: DI ile gelen options parametresi DbContextOptions kullanılarak context konfigürasyonu yapılır
        public SatısContext(DbContextOptions<SatısContext> options) : base(options)
        {
        }

        // Tabloların DbSet olarak tanımlanması
        public DbSet<Brand> Brands { get; set; }                // Marka tablosu
        public DbSet<Category> Categories { get; set; }         // Kategori tablosu
        public DbSet<Customer> Customers { get; set; }          // Müşteri tablosu
        public DbSet<Department> Departments { get; set; }      // Departman tablosu
        public DbSet<Employee> Employees { get; set; }          // Çalışan tablosu
        public DbSet<Invoice> Invoices { get; set; }            // Fatura tablosu
        public DbSet<InvoiceItem> InvoiceItems { get; set; }    // Fatura detay tablosu
        public DbSet<Product> Products { get; set; }            // Ürün tablosu
        public DbSet<Sale> Sales { get; set; }                  // Satış tablosu
        public DbSet<UserLoginHistory> UserLoginHistories { get; set; } // Kullanıcı giriş geçmişi

        // Model konfigürasyonları
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Employee -> Department ilişkisi
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee <-> AppUser birebir ilişkisi
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.AppUser)
                .WithOne(u => u.Employee)
                .HasForeignKey<Employee>(e => e.AppUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Silinince bağlı kullanıcı da silinsin

            // Ürün -> Marka & Kategori ilişkileri
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Satış -> Müşteri & Çalışan & Ürün ilişkileri
            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Customer)
                .WithMany(c => c.Sales)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Employee)
                .WithMany(e => e.Sales)
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Product)
                .WithMany(p => p.Sales)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Fatura -> Müşteri ilişkisi
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Fatura -> Satış ilişkisi (opsiyonel)
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Sale)
                .WithMany(s => s.Invoices)
                .HasForeignKey(i => i.SaleId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Fatura Detayı -> Fatura ilişkisi
            modelBuilder.Entity<InvoiceItem>()
                .HasOne(i => i.Invoice)
                .WithMany(inv => inv.Items)
                .HasForeignKey(i => i.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Fatura Detayı -> Ürün ilişkisi
            modelBuilder.Entity<InvoiceItem>()
                .HasOne(i => i.Product)
                .WithMany(p => p.InvoiceItems)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Kullanıcı giriş geçmişi ilişkisi
            modelBuilder.Entity<UserLoginHistory>()
                .HasOne(ulh => ulh.User)
                .WithMany(u => u.LoginHistories)
                .HasForeignKey(ulh => ulh.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
/**
 * public async Task<IActionResult> DetailsBySku(string sku)
{
    if (string.IsNullOrEmpty(sku))
    {
        return NotFound();
    }

    var product = await _context.Products
        .Include(p => p.Category)
        .Include(p => p.Brand)
        .FirstOrDefaultAsync(p => p.SKU == sku);

    if (product == null)
    {
        return NotFound();
    }

    return View(product);
}

 */