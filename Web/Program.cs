
using Web.Data;
using Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabanı Bağlantısını Al
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. DbContext'i Servis Olarak Ekle (Hatanın sebebi buranın eksik olmasıydı)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Identity (Kullanıcı Giriş Sistemi) Ayarları
builder.Services.AddIdentity<AppUser, AppRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 3; // Test için kısa şifre
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- SEED DATA (Admin Ekleme) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Veritabanını oluştur ve verileri bas
        await DbSeeder.SeedRolesAndUsers(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı seed edilirken hata oluştu.");
    }
}
// --------------------------------

// HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Giriş yapma yeteneği
app.UseAuthorization();  // Yetki kontrolü

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();