using System.ComponentModel.DataAnnotations;

namespace Web.Models.ViewModels;

public class TrainerViewModel
{
    // --- KULLANICI BİLGİLERİ ---
    [Required(ErrorMessage = "Ad zorunludur")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    // --- ANTRENÖR DETAYLARI ---
    [Display(Name = "Çalışacağı Şube")]
    [Required(ErrorMessage = "Lütfen bir şube seçiniz")]
    public int GymBranchId { get; set; }

    [Display(Name = "Biyografi")]
    public string? Biography { get; set; }

    [Display(Name = "Saatlik Ücret")]
    public decimal? HourlyRate { get; set; }

    [Display(Name = "Profil Fotoğrafı")]
    public IFormFile? ProfileImage { get; set; } // Dosya yükleme için
    
    [Display(Name = "Uzmanlık Alanları")]
    public List<int> SelectedSpecializationIds { get; set; } = new();
}