using Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Web.Data;

public static class DbSeeder
{
    public static async Task SeedRolesAndUsers(IServiceProvider serviceProvider)
    {
        // Servisleri çağırıyoruz
        var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var context = serviceProvider.GetRequiredService<AppDbContext>();

        // 1. Rolleri Ekle
        string[] roleNames = { "Admin", "Trainer", "Member" };
        
        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new AppRole { Name = roleName, Description = $"{roleName} rolü" });
            }
        }

        // 2. Admin Kullanıcısını Ekle (Senin Bilgilerinle)
        var adminEmail = "g231210010@sakarya.edu.tr"; 
        
        // Kullanıcı var mı kontrol et
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var newAdmin = new AppUser
            {
                UserName = adminEmail, // Identity'de UserName zorunludur
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            // Şifre: "sau" 
            // (Program.cs'deki şifre kuralını 3 karaktere düşürdüğümüz için hata vermez)
            var result = await userManager.CreateAsync(newAdmin, "sau"); 

            if (result.Succeeded)
            {
                // Admin rolünü ata
                await userManager.AddToRoleAsync(newAdmin, "Admin");
            }
        }

        // 3. Hizmet Kategorilerini Ekle (Varsa eklemez)
        if (!context.ServiceCategories.Any())
        {
            var categories = new List<ServiceCategory>
            {
                new() { Name = "Fitness", Description = "Ağırlık ve güç antrenmanları" },
                new() { Name = "Yoga", Description = "Esneklik ve zihin rahatlatma" },
                new() { Name = "Pilates", Description = "Core bölgesi güçlendirme" },
                new() { Name = "Kardio", Description = "Kondisyon ve yağ yakımı" },
                new() { Name = "Personal Training", Description = "Birebir özel ders" }
            };
            await context.ServiceCategories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 4. Üyelik Tiplerini Ekle
        if (!context.MembershipTypes.Any())
        {
            var types = new List<MembershipType>
            {
                new() { Name = "Aylık Basic", Description = "Temel üyelik", DurationDays = 30, Price = 500, MaxSessionsPerMonth = 12 },
                new() { Name = "Aylık Premium", Description = "Sınırsız erişim", DurationDays = 30, Price = 800, MaxSessionsPerMonth = null },
                new() { Name = "3 Aylık", Description = "Avantajlı paket", DurationDays = 90, Price = 1350, MaxSessionsPerMonth = 40 },
                new() { Name = "Yıllık", Description = "En iyi fiyat", DurationDays = 365, Price = 4800, MaxSessionsPerMonth = null }
            };
            await context.MembershipTypes.AddRangeAsync(types);
            await context.SaveChangesAsync();
        }
        
        // 5. Uzmanlık Alanlarını Ekle
        if (!context.Specializations.Any())
        {
            var specs = new List<Specialization>
            {
                new() { Name = "Kas Geliştirme" },
                new() { Name = "Kilo Verme" },
                new() { Name = "Fonksiyonel Antrenman" },
                new() { Name = "Rehabilitasyon" }
            };
            await context.Specializations.AddRangeAsync(specs);
            await context.SaveChangesAsync();
        }
    }
}