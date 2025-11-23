using Web.Data;
using Web.Models;
using Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

[Authorize] // Sadece giriş yapmış üyeler
public class AppointmentsController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public AppointmentsController(AppDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // 1. RANDEVULARIM (Üyenin kendi randevuları)
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        
        // Üyenin Member Id'sini bul
        var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == user!.Id);
        if (member == null) return RedirectToAction("Create"); // Profil yoksa randevu sayfasına at

        var appointments = await _context.Appointments
            .Include(a => a.Trainer).ThenInclude(t => t.User) // Hoca adını al
            .Include(a => a.GymService).ThenInclude(gs => gs.Service) // Hizmet adını al
            .Where(a => a.MemberId == member.Id)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        return View(appointments);
    }

    // 2. RANDEVU AL (Sayfa Açılışı)
    [HttpGet]
    public IActionResult Create()
    {
        ViewData["TrainerId"] = new SelectList(_context.Trainers.Include(t => t.User), "Id", "User.FirstName");
        // Not: Burada normalde sadece o hocanın verdiği hizmetleri filtrelemek gerekir ama basitlik için tüm hizmetleri getiriyoruz.
        // Ancak GymService üzerinden gitmek daha doğru olurdu. Şimdilik Service listesi üzerinden gidelim:
        ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name");
        
        return View(new BookAppointmentViewModel());
    }

    // 3. RANDEVU KAYDET (İşlem)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookAppointmentViewModel model)
    {
        if (ModelState.IsValid)
        {
            // A. Geçerli Kullanıcıyı Bul
            var user = await _userManager.GetUserAsync(User);
            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == user!.Id);

            // Eğer üye profili yoksa oluştur (Otomatik)
            if (member == null)
            {
                member = new Member { UserId = user!.Id, JoinDate = DateTime.UtcNow };
                _context.Add(member);
                await _context.SaveChangesAsync();
            }

            // B. Seçilen Hizmeti Bul (Süre ve Fiyat için)
            var service = await _context.Services.FindAsync(model.ServiceId);
            if (service == null) return NotFound("Hizmet bulunamadı");

            // C. Bitiş Saatini Hesapla
            var endTime = model.StartTime.AddMinutes(service.DurationMinutes);

            // D. --- KRİTİK: ÇAKIŞMA KONTROLÜ (LINQ) ---
            // PDF Gereksinimi: "Randevu saati, önceki randevular dikkate alınarak uygun değilse uyar"
            bool isConflict = await _context.Appointments.AnyAsync(a => 
                a.TrainerId == model.TrainerId &&
                a.AppointmentDate == model.AppointmentDate &&
                (
                    (model.StartTime >= a.StartTime && model.StartTime < a.EndTime) || // Başlangıç çakışması
                    (endTime > a.StartTime && endTime <= a.EndTime) ||                 // Bitiş çakışması
                    (model.StartTime <= a.StartTime && endTime >= a.EndTime)           // Kapsama çakışması
                )
                && a.Status != "Cancelled" // İptal edilenleri sayma
            );

            if (isConflict)
            {
                ModelState.AddModelError("", "Seçtiğiniz saatte eğitmenimiz maalesef dolu. Lütfen başka bir saat seçiniz.");
            }
            else
            {
                // E. GymService Bağlantısını Bul (veya oluştur/seç)
                // Veritabanı şemasında Appointment -> GymServiceId istiyor.
                // Basitlik adına: Seçilen hocanın şubesindeki, seçilen hizmeti buluyoruz.
                var trainer = await _context.Trainers.FindAsync(model.TrainerId);
                var gymService = await _context.GymServices
                    .FirstOrDefaultAsync(gs => gs.GymBranchId == trainer!.GymBranchId && gs.ServiceId == model.ServiceId);

                // Eğer o şubede bu hizmet tanımlı değilse, geçici olarak oluştur veya hata ver.
                // Biz hata vermemek için şube bağımsız hizmeti alıyoruz varsayımıyla ilerleyelim,
                // ama foreign key için bir GymService kaydı lazım.
                if (gymService == null)
                {
                    // Otomatik oluştur (Senaryo gereği)
                    gymService = new GymService 
                    { 
                        GymBranchId = trainer!.GymBranchId, 
                        ServiceId = model.ServiceId, 
                        Price = service.BasePrice 
                    };
                    _context.Add(gymService);
                    await _context.SaveChangesAsync();
                }

                // F. Randevuyu Kaydet
                var appointment = new Appointment
                {
                    MemberId = member.Id,
                    TrainerId = model.TrainerId,
                    GymServiceId = gymService.Id,
                    AppointmentDate = model.AppointmentDate,
                    StartTime = model.StartTime,
                    EndTime = endTime,
                    TotalPrice = gymService.Price, // Hizmet fiyatı
                    Status = "Confirmed",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Add(appointment);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
        }

        // Hata varsa formu tekrar doldur
        ViewData["TrainerId"] = new SelectList(_context.Trainers.Include(t => t.User), "Id", "User.FirstName", model.TrainerId);
        ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name", model.ServiceId);
        return View(model);
    }
}