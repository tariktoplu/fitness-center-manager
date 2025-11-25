using Web.Data;
using Web.Models;
using Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

[Authorize(Roles = "Admin")]
public class GymBranchesController : Controller
{
    private readonly AppDbContext _context;

    public GymBranchesController(AppDbContext context)
    {
        _context = context;
    }

    // 1. LİSTELEME
    public async Task<IActionResult> Index()
    {
        return View(await _context.GymBranches.ToListAsync());
    }

    // 2. EKLEME
    [HttpGet]
    public IActionResult Create() => View();

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

    // 3. DÜZENLEME
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var gymBranch = await _context.GymBranches.FindAsync(id);
        if (gymBranch == null) return NotFound();
        return View(gymBranch);
    }

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
                if (!_context.GymBranches.Any(e => e.Id == gymBranch.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(gymBranch);
    }

    // 4. SİLME
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var gymBranch = await _context.GymBranches.FirstOrDefaultAsync(m => m.Id == id);
        if (gymBranch == null) return NotFound();
        return View(gymBranch);
    }

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

    // --- 5. ŞUBE HİZMET YÖNETİMİ (GELİŞMİŞ CRUD) ---
    
    [HttpGet]
    public async Task<IActionResult> ManageServices(int id)
    {
        var branch = await _context.GymBranches.FindAsync(id);
        if (branch == null) return NotFound();

        // Tüm genel hizmetler
        var allServices = await _context.Services.ToListAsync();
        
        // Bu şubeye zaten eklenmiş hizmetler
        var existingServices = await _context.GymServices
            .Where(gs => gs.GymBranchId == id)
            .ToListAsync();

        var model = new BranchServicesViewModel
        {
            GymBranchId = branch.Id,
            BranchName = branch.Name,
            Services = new List<BranchServiceItem>()
        };

        foreach (var service in allServices)
        {
            // Bu hizmet bu şubede var mı?
            var existing = existingServices.FirstOrDefault(gs => gs.ServiceId == service.Id);

            model.Services.Add(new BranchServiceItem
            {
                ServiceId = service.Id,
                ServiceName = service.Name,
                // Eğer kayıt varsa ve 'IsAvailable' ise seçili gelsin
                IsSelected = existing != null && existing.IsAvailable,
                // Varsa şube fiyatını, yoksa genel fiyatı getir
                Price = existing != null ? existing.Price : service.BasePrice
            });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageServices(BranchServicesViewModel model)
    {
        // 1. Bu şubeye ait veritabanındaki mevcut kayıtları çek
        var existingRecords = await _context.GymServices
            .Where(gs => gs.GymBranchId == model.GymBranchId)
            .ToListAsync();

        foreach (var item in model.Services)
        {
            var existingRecord = existingRecords.FirstOrDefault(r => r.ServiceId == item.ServiceId);

            if (item.IsSelected)
            {
                // A. KUTUCUK SEÇİLİ (EKLE veya GÜNCELLE)
                if (existingRecord != null)
                {
                    // Zaten varsa: Fiyatını güncelle ve Aktif yap
                    existingRecord.Price = item.Price;
                    existingRecord.IsAvailable = true;
                    _context.Update(existingRecord);
                }
                else
                {
                    // Yoksa: Yeni kayıt oluştur
                    var newRecord = new GymService
                    {
                        GymBranchId = model.GymBranchId,
                        ServiceId = item.ServiceId,
                        Price = item.Price,
                        IsAvailable = true
                    };
                    _context.Add(newRecord);
                }
            }
            else
            {
                // B. KUTUCUK SEÇİLİ DEĞİL (SİLME/PASİF MANTIĞI)
                if (existingRecord != null)
                {
                    // Kayıt varsa: Pasife çek (SOFT DELETE)
                    // Veritabanından silersek geçmiş randevular bozulur.
                    existingRecord.IsAvailable = false; 
                    _context.Update(existingRecord);
                }
                // Kayıt yoksa ve seçilmediyse işlem yapma.
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // --- 6. ÇALIŞMA SAATLERİ YÖNETİMİ ---
    [HttpGet]
    public async Task<IActionResult> ManageHours(int id)
    {
        var branch = await _context.GymBranches.FindAsync(id);
        if (branch == null) return NotFound();

        var existingHours = await _context.GymWorkingHours.Where(wh => wh.GymBranchId == id).ToListAsync();
        var model = new BranchHoursViewModel
        {
            GymBranchId = branch.Id, BranchName = branch.Name, Hours = new List<BranchHourItem>()
        };

        string[] gunler = { "Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi" };
        for (int i = 0; i < 7; i++)
        {
            var exist = existingHours.FirstOrDefault(x => x.DayOfWeek == i);
            model.Hours.Add(new BranchHourItem
            {
                DayOfWeek = i, DayName = gunler[i],
                IsClosed = exist?.IsClosed ?? false,
                OpeningTime = exist != null ? exist.OpeningTime.ToTimeSpan() : new TimeSpan(9, 0, 0),
                ClosingTime = exist != null ? exist.ClosingTime.ToTimeSpan() : new TimeSpan(22, 0, 0)
            });
        }
        // Pazar gününü en sona atmak için sıralama
        model.Hours = model.Hours.OrderBy(h => h.DayOfWeek == 0 ? 7 : h.DayOfWeek).ToList();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageHours(BranchHoursViewModel model)
    {
        // Saatlerde geçmiş veri tutmaya gerek olmadığı için silip yeniden ekliyoruz
        var existing = await _context.GymWorkingHours.Where(x => x.GymBranchId == model.GymBranchId).ToListAsync();
        _context.GymWorkingHours.RemoveRange(existing);
        await _context.SaveChangesAsync();

        foreach (var item in model.Hours)
        {
            _context.GymWorkingHours.Add(new GymWorkingHour
            {
                GymBranchId = model.GymBranchId, DayOfWeek = item.DayOfWeek, IsClosed = item.IsClosed,
                OpeningTime = TimeOnly.FromTimeSpan(item.OpeningTime), ClosingTime = TimeOnly.FromTimeSpan(item.ClosingTime)
            });
        }
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}