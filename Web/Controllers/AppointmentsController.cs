using Web.Data;
using Web.Models;
using Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

[Authorize]
public class AppointmentsController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public AppointmentsController(AppDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // 1. LİSTELEME
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");
        
        var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == user.Id);
        if (member == null) return View(new List<Appointment>());

        var appointments = await _context.Appointments
            .Include(a => a.Trainer).ThenInclude(t => t.User)
            .Include(a => a.GymService).ThenInclude(gs => gs.Service)
            .Where(a => a.MemberId == member.Id)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        return View(appointments);
    }

    // 2. BEKLEYENLER (ADMIN)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Pending()
    {
        var appointments = await _context.Appointments
            .Include(a => a.Trainer).ThenInclude(t => t.User)
            .Include(a => a.GymService).ThenInclude(gs => gs.Service)
            .Include(a => a.Member).ThenInclude(m => m.User)
            .Where(a => a.Status == "Pending")
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync();
        return View(appointments);
    }

    // 3. ONAYLA (ADMIN)
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Approve(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment != null)
        {
            appointment.Status = "Confirmed";
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Pending));
    }

    // 4. İPTAL
    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment != null)
        {
            appointment.Status = "Cancelled";
            await _context.SaveChangesAsync();
        }
        if (User.IsInRole("Admin")) return RedirectToAction(nameof(Pending));
        return RedirectToAction(nameof(Index));
    }

    // 5. RANDEVU AL (GET)
    [HttpGet]
    public IActionResult Create()
    {
        var trainers = _context.Trainers.Include(t => t.User)
            .Select(t => new { Id = t.Id, FullName = t.User.FirstName + " " + t.User.LastName }).ToList();

        ViewData["TrainerId"] = new SelectList(trainers, "Id", "FullName");
        ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name");
        return View(new BookAppointmentViewModel());
    }

    // 6. RANDEVU KAYDET (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookAppointmentViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Kontrol 0: Geçmiş Tarih
            if (model.AppointmentDate < DateOnly.FromDateTime(DateTime.Now))
            {
                ModelState.AddModelError("AppointmentDate", "Geçmiş bir tarihe randevu alamazsınız.");
                ReloadDropdowns(model);
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == user!.Id);

            if (member == null)
            {
                member = new Member { UserId = user!.Id, JoinDate = DateTime.UtcNow, IsActive = true };
                _context.Add(member);
                await _context.SaveChangesAsync();
            }

            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == model.ServiceId);
            if (service == null) return NotFound("Hizmet bulunamadı");

            var endTime = model.StartTime.AddMinutes(service.DurationMinutes);

            // --- NOT: UZMANLIK KONTROLÜ KALDIRILDI ---
            // Artık her hoca, her hizmet için seçilebilir.

            // Kontrol 1: Müsaitlik (Gün/Saat)
            int dayOfWeek = (int)model.AppointmentDate.DayOfWeek;
            var trainerSchedule = await _context.TrainerAvailabilities
                .FirstOrDefaultAsync(t => t.TrainerId == model.TrainerId && t.DayOfWeek == dayOfWeek);

            if (trainerSchedule == null)
            {
                ModelState.AddModelError("", "Bu eğitmenin çalışma takvimi tanımlanmamış.");
                ReloadDropdowns(model);
                return View(model);
            }

            if (!trainerSchedule.IsAvailable)
            {
                ModelState.AddModelError("", "Seçtiğiniz eğitmen bu günde çalışmamaktadır.");
                ReloadDropdowns(model);
                return View(model);
            }

            if (model.StartTime < trainerSchedule.StartTime || endTime > trainerSchedule.EndTime)
            {
                ModelState.AddModelError("", $"Eğitmen sadece {trainerSchedule.StartTime:HH:mm} - {trainerSchedule.EndTime:HH:mm} saatleri arasında hizmet vermektedir.");
                ReloadDropdowns(model);
                return View(model);
            }

            // Kontrol 2: Çakışma (Conflict)
            bool isConflict = await _context.Appointments.AnyAsync(a => 
                a.TrainerId == model.TrainerId &&
                a.AppointmentDate == model.AppointmentDate &&
                (
                    (model.StartTime >= a.StartTime && model.StartTime < a.EndTime) ||
                    (endTime > a.StartTime && endTime <= a.EndTime) ||
                    (model.StartTime <= a.StartTime && endTime >= a.EndTime)
                )
                && a.Status != "Cancelled"
            );

            if (isConflict)
            {
                ModelState.AddModelError("", "Seçtiğiniz saatte eğitmenimiz maalesef dolu.");
                ReloadDropdowns(model);
                return View(model);
            }

            // Kayıt İşlemi
            var trainer = await _context.Trainers.FindAsync(model.TrainerId);
            
            // Şubenin bu hizmet için belirlediği fiyatı bul, yoksa genel fiyatı al
            var gymService = await _context.GymServices
                .FirstOrDefaultAsync(gs => gs.GymBranchId == trainer!.GymBranchId && gs.ServiceId == model.ServiceId);

            if (gymService == null)
            {
                // Şubeye eklenmemişse bile (isteğin üzerine) randevu alınabilsin, genel fiyatla.
                // Ancak FK hatası almamak için geçici olarak oluşturuyoruz.
                gymService = new GymService 
                { 
                    GymBranchId = trainer!.GymBranchId, 
                    ServiceId = model.ServiceId, 
                    Price = service.BasePrice,
                    IsAvailable = true
                };
                _context.Add(gymService);
                await _context.SaveChangesAsync();
            }

            var appointment = new Appointment
            {
                MemberId = member.Id,
                TrainerId = model.TrainerId,
                GymServiceId = gymService.Id,
                AppointmentDate = model.AppointmentDate,
                StartTime = model.StartTime,
                EndTime = endTime,
                TotalPrice = gymService.Price,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(appointment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        ReloadDropdowns(model);
        return View(model);
    }

    private void ReloadDropdowns(BookAppointmentViewModel model)
    {
        var trainers = _context.Trainers.Include(t => t.User)
            .Select(t => new { Id = t.Id, FullName = t.User.FirstName + " " + t.User.LastName }).ToList();
        ViewData["TrainerId"] = new SelectList(trainers, "Id", "FullName", model.TrainerId);
        ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name", model.ServiceId);
    }
    
    // --- AJAX İÇİN: MÜSAİT SAATLERİ GETİR ---
        [HttpGet]
        public async Task<JsonResult> GetAvailableHours(int trainerId, string date)
        {
            if (!DateOnly.TryParse(date, out DateOnly selectedDate))
            {
                return Json(new { error = "Geçersiz tarih." });
            }
    
            // 1. Hocanın o günkü çalışma saatlerini bul
            int dayOfWeek = (int)selectedDate.DayOfWeek;
            var schedule = await _context.TrainerAvailabilities
                .FirstOrDefaultAsync(t => t.TrainerId == trainerId && t.DayOfWeek == dayOfWeek);
    
            if (schedule == null || !schedule.IsAvailable)
            {
                return Json(new List<string>()); // O gün çalışmıyor, boş liste dön
            }
    
            // 2. O günkü dolu randevuları çek
            var existingAppointments = await _context.Appointments
                .Where(a => a.TrainerId == trainerId && 
                            a.AppointmentDate == selectedDate && 
                            a.Status != "Cancelled")
                .ToListAsync();
    
            // 3. Saat aralıklarını oluştur (Örn: 09:00, 10:00... diye saat başı)
            var availableSlots = new List<string>();
            
            // Başlangıç ve Bitiş saatlerini al
            TimeSpan current = schedule.StartTime.ToTimeSpan();
            TimeSpan end = schedule.EndTime.ToTimeSpan();
    
            // Hizmet süresi varsayılan 60 dk kabul edelim (Listeleme için)
            // İstenirse serviceId de parametre alınıp dinamik yapılabilir.
            TimeSpan interval = TimeSpan.FromMinutes(60); 
    
            while (current.Add(interval) <= end)
            {
                // Bu saat aralığında çakışma var mı?
                TimeOnly slotStart = TimeOnly.FromTimeSpan(current);
                TimeOnly slotEnd = TimeOnly.FromTimeSpan(current.Add(interval));
    
                bool isTaken = existingAppointments.Any(a => 
                    (slotStart >= a.StartTime && slotStart < a.EndTime) || // Başlangıç çakışması
                    (slotEnd > a.StartTime && slotEnd <= a.EndTime) ||     // Bitiş çakışması
                    (slotStart <= a.StartTime && slotEnd >= a.EndTime)     // Kapsama
                );
    
                if (!isTaken)
                {
                    availableSlots.Add(slotStart.ToString("HH:mm"));
                }
    
                current = current.Add(interval); // Bir sonraki saate geç
            }
    
            return Json(availableSlots);
        }
}