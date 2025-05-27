using Microsoft.AspNetCore.Mvc;
using SatışProject.Context;
using SatışProject.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SatışProject.Controllers
{
    //[Authorize]   // Hazırsa aç
    [AutoValidateAntiforgeryToken]          // Tüm POST’lar için otomatik CSRF koruması
    public class ToDoListController : Controller
    {
        private readonly SatısContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ToDoListController(SatısContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /*-------------------------------------------------
         *  LISTE
         *------------------------------------------------*/
        public async Task<IActionResult> Index()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return RedirectToAction("Login", "Account");

            var employee = await _context.Employees
                                         .FirstOrDefaultAsync(e => e.AppUserId == userId);
            if (employee is null) return NotFound("Çalışan kaydı bulunamadı.");

            var items = await _context.ToDoItems
                                      .Where(t => t.EmployeeId == employee.EmployeeID)
                                      .OrderByDescending(t => t.CreatedDate)
                                      .ToListAsync();

            ViewBag.EmployeeId = employee.EmployeeID;
            return View(items);
        }

        /*-------------------------------------------------
         *  CREATE
         *------------------------------------------------*/
        [HttpGet]
        public async Task<IActionResult> Create(int employeeId)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.AppUserId == userId);

            if (employee is null || employee.EmployeeID != employeeId)
                return Unauthorized("Bu işlem için yetkiniz yok.");

            return View(new ToDoItem { EmployeeId = employeeId });
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Description,DueDate,EmployeeId")] ToDoItem item)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.AppUserId == userId);

            if (employee is null || employee.EmployeeID != item.EmployeeId)
            {
                ModelState.AddModelError(string.Empty, "Bu işlem için yetkiniz yok.");
                return View(item);
            }

            item.CreatedDate = DateTime.Now;
            _context.Add(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /*-------------------------------------------------
         *  EDIT
         *------------------------------------------------*/
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.ToDoItems.FindAsync(id);
            if (item is null) return NotFound();

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.AppUserId == userId);

            if (employee is null || item.EmployeeId != employee.EmployeeID)
                return Unauthorized("Bu öğeyi düzenlemek için yetkiniz yok.");

            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("ToDoItemID,Description,IsCompleted,CreatedDate,DueDate,EmployeeId")] ToDoItem item)
        {
            if (id != item.ToDoItemID) return NotFound();

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.AppUserId == userId);

            if (employee is null || employee.EmployeeID != item.EmployeeId)
            {
                ModelState.AddModelError(string.Empty, "Bu öğeyi düzenlemek için yetkiniz yok.");
                return View(item);
            }

            try
            {
                var original = await _context.ToDoItems.AsNoTracking().FirstOrDefaultAsync(t => t.ToDoItemID == id);
                if (original is null) return NotFound();

                item.CreatedDate = original.CreatedDate;   // Oluşturulma tarihini koru
                _context.Update(item);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ToDoItemExists(item.ToDoItemID)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        /*-------------------------------------------------
         *  DELETE  (AJAX)
         *------------------------------------------------*/
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.ToDoItems.FindAsync(id);
            if (item is null)
                return Json(new { success = false, message = "Görev bulunamadı." });

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.AppUserId == userId);

            if (employee is null || item.EmployeeId != employee.EmployeeID)
                return Json(new { success = false, message = "Bu öğeyi silmek için yetkiniz yok." });

            _context.ToDoItems.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Görev başarıyla silindi." });
        }

        /*-------------------------------------------------
         *  TOGGLE COMPLETE (AJAX)
         *------------------------------------------------*/
        [HttpPost]
        public async Task<IActionResult> ToggleComplete(int id)
        {
            var item = await _context.ToDoItems.FindAsync(id);
            if (item is null) return NotFound();

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.AppUserId == userId);

            if (employee is null || item.EmployeeId != employee.EmployeeID)
                return Unauthorized();

            item.IsCompleted = !item.IsCompleted;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isCompleted = item.IsCompleted });
        }

        private bool ToDoItemExists(int id) =>
            _context.ToDoItems.Any(e => e.ToDoItemID == id);
    }
}
