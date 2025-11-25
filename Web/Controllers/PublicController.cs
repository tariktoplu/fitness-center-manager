using Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

public class PublicController : Controller
{
    private readonly AppDbContext _context;

    public PublicController(AppDbContext context)
    {
        _context = context;
    }

    // 1. TÜM SALONLARI LİSTELE
    public async Task<IActionResult> Gyms()
    {
        var gyms = await _context.GymBranches
            .Where(g => g.IsActive) // Sadece aktif olanlar
            .ToListAsync();
        return View(gyms);
    }

    // 2. SALON DETAYI (HİZMETLER VE HOCALAR BURADA)
    public async Task<IActionResult> GymDetails(int id)
    {
        var gym = await _context.GymBranches
            .Include(g => g.GymServices).ThenInclude(gs => gs.Service) // Hizmetleri getir
            .Include(g => g.Trainers).ThenInclude(t => t.User) // Hocaları getir
            .Include(g => g.Trainers).ThenInclude(t => t.Specializations).ThenInclude(ts => ts.Specialization) // Uzmanlıkları getir
            .FirstOrDefaultAsync(g => g.Id == id);

        if (gym == null) return NotFound();

        return View(gym);
    }

    // 3. TÜM EĞİTMENLERİMİZ (GENEL LİSTE)
    public async Task<IActionResult> Trainers()
    {
        var trainers = await _context.Trainers
            .Include(t => t.User)
            .Include(t => t.GymBranch)
            .Include(t => t.Specializations).ThenInclude(ts => ts.Specialization)
            .Where(t => t.IsActive)
            .ToListAsync();

        return View(trainers);
    }
}