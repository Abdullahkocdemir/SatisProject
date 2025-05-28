using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;
using SatışProject.Entities;

namespace SatışProject.Controllers
{
    public class AdminDepartmentController : Controller
    {
        private readonly SatısContext _context;

        public AdminDepartmentController(SatısContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var values = _context.Departments
                .Where(d => d.IsActive)
                .ToList();
            return View(values);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department department)
        {
            if (ModelState.IsValid)
            {
                department.IsActive = true; 
                _context.Add(department);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.DepartmentID == id);

            if (department == null)
                return NotFound();

            return View(department);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound();

            return View(department);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Department department)
        {
            if (!ModelState.IsValid)
                return View(department);

            _context.Update(department);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Silme işlemi artık fiziksel silme yapmıyor, pasif hale getiriyor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound();

            department.IsActive = false; // Pasif hale getir
            _context.Departments.Update(department);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
