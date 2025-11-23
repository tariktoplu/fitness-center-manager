using Web.Data;
using Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

// Sadece Admin rolü olanlar bu sayfaları görebilir
[Authorize(Roles = "Admin")]
public class GymBranchesController : Controller
{
    private readonly AppDbContext _context;

    public GymBranchesController(AppDbContext context)
    {
        _context = context;
    }

    // 1. LİSTELEME (READ)
    public async Task<IActionResult> Index()
    {
        var branches = await _context.GymBranches.ToListAsync();
        return View(branches);
    }

    // 2. EKLEME SAYFASI (CREATE - GET)
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // 2. EKLEME İŞLEMİ (CREATE - POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GymBranch gymBranch)
    {
        if (ModelState.IsValid)
        {
            gymBranch.CreatedAt = DateTime.UtcNow;
            _context.Add(gymBranch);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(gymBranch);
    }

    // 3. GÜNCELLEME SAYFASI (EDIT - GET)
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var gymBranch = await _context.GymBranches.FindAsync(id);
        if (gymBranch == null) return NotFound();

        return View(gymBranch);
    }

    // 3. GÜNCELLEME İŞLEMİ (EDIT - POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, GymBranch gymBranch)
    {
        if (id != gymBranch.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(gymBranch);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.GymBranches.Any(e => e.Id == gymBranch.Id))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(gymBranch);
    }

    // 4. SİLME SAYFASI (DELETE - GET)
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var gymBranch = await _context.GymBranches
            .FirstOrDefaultAsync(m => m.Id == id);
        
        if (gymBranch == null) return NotFound();

        return View(gymBranch);
    }

    // 4. SİLME İŞLEMİ (DELETE - POST)
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var gymBranch = await _context.GymBranches.FindAsync(id);
        if (gymBranch != null)
        {
            _context.GymBranches.Remove(gymBranch);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}