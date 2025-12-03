using Web.Models;
using Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
    : Controller
{
    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login()
    {
        // Eğer kullanıcı zaten giriş yapmışsa Ana Sayfaya at
        if (User.Identity!.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Kullanıcıyı email ile bul
        var user = await userManager.FindByEmailAsync(model.Email);
        
        if (user != null)
        {
            // Şifre kontrolü ve giriş yapma işlemi
            // "false" -> lockoutOnFailure (şifreyi çok yanlış girince hesabı kilitleme) kapalı
            var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        ModelState.AddModelError("", "Geçersiz e-posta veya şifre.");
        return View(model);
    }
    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity!.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    // POST: /Account/Register
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Yeni Kullanıcı Oluşturma
        var user = new AppUser
        {
            UserName = model.Email, // Identity Username olarak Email kullanıyoruz
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Kullanıcıya otomatik olarak "Member" (Üye) rolü ver
            await userManager.AddToRoleAsync(user, "Member");
            
            // Member tablosuna da boş bir kayıt aç (İlişki için)
            var memberProfile = new Member { UserId = user.Id };
            // Not: Member tablosuna ekleme işlemini DbContext ile yapabiliriz
            // Ancak şimdilik sadece User tablosu yeterli, Member profili detayları sonra doldurulabilir.

            // Kayıt olduktan sonra otomatik giriş yap
            await signInManager.SignInAsync(user, isPersistent: false);
            
            return RedirectToAction("Index", "Home");
        }

        // Hata varsa ekrana bas (Örn: Şifre çok basit, email zaten var vb.)
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View(model);
    }

    // Çıkış Yapma (Logout)
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
    
    // Yetkisiz Giriş Sayfası
    public IActionResult AccessDenied()
    {
        return View();
    }
}