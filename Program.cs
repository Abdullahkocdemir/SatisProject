using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sat��Project.Context;
using Sat��Project.Entities;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Sat��Project.Hubs;
using Sat��Project.Models;
using Sat��Project.Extensions;


var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllersWithViews();
builder.Services.AddControllersWithViews()
    .AddMvcOptions(options =>
    {
        options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
            _ => "Ge�erli bir say� giriniz." // Custom error message
        );
    });

builder.Services.AddDbContext<Sat�sContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSignalR(); // SignalR servisi eklenmi�, do�ru

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Oturum 30 dakika hareketsizlikten sonra sona erecek
    options.Cookie.HttpOnly = true; // Oturum �erezini istemci taraf� betiklerinden eri�ilemez yap
    options.Cookie.IsEssential = true; // Oturum �erezini uygulaman�n i�levselli�i i�in gerekli yap
});


builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<Sat�sContext>()
.AddDefaultTokenProviders();


builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ReturnUrlParameter = "ReturnUrl";
    options.Cookie.Name = "SatisOtomasyonProject";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(15); // Oturum s�resi
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Oturum middleware'ini UseRouting ve UseEndpoints aras�na ekleyin
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<MessageHub>("/messageHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    string adminRole = "Admin";
    string adminEmail = "kcdmirapo96@gmail.com";
    string adminPassword = "123456aA*";

    if (!await roleManager.RoleExistsAsync(adminRole))
    {
        await roleManager.CreateAsync(new AppRole
        {
            Name = adminRole,
            Description = "Sistem Y�neticisi"
        });
    }

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = "Abdullah",
            LastName = "KO�DEM�R"
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, adminRole);
        }
    }
}

app.Run();