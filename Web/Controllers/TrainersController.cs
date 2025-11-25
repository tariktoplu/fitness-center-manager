using Web.Data;
using Web.Models;
using Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

[Authorize(Roles = "Admin")]
public class TrainersController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public TrainersController(AppDbContext context, UserManager<AppUser> userManager, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
    }

    // 1. LİSTELEME
    public async Task<IActionResult> Index()
    {
        var trainers = await _context.Trainers
            .Include(t => t.User)
            .Include(t => t.GymBranch)
            .Include(t => t.Specializations).ThenInclude(ts => ts.Specialization)
            .ToListAsync();
        return View(trainers);
    }

    // 2. EKLEME SAYFASI (GET)
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["GymBranchId"] = new SelectList(_context.GymBranches, "Id", "Name");
        ViewData["Specializations"] = await _context.Specializations.ToListAsync();
        return View();
    }

    // 2. EKLEME İŞLEMİ (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainerViewModel model)
    {
        if (ModelState.IsValid)
        {
            // A. Identity Kullanıcısı
            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Trainer");

                // B. Resim Yükleme
                string? uniqueFileName = null;
                if (model.ProfileImage != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "trainers");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfileImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfileImage.CopyToAsync(fileStream);
                    }
                }

                // C. Trainer Profili
                var trainer = new Trainer
                {
                    UserId = user.Id,
                    GymBranchId = model.GymBranchId,
                    Biography = model.Biography,
                    HourlyRate = model.HourlyRate,
                    ProfileImageUrl = uniqueFileName,
                    HireDate = DateOnly.FromDateTime(DateTime.UtcNow)
                };

                _context.Add(trainer);
                await _context.SaveChangesAsync(); // ID oluşsun diye kaydet

                // --- DÜZELTME: OTOMATİK MÜSAİTLİK SAATLERİ EKLEME ---
                // Hoca oluşturulurken takvimi boş kalmasın
                var defaultSchedule = new List<TrainerAvailability>();
                for (int i = 0; i < 7; i++)
                {
                    // 0: Pazar, 6: Cumartesi -> Hafta sonu kapalı olsun
                    bool isClosed = (i == 0 || i == 6); 
                    defaultSchedule.Add(new TrainerAvailability
                    {
                        TrainerId = trainer.Id,
                        DayOfWeek = i,
                        IsAvailable = !isClosed,
                        StartTime = new TimeOnly(09, 00),
                        EndTime = new TimeOnly(17, 00)
                    });
                }
                _context.TrainerAvailabilities.AddRange(defaultSchedule);
                // ----------------------------------------------------

                // D. Uzmanlık Alanları
                if (model.SelectedSpecializationIds != null)
                {
                    foreach (var specId in model.SelectedSpecializationIds)
                    {
                        _context.TrainerSpecializations.Add(new TrainerSpecialization
                        {
                            TrainerId = trainer.Id,
                            SpecializationId = specId,
                            ExperienceYears = 1
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        ViewData["GymBranchId"] = new SelectList(_context.GymBranches, "Id", "Name", model.GymBranchId);
        ViewData["Specializations"] = await _context.Specializations.ToListAsync();
        return View(model);
    }

    // ... (ManageAvailability, Edit, Delete metodları önceki verdiğim kodla aynı kalacak) ...
    // Kodun çok uzamaması için alt kısımları tekrar etmiyorum, onlar zaten doğruydu. 
    // Sadece Create metodundaki otomatik saat ekleme kısmı kritikti.
    
    // Ancak eksik kalmasın diye Edit ve Delete metodlarını da içeren tam blok aşağıdadır:

    [HttpGet]
    public async Task<IActionResult> ManageAvailability(int id)
    {
        var trainer = await _context.Trainers.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == id);
        if (trainer == null) return NotFound();

        var existingAvailabilities = await _context.TrainerAvailabilities.Where(ta => ta.TrainerId == id).ToListAsync();
        var model = new TrainerAvailabilityViewModel
        {
            TrainerId = trainer.Id,
            TrainerName = trainer.User.FirstName + " " + trainer.User.LastName,
            WeeklySchedule = new List<TrainerAvailabilityItem>()
        };
        string[] gunler = { "Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi" };
        for (int i = 0; i < 7; i++)
        {
            var exist = existingAvailabilities.FirstOrDefault(x => x.DayOfWeek == i);
            model.WeeklySchedule.Add(new TrainerAvailabilityItem
            {
                DayOfWeek = i, DayName = gunler[i],
                IsAvailable = exist?.IsAvailable ?? true,
                StartTime = exist != null ? exist.StartTime.ToTimeSpan() : new TimeSpan(9,0,0),
                EndTime = exist != null ? exist.EndTime.ToTimeSpan() : new TimeSpan(18,0,0)
            });
        }
        model.WeeklySchedule = model.WeeklySchedule.OrderBy(x => x.DayOfWeek == 0 ? 7 : x.DayOfWeek).ToList();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageAvailability(TrainerAvailabilityViewModel model)
    {
        var oldRecords = await _context.TrainerAvailabilities.Where(x => x.TrainerId == model.TrainerId).ToListAsync();
        _context.TrainerAvailabilities.RemoveRange(oldRecords);
        await _context.SaveChangesAsync();
        foreach (var item in model.WeeklySchedule)
        {
            _context.Add(new TrainerAvailability {
                TrainerId = model.TrainerId, DayOfWeek = item.DayOfWeek, IsAvailable = item.IsAvailable,
                StartTime = TimeOnly.FromTimeSpan(item.StartTime), EndTime = TimeOnly.FromTimeSpan(item.EndTime)
            });
        }
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var trainer = await _context.Trainers.Include(t => t.User).Include(t => t.Specializations).FirstOrDefaultAsync(t => t.Id == id);
        if (trainer == null) return NotFound();
        var model = new TrainerEditViewModel
        {
            Id = trainer.Id, FirstName = trainer.User.FirstName, LastName = trainer.User.LastName, Email = trainer.User.Email!,
            GymBranchId = trainer.GymBranchId, Biography = trainer.Biography, HourlyRate = trainer.HourlyRate, ExistingImageUrl = trainer.ProfileImageUrl,
            SelectedSpecializationIds = trainer.Specializations.Select(ts => ts.SpecializationId).ToList()
        };
        ViewData["GymBranchId"] = new SelectList(_context.GymBranches, "Id", "Name", trainer.GymBranchId);
        ViewData["Specializations"] = await _context.Specializations.ToListAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TrainerEditViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (ModelState.IsValid)
        {
            var trainer = await _context.Trainers.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == id);
            if (trainer == null) return NotFound();

            trainer.User.FirstName = model.FirstName; trainer.User.LastName = model.LastName;
            trainer.User.Email = model.Email; trainer.User.UserName = model.Email;
            var identityResult = await _userManager.UpdateAsync(trainer.User);
            if (!identityResult.Succeeded) {
                foreach (var error in identityResult.Errors) ModelState.AddModelError("", error.Description);
                ViewData["GymBranchId"] = new SelectList(_context.GymBranches, "Id", "Name", model.GymBranchId);
                ViewData["Specializations"] = await _context.Specializations.ToListAsync(); return View(model);
            }

            trainer.GymBranchId = model.GymBranchId; trainer.Biography = model.Biography; trainer.HourlyRate = model.HourlyRate;
            if (model.ProfileImage != null) {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "trainers");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfileImage.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create)) await model.ProfileImage.CopyToAsync(fileStream);
                trainer.ProfileImageUrl = uniqueFileName;
            }

            _context.Update(trainer);
            var oldSpecs = await _context.TrainerSpecializations.Where(ts => ts.TrainerId == id).ToListAsync();
            _context.TrainerSpecializations.RemoveRange(oldSpecs);
            if (model.SelectedSpecializationIds != null) {
                foreach (var specId in model.SelectedSpecializationIds) _context.TrainerSpecializations.Add(new TrainerSpecialization { TrainerId = id, SpecializationId = specId, ExperienceYears = 1 });
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewData["GymBranchId"] = new SelectList(_context.GymBranches, "Id", "Name", model.GymBranchId);
        ViewData["Specializations"] = await _context.Specializations.ToListAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var trainer = await _context.Trainers.Include(t => t.User).Include(t => t.GymBranch).FirstOrDefaultAsync(m => m.Id == id);
        if (trainer == null) return NotFound();
        return View(trainer);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var trainer = await _context.Trainers.FindAsync(id);
        if (trainer != null) {
            var user = await _userManager.FindByIdAsync(trainer.UserId.ToString());
            if (user != null) await _userManager.DeleteAsync(user);
            else { _context.Trainers.Remove(trainer); await _context.SaveChangesAsync(); }
        }
        return RedirectToAction(nameof(Index));
    }
}