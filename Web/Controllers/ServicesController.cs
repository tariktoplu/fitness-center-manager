using Web.Data;
using Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

[Authorize(Roles = "Admin")] // Sadece Admin
public class ServicesController : Controller
{
    private readonly AppDbContext _context;

    public ServicesController(AppDbContext context)
    {
        _context = context;
    }

    // 1. LİSTELEME
    public async Task<IActionResult> Index()
    {
        // Kategorisiyle beraber getir (Include)
        var services = await _context.Services.Include(s => s.Category).ToListAsync();
        return View(services);
    }

    // 2. EKLEME SAYFASI (GET)
    public IActionResult Create()
    {
        // Kategorileri Dropdown'a doldur
        ViewData["CategoryId"] = new SelectList(_context.ServiceCategories, "Id", "Name");
        return View();
    }

    // 2. EKLEME İŞLEMİ (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Service service)
    {
        // Validasyon kontrolü
        // Kategori navigation property'si null gelebilir, onu yok sayıyoruz
        ModelState.Remove("Category"); 
        ModelState.Remove("GymServices");

        if (ModelState.IsValid)
        {
            service.IsActive = true;
            _context.Add(service);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        ViewData["CategoryId"] = new SelectList(_context.ServiceCategories, "Id", "Name", service.CategoryId);
        return View(service);
    }

    // 3. SİLME SAYFASI (GET)
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var service = await _context.Services
            .Include(s => s.Category)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (service == null) return NotFound();

        return View(service);
    }

    // 3. SİLME İŞLEMİ (POST)
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service != null)
        {
            _context.Services.Remove(service);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}