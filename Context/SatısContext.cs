// SatışProject.Context/SatısContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SatışProject.Entities;
using Microsoft.AspNetCore.Identity; // IdentityUserRole için ekle

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
        public DbSet<Brand> Brands { get; set; } = null!; // null! ekleyerek null uyarısını kaldırırız
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<InvoiceItem> InvoiceItems { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Sale> Sales { get; set; } = null!;
        public DbSet<SaleItem> SaleItems { get; set; } = null!;
        public DbSet<UserLoginHistory> UserLoginHistories { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<MessageRecipient> MessageRecipients { get; set; } = null!;
        public DbSet<WhyUs> Whies { get; set; } = null!;
        public DbSet<Testimonial> Testimonials { get; set; } = null!;
        public DbSet<Contact> Contacts { get; set; } = null!;
        public DbSet<Basket> Baskets { get; set; } = null!;
        public DbSet<BasketItem> BasketItems { get; set; } = null!;
        public DbSet<ToDoItem> ToDoItems { get; set; } = null!;


        // Model konfigürasyonları
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // IdentityDbContext'in kendi model oluşturma mantığını çalıştırır
            // Bu, Identity tablolarının (Users, Roles, UserRoles vb.) doğru bir şekilde yapılandırılmasını sağlar.
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
            modelBuilder.Entity<ToDoItem>()
                         .HasOne(t => t.Employee)
                         .WithMany(e => e.ToDoItems)
                         .HasForeignKey(t => t.EmployeeId)
                          .OnDelete(DeleteBehavior.Cascade); // Çalışan silinince görevleri de silinsin


            modelBuilder.Entity<BasketItem>()
    .HasOne(bi => bi.Product)
    .WithMany() // Or WithMany(p => p.BasketItems) if Product has a BasketItems collection
    .HasForeignKey(bi => bi.ProductId);

            modelBuilder.Entity<BasketItem>()
                .HasOne(bi => bi.Basket)
                .WithMany(b => b.BasketItems)
                .HasForeignKey(bi => bi.BasketId);



            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Satış -> Müşteri & Çalışan ilişkileri (SatışItem ilişkisi SaleItem sınıfında tanımlanmalı)
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
                .IsRequired(false) // Satışa bağlı olmayan faturalar olabilir
                .OnDelete(DeleteBehavior.Restrict);

            // Fatura Detayı -> Fatura ilişkisi
            modelBuilder.Entity<InvoiceItem>()
                .HasOne(i => i.Invoice)
                .WithMany(inv => inv.Items)
                .HasForeignKey(i => i.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade); // Fatura silindiğinde detayları da silinsin

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

            // Mesajlaşma ilişkileri
            modelBuilder.Entity<MessageRecipient>()
                .HasOne(mr => mr.Message)
                .WithMany(m => m.Recipients)
                .HasForeignKey(mr => mr.MessageId)
                .OnDelete(DeleteBehavior.Cascade); // Mesaj silindiğinde alıcı kayıtları da silinsin (daha uygun olabilir)

            modelBuilder.Entity<MessageRecipient>()
                .HasOne(mr => mr.Recipient)
                .WithMany() // AppUser tarafında MessageRecipient koleksiyonu yoksa Many()
                .HasForeignKey(mr => mr.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict); // Alıcı kullanıcı silindiğinde mesaj alıcı kayıtları silinmesin

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany() // AppUser tarafında gönderilen mesajların koleksiyonu yoksa Many()
                .HasForeignKey(m => m.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict); // Gönderen kullanıcı silindiğinde mesajlar silinmesin
        }
    }
}