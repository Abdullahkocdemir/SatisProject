using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SatışProject.Context;
using SatışProject.Entities;
using System.Linq; 

namespace SatışProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminWhyUseController : Controller
    {
        private readonly SatısContext _context;

        public AdminWhyUseController(SatısContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var values = _context.Whies.ToList();
            return View(values);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(WhyUs whyUs)
        {
            if (ModelState.IsValid)
            {
                _context.Whies.Add(whyUs);
                _context.SaveChanges();
                TempData["Success"] = "Yeni 'Neden Biz' öğesi başarıyla eklendi!";
                return RedirectToAction("Index");
            }
            return View(whyUs);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var value = _context.Whies.Find(id);
            if (value == null)
            {
                return NotFound();
            }
            return View(value);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(WhyUs whyUs)
        {
            if (ModelState.IsValid)
            {
                _context.Update(whyUs);
                _context.SaveChanges();
                TempData["Success"] = "'Neden Biz' öğesi başarıyla güncellendi!";
                return RedirectToAction("Index");
            }
            return View(whyUs);
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var whyUs = _context.Whies.Find(id);
            if (whyUs == null)
            {
                return NotFound(); 
            }
            return View(whyUs);
        }

        [HttpPost, ActionName("Delete")] 
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id) 
        {
            var whyUs = _context.Whies.Find(id);
            if (whyUs == null)
            {
                return NotFound(); 
            }

            _context.Whies.Remove(whyUs);
            _context.SaveChanges();
            TempData["Success"] = "'Neden Biz' öğesi başarıyla silindi!"; 
            return RedirectToAction("Index");
        }
    }
}