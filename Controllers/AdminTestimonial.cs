using Microsoft.AspNetCore.Mvc;
using SatışProject.Context;
using SatışProject.Entities;

namespace SatışProject.Controllers
{
    public class AdminTestimonial : Controller
    {
        private readonly SatısContext _context;

        public AdminTestimonial(SatısContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var values = _context.Testimonials.ToList();
            return View(values);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(Testimonial testimonial)
        {
            if (ModelState.IsValid)
            {
                var value = _context.Testimonials.Add(testimonial);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(testimonial);
        }
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var update = _context.Testimonials.Find(id);
            if (update == null)
            {
                TempData["Referans Bulunamadı"] = "Referans Bulunamadı";
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Testimonial updateTestimonial)
        {
            if (ModelState.IsValid)
            {
                var update = _context.Testimonials.Update(updateTestimonial);
                await _context.SaveChangesAsync();

            }
            return View(updateTestimonial);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var delete = await _context.Testimonials.FindAsync(id);
            _context.Testimonials.Remove(delete!);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
