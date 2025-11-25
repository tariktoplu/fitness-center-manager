using System.ComponentModel.DataAnnotations;

namespace Web.Models.ViewModels;

public class ProfileViewModel
{
    [Display(Name = "Ad")]
    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Soyad")]
    [Required(ErrorMessage = "Soyad alanı zorunludur.")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty; // Sadece okunur olacak

    [Display(Name = "Telefon Numarası")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Doğum Tarihi")]
    [DataType(DataType.Date)]
    public DateOnly? BirthDate { get; set; }

    [Display(Name = "Acil Durumda Aranacak Kişi")]
    public string? EmergencyContact { get; set; }

    // Profil Resmi Yükleme
    [Display(Name = "Profil Fotoğrafı")]
    public IFormFile? ProfilePicture { get; set; }
    
    // Mevcut Resim Yolu (Göstermek için)
    public string? ExistingProfilePicture { get; set; }
}