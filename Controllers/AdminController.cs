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
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly SatısContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;

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

        #region Employee Management (Çalışan Yönetimi)

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
                model.Add((user, roles)); 
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = _context.Departments.Where(d => d.IsActive).ToList();

            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = roles;

            return View();
        }

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

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            try
            {
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

                transaction.Complete(); 
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Çalışan oluşturulurken bir hata oluştu: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.AppUser)
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeID == id);

            if (employee == null) return NotFound();

            PopulateSelectLists(); 
            return View(employee);
        }

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

            dbEmployee.Address = employee.Address;
            dbEmployee.City = employee.City;
            dbEmployee.Country = employee.Country;
            dbEmployee.Title = employee.Title;
            dbEmployee.DepartmentId = employee.DepartmentId;
            dbEmployee.BirthDate = employee.BirthDate;
            dbEmployee.Notes = employee.Notes;
            dbEmployee.Salary = employee.Salary;

            if (profilePhoto != null && dbEmployee.AppUser != null)
            {
                string? oldPhoto = dbEmployee.AppUser.ProfilePhotoUrl;
                if (!string.IsNullOrEmpty(oldPhoto))
                    DeletePhoto(oldPhoto); 

                string newPath = await SavePhotoAsync(profilePhoto);
                dbEmployee.AppUser.ProfilePhotoUrl = newPath;
                dbEmployee.AppUser.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.AppUser)
                .FirstOrDefaultAsync(e => e.EmployeeID == id);

            if (employee == null) return NotFound();

            employee.IsActive = false; 
            if (employee.AppUser != null)
            {
                employee.AppUser.IsActive = false;
                employee.AppUser.ClosedAt = DateTime.Now;
                employee.AppUser.ProfilePhotoUrl = null; 
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Role Management (Rol Yönetimi)

        [HttpGet]
        public async Task<IActionResult> EditRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            return View(role);
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(string id)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteRole method called with ID: {id}");
            Console.WriteLine($"DeleteRole method called with ID: {id}");

            try
            {
                // 1. ID kontrolü
                if (string.IsNullOrEmpty(id))
                {
                    TempData["Error"] = "Geçersiz rol ID'si.";
                    System.Diagnostics.Debug.WriteLine("ERROR: Empty ID received");
                    return RedirectToAction("Roles");
                }

                System.Diagnostics.Debug.WriteLine($"Processing role deletion for ID: {id}");

                // 2. Rolü bul
                var role = await _roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    TempData["Error"] = "Silinmek istenen rol bulunamadı.";
                    System.Diagnostics.Debug.WriteLine($"ERROR: Role not found for ID: {id}");
                    return RedirectToAction("Roles");
                }

                System.Diagnostics.Debug.WriteLine($"Role found: {role.Name}");

                var protectedRoles = new[] { "Admin", "SuperAdmin", "System" };
                if (protectedRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
                {
                    TempData["Error"] = $"'{role.Name}' rolü sistem rolüdür ve silinemez.";
                    System.Diagnostics.Debug.WriteLine($"ERROR: Protected role cannot be deleted: {role.Name}");
                    return RedirectToAction("Roles");
                }

                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
                if (usersInRole != null && usersInRole.Any())
                {
                    TempData["Error"] = $"'{role.Name}' rolü silinemedi. Bu role sahip {usersInRole.Count} kullanıcı bulunmaktadır.";
                    System.Diagnostics.Debug.WriteLine($"ERROR: Role has {usersInRole.Count} users assigned");
                    return RedirectToAction("Roles");
                }

                System.Diagnostics.Debug.WriteLine($"Attempting to delete role: {role.Name}");
                var result = await _roleManager.DeleteAsync(role);

                if (result.Succeeded)
                {
                    TempData["Success"] = $"'{role.Name}' rolü başarıyla silindi.";
                    System.Diagnostics.Debug.WriteLine($"SUCCESS: Role deleted successfully: {role.Name}");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["Error"] = $"Rol silinirken bir hata oluştu: {errors}";
                    System.Diagnostics.Debug.WriteLine($"ERROR: Role deletion failed: {errors}");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Rol silinirken beklenmeyen bir hata oluştu.";
                System.Diagnostics.Debug.WriteLine($"EXCEPTION: {ex.Message}");
                Console.WriteLine($"EXCEPTION in DeleteRole: {ex}");
            }

            System.Diagnostics.Debug.WriteLine("Redirecting to Roles action");
            return RedirectToAction("Roles");
        }

        // Alternatif: GET version için de test ekle
        [HttpGet]
        public async Task<IActionResult> TestDeleteRole(string id)
        {
            System.Diagnostics.Debug.WriteLine($"TEST: TestDeleteRole called with ID: {id}");

            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Test: Geçersiz rol ID'si.";
                return RedirectToAction("Roles");
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                TempData["Error"] = "Test: Rol bulunamadı.";
                return RedirectToAction("Roles");
            }

            TempData["Success"] = $"Test başarılı! Rol bulundu: {role.Name}";
            return RedirectToAction("Roles");
        }


        #endregion

        #region User Role Management (Kullanıcı-Rol İlişkisi)
        [HttpGet]
        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles.ToListAsync(); 
            return View(roles); 
        }

        [HttpGet]
        public IActionResult CreateRole()
        {
            return View(); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(AppRole role)
        {
            if (!ModelState.IsValid)
                return View(role); 

            var result = await _roleManager.CreateAsync(role); 

            if (result.Succeeded)
                return RedirectToAction("Roles"); 

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(role); 
        }


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

        private void PopulateSelectLists()
        {
            ViewBag.Departments = _context.Departments.Where(d => d.IsActive).ToList();
        }

        private async Task<string> SavePhotoAsync(IFormFile file)
        {
            string folder = Path.Combine(_env.WebRootPath, "Employees");
            Directory.CreateDirectory(folder); 

            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            string path = Path.Combine(folder, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/Employees/{fileName}"; 
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
