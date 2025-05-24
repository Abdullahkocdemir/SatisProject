// File: SatışProject.Controllers/SettingsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Entities;
using SatışProject.Models;
using System.IO;
using System.Threading.Tasks;

namespace SatışProject.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment; // For file uploads

        public SettingsController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // If user is not found, sign out and redirect to login
                await _signInManager.SignOutAsync();
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Account"); // Assuming an Account controller for login
            }

            // Load related Employee data
            user = await _userManager.Users
                         .Include(u => u.Employee)
                            .ThenInclude(e => e.Department) // Include Department for Employee
                         .FirstOrDefaultAsync(u => u.Id == user.Id);

            if (user == null)
            {
                // Should not happen if GetUserAsync succeeded, but as a safeguard
                await _signInManager.SignOutAsync();
                TempData["ErrorMessage"] = "Kullanıcı detayları yüklenirken bir hata oluştu. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Login", "Account");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new UserProfileViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? "N/A",
                Email = user.Email ?? "N/A",
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                Roles = roles
            };

            if (user.Employee != null)
            {
                viewModel.EmployeeDetails = new EmployeeDetailsViewModel
                {
                    Address = user.Employee.Address,
                    City = user.Employee.City,
                    Country = user.Employee.Country,
                    Title = user.Employee.Title,
                    DepartmentName = user.Employee.Department?.Name ?? "N/A",
                    BirthDate = user.Employee.BirthDate,
                    Salary = user.Employee.Salary
                };
            }

            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }
            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            }

            return View(viewModel);
        }
        


        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index");
            }

            var viewModel = new EditProfileViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                CurrentProfilePhotoUrl = user.ProfilePhotoUrl
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index");
            }

            // Update user properties
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.UpdatedAt = DateTime.Now; // Update the UpdatedAt field

            // Check if email has changed and update it
            if (user.Email != model.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
                // If email changed, consider re-signing in the user to update claims
                await _signInManager.RefreshSignInAsync(user);
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Re-sign in the user to update their claims (e.g., FullName)
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (changePasswordResult.Succeeded)
            {
                // Re-sign in the user after password change for security
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Şifreniz başarıyla değiştirildi.";
                return RedirectToAction("Index");
            }

            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> UpdateProfilePhoto()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index");
            }

            var viewModel = new UpdateProfilePhotoViewModel
            {
                CurrentProfilePhotoUrl = user.ProfilePhotoUrl
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfilePhoto(UpdateProfilePhotoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index");
            }

            if (model.ProfilePhotoFile != null && model.ProfilePhotoFile.Length > 0)
            {
                // Define the path to save the image
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profile_photos");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate a unique file name
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfilePhotoFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file to the server
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePhotoFile.CopyToAsync(fileStream);
                }

                // Delete old profile photo if it exists and is not the default
                if (!string.IsNullOrEmpty(user.ProfilePhotoUrl) && user.ProfilePhotoUrl != "/images/default_profile.png")
                {
                    string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfilePhotoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Update the user's ProfilePhotoUrl
                user.ProfilePhotoUrl = "/images/profile_photos/" + uniqueFileName;
                user.UpdatedAt = DateTime.Now;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Profil fotoğrafınız başarıyla güncellendi.";
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Lütfen bir fotoğraf seçin.");
            }
            model.CurrentProfilePhotoUrl = user.ProfilePhotoUrl;
            return View(model);
        }
    }
}
