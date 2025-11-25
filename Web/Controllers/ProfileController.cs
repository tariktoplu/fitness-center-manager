using Web.Data;
using Web.Models;
using Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager; // EKLENDİ
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    // Constructor'a SignInManager eklendi
    public ProfileController(UserManager<AppUser> userManager, 
                             SignInManager<AppUser> signInManager, 
                             AppDbContext context, 
                             IWebHostEnvironment webHostEnvironment)
    {
        _userManager = userManager;
        _signInManager = signInManager; // EKLENDİ
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET: Profil Sayfası
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == user.Id);

        if (member == null)
        {
            member = new Member { UserId = user.Id, JoinDate = DateTime.UtcNow, IsActive = true };
            _context.Add(member);
            await _context.SaveChangesAsync();
        }

        var model = new ProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            BirthDate = member.BirthDate,
            EmergencyContact = member.EmergencyContact,
            ExistingProfilePicture = member.ProfileImageUrl
        };

        return View(model);
    }

    // POST: Profil Güncelleme
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        model.Email = user.Email!; 

        if (ModelState.IsValid)
        {
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
                return View(model);
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == user.Id);
            if (member != null)
            {
                member.BirthDate = model.BirthDate;
                member.EmergencyContact = model.EmergencyContact;

                if (model.ProfilePicture != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "members");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    if (!string.IsNullOrEmpty(member.ProfileImageUrl))
                    {
                        string oldPath = Path.Combine(uploadsFolder, member.ProfileImageUrl);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfilePicture.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfilePicture.CopyToAsync(fileStream);
                    }

                    member.ProfileImageUrl = uniqueFileName;
                }

                _context.Update(member);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Profil bilgileriniz başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
        }

        return View(model);
    }

    // Şifre Değiştirme Sayfası (GET)
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    // Şifre Değiştirme İşlemi (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (result.Succeeded)
        {
            // DÜZELTİLEN KISIM: SignInManager kullanılarak çıkış yapılıyor
            await _signInManager.SignOutAsync(); 
            
            TempData["Success"] = "Şifreniz değiştirildi. Lütfen yeni şifrenizle giriş yapın.";
            return RedirectToAction("Login", "Account");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View(model);
    }
}