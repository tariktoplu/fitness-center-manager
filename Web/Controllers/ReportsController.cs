using Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

[Route("api/[controller]")] // URL: /api/reports
[ApiController]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    // 1. TÜM ANTRENÖRLERİ LİSTELEME (JSON)
    // GET: /api/reports/trainers
    [HttpGet("trainers")]
    public async Task<IActionResult> GetTrainers()
    {
        var trainers = await _context.Trainers
            .Include(t => t.User)
            .Include(t => t.GymBranch)
            .Select(t => new 
            {
                // Sadece gerekli verileri seçiyoruz (DTO Mantığı)
                AdSoyad = t.User.FirstName + " " + t.User.LastName,
                Uzmanlik = t.Biography, // Varsa uzmanlık tablosundan da çekilebilir
                Sube = t.GymBranch.Name,
                SaatlikUcret = t.HourlyRate,
                Puan = t.Rating
            })
            .ToListAsync();

        return Ok(trainers);
    }

    // 2. BELİRLİ TARİHTEKİ RANDEVULARI GETİRME
    // GET: /api/reports/appointments?date=2025-11-24
    [HttpGet("appointments")]
    public async Task<IActionResult> GetAppointments([FromQuery] string? date)
    {
        var query = _context.Appointments
            .Include(a => a.Trainer).ThenInclude(t => t.User)
            .Include(a => a.GymService).ThenInclude(gs => gs.Service)
            .AsQueryable();

        // Eğer tarih parametresi geldiyse filtrele
        if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out DateOnly filterDate))
        {
            query = query.Where(a => a.AppointmentDate == filterDate);
        }

        var appointments = await query
            .Select(a => new 
            {
                Tarih = a.AppointmentDate,
                Baslangic = a.StartTime,
                Bitis = a.EndTime,
                Egitmen = a.Trainer.User.FirstName + " " + a.Trainer.User.LastName,
                Hizmet = a.GymService.Service.Name,
                Durum = a.Status
            })
            .ToListAsync();

        return Ok(appointments);
    }
}