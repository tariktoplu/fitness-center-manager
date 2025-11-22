using Web.Data;
using Web.Models;
using Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

[Authorize(Roles = "Admin")] // Sadece Admin görebilir
public class TrainersController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _webHostEnvironment; // Dosya kaydetmek için

    public TrainersController(AppDbContext context, UserManager<AppUser> userManager, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
    }

    // 1. LİSTELEME
    public async Task<IActionResult> Index()
    {
        // Include ile ilişkili verileri (User ve GymBranch) çekiyoruz
        var trainers = await _context.Trainers
            .Include(t => t.User)
            .Include(t => t.GymBranch)
            .ToListAsync();
        return View(trainers);
    }

    // 2. EKLEME SAYFASI (GET)
    [HttpGet]
    public IActionResult Create()
    {
        // Dropdown için Şubeleri ViewBag'e atıyoruz
        ViewData["GymBranchId"] = new SelectList(_context.GymBranches, "Id", "Name");
        return View();
    }

    // 2. EKLEME İŞLEMİ (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainerViewModel model)
    {
        if (ModelState.IsValid)
        {
            // A. Önce Kullanıcıyı (Identity) Oluştur
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
                // B. Kullanıcıya "Trainer" rolünü ver
                await _userManager.AddToRoleAsync(user, "Trainer");

                // C. Resim Yükleme İşlemi
                string? uniqueFileName = null;
                if (model.ProfileImage != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "trainers");
                    // Klasör yoksa oluştur
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfileImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfileImage.CopyToAsync(fileStream);
                    }
                }

                // D. Antrenör Profilini Oluştur
                var trainer = new Trainer
                {
                    UserId = user.Id,
                    GymBranchId = model.GymBranchId,
                    Biography = model.Biography,
                    HourlyRate = model.HourlyRate,
                    ProfileImageUrl = uniqueFileName, // Resim adı
                    HireDate = DateOnly.FromDateTime(DateTime.UtcNow)
                };

                _context.Add(trainer);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            // Hata varsa modele ekle
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        // Hata durumunda Dropdown'ı tekrar doldur
        ViewData["GymBranchId"] = new SelectList(_context.GymBranches, "Id", "Name", model.GymBranchId);
        return View(model);
    }
    
    // Not: Edit ve Delete işlemlerini şimdilik atlıyorum, önce eklemeyi başaralım.
}