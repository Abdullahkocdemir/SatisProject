// Gerekli kütüphaneler (Kimlik yönetimi, yetkilendirme, veri erişimi vs.)
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;
using SatışProject.Entities;
using SatışProject.Models;
using SatışProject.ViewModels;
using System.Transactions;

namespace SatışProject.Controllers
{
    // Bu controller sadece "Admin" rolüne sahip kullanıcılar tarafından erişilebilir
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        // Gerekli bağımlılıklar: veritabanı context'i, dosya kaydetme ortamı, kullanıcı ve rol yöneticileri
        private readonly SatısContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;

        // Constructor: Gerekli servisleri enjekte eder
        public AdminController(
            SatısContext context,
            IWebHostEnvironment env,
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Aktif kullanıcıları listeleyen sayfa
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .Where(u => u.IsActive) // Sadece aktif kullanıcılar
                .OrderBy(u => u.CreatedAt) // Oluşturulma tarihine göre sırala
                .ToListAsync();

            return View(users);
        }

        #region Employee Management (Çalışan Yönetimi)

        // Tüm aktif kullanıcı ve rollerini listeleyen sayfa
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            var model = new List<(AppUser user, IList<string> roles)>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Add((user, roles)); // Tuple olarak kullanıcı ve rolleri ekleniyor
            }

            return View(model);
        }

        // Yeni çalışan oluşturma sayfası (GET)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = _context.Departments.Where(d => d.IsActive).ToList(); // Aktif departmanlar

            var roles = await _roleManager.Roles.ToListAsync(); // Roller dropdown için
            ViewBag.Roles = roles;

            return View();
        }

        // Yeni çalışan oluşturma işlemi (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeCreateViewModel model)
        {
            ViewBag.Departments = _context.Departments.Where(d => d.IsActive).ToList();
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = roles;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Aynı e-posta adresi var mı kontrolü
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
                return View(model);
            }

            // Fotoğraf varsa kaydet
            string? photoPath = null;
            if (model.ProfilePhoto != null)
            {
                photoPath = await SavePhotoAsync(model.ProfilePhoto);
            }

            // İşlemleri transaction içinde yapıyoruz (ya hep ya hiç)
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                // Yeni kullanıcı oluşturuluyor
                var user = new AppUser
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    UserName = model.Email,
                    Email = model.Email,
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    ProfilePhotoUrl = photoPath
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error.Description);

                    return View(model);
                }

                // Kullanıcıya bağlı çalışan kaydı oluşturuluyor
                var employee = new Employee
                {
                    AppUserId = user.Id,
                    Address = model.Address,
                    City = model.City,
                    Country = model.Country,
                    Title = model.Title,
                    DepartmentId = model.DepartmentId,
                    BirthDate = model.BirthDate,
                    Notes = model.Notes,
                    Salary = model.Salary,
                    IsActive = true
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // Rol atanıyor
                if (!string.IsNullOrWhiteSpace(model.SelectedRole))
                {
                    var roleExists = await _roleManager.RoleExistsAsync(model.SelectedRole);
                    if (roleExists)
                    {
                        await _userManager.AddToRoleAsync(user, model.SelectedRole);
                    }
                    else
                    {
                        ModelState.AddModelError("SelectedRole", "Seçilen rol sistemde bulunamadı.");
                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError("SelectedRole", "Lütfen bir rol seçiniz.");
                    return View(model);
                }

                transaction.Complete(); // Tüm işlemler başarılıysa commit
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Çalışan oluşturulurken bir hata oluştu: " + ex.Message);
                return View(model);
            }
        }

        // Çalışan düzenleme sayfası (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.AppUser)
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeID == id);

            if (employee == null) return NotFound();

            PopulateSelectLists(); // Dropdown listeleri hazırla
            return View(employee);
        }

        // Çalışan düzenleme işlemi (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Employee employee, IFormFile? profilePhoto)
        {
            if (!ModelState.IsValid)
            {
                PopulateSelectLists();
                return View(employee);
            }

            var dbEmployee = await _context.Employees
                .Include(e => e.AppUser)
                .FirstOrDefaultAsync(e => e.EmployeeID == employee.EmployeeID);

            if (dbEmployee == null) return NotFound();

            // Alanlar güncelleniyor
            dbEmployee.Address = employee.Address;
            dbEmployee.City = employee.City;
            dbEmployee.Country = employee.Country;
            dbEmployee.Title = employee.Title;
            dbEmployee.DepartmentId = employee.DepartmentId;
            dbEmployee.BirthDate = employee.BirthDate;
            dbEmployee.Notes = employee.Notes;
            dbEmployee.Salary = employee.Salary;

            // Profil fotoğrafı güncelleniyor
            if (profilePhoto != null && dbEmployee.AppUser != null)
            {
                string? oldPhoto = dbEmployee.AppUser.ProfilePhotoUrl;
                if (!string.IsNullOrEmpty(oldPhoto))
                    DeletePhoto(oldPhoto); // Eski fotoğraf siliniyor

                string newPath = await SavePhotoAsync(profilePhoto);
                dbEmployee.AppUser.ProfilePhotoUrl = newPath;
                dbEmployee.AppUser.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Çalışan detay sayfası
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.AppUser)
                .FirstOrDefaultAsync(e => e.EmployeeID == id);

            if (employee == null) return NotFound();

            // Kullanıcının rolleri
            var roles = await _userManager.GetRolesAsync(employee.AppUser);
            ViewBag.Roles = roles;

            return View(employee);
        }

        // Çalışan silme (Soft Delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.AppUser)
                .FirstOrDefaultAsync(e => e.EmployeeID == id);

            if (employee == null) return NotFound();

            employee.IsActive = false; // Çalışan devre dışı

            if (employee.AppUser != null)
            {
                employee.AppUser.IsActive = false;
                employee.AppUser.ClosedAt = DateTime.Now;
                employee.AppUser.ProfilePhotoUrl = null; // Fotoğraf referansı siliniyor
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Role Management (Rol Yönetimi)

        // Rol düzenleme sayfası
        [HttpGet]
        public async Task<IActionResult> EditRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            return View(role);
        }

        // Rol düzenleme işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(AppRole role)
        {
            if (!ModelState.IsValid) return View(role);

            var dbRole = await _roleManager.FindByIdAsync(role.Id);
            if (dbRole == null) return NotFound();

            dbRole.Name = role.Name;
            dbRole.Description = role.Description;

            var result = await _roleManager.UpdateAsync(dbRole);
            if (result.Succeeded)
                return RedirectToAction("Roles");

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(role);
        }

        // Rol silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Rol silinemedi. Bu role sahip kullanıcılar olabilir.";
            }

            return RedirectToAction("Roles");
        }

        #endregion

        #region User Role Management (Kullanıcı-Rol İlişkisi)
        // Tüm rolleri listeleme sayfası
        [HttpGet]
        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles.ToListAsync(); // Veritabanındaki tüm rolleri çek
            return View(roles); // Rolleri Roles.cshtml sayfasına gönder
        }

        // Yeni rol oluşturma sayfasını gösterir
        [HttpGet]
        public IActionResult CreateRole()
        {
            return View(); // Boş bir form döndürür
        }

        // Yeni rol oluşturma işlemini yapar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(AppRole role)
        {
            if (!ModelState.IsValid)
                return View(role); // Model doğrulanmazsa aynı formu geri döndür

            var result = await _roleManager.CreateAsync(role); // Yeni rol oluşturur

            if (result.Succeeded)
                return RedirectToAction("Roles"); // Başarılıysa roller listesine yönlendir

            // Hata varsa hata mesajlarını modele ekle
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(role); // Hatalarla birlikte formu yeniden göster
        }


        // Belirli bir kullanıcının rollerini görüntüleme
        [HttpGet]
        public async Task<IActionResult> UserRoles(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Geçersiz kullanıcı ID.";
                return RedirectToAction("Users");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Users");
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            var model = new UserRoleViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                UserRoles = userRoles.ToList(),
                AllRoles = allRoles
            };

            return View(model);
        }

        // Kullanıcıya rol ekleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(roleName))
            {
                TempData["Error"] = "Geçersiz işlem.";
                return RedirectToAction("UserRoles", new { userId });
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (result.Succeeded)
                TempData["Success"] = "Rol başarıyla eklendi.";
            else
                TempData["Error"] = "Rol eklenirken bir hata oluştu.";

            return RedirectToAction("UserRoles", new { userId });
        }

        // Kullanıcıdan rol kaldırma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("UserRoles", new { userId });
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                TempData["Success"] = $"'{user.UserName}' kullanıcısından '{roleName}' rolü başarıyla kaldırıldı.";
            }
            else
            {
                TempData["Error"] = "Rol kaldırma başarısız: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("UserRoles", new { userId });
        }

        #endregion

        #region Helper Methods (Yardımcı Metotlar)

        // Dropdownlar için departman listesi
        private void PopulateSelectLists()
        {
            ViewBag.Departments = _context.Departments.Where(d => d.IsActive).ToList();
        }

        // Profil fotoğrafını sunucuya kaydet
        private async Task<string> SavePhotoAsync(IFormFile file)
        {
            string folder = Path.Combine(_env.WebRootPath, "Employees");
            Directory.CreateDirectory(folder); // Klasör yoksa oluştur

            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            string path = Path.Combine(folder, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/Employees/{fileName}"; // Web için kullanılacak path
        }

        // Fotoğrafı sil (sunucudan)
        private void DeletePhoto(string relativePath)
        {
            string fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        #endregion
    }
}
