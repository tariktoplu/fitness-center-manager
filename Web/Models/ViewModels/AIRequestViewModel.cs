using System.ComponentModel.DataAnnotations;

namespace Web.Models.ViewModels; // Burasının "Web" olduguna dikkat et

public class AIRequestViewModel
{
    [Required(ErrorMessage = "Boy bilgisi gereklidir (cm).")]
    [Range(100, 250, ErrorMessage = "Geçerli bir boy giriniz.")]
    public decimal Height { get; set; } // CM

    [Required(ErrorMessage = "Kilo bilgisi gereklidir (kg).")]
    [Range(30, 200, ErrorMessage = "Geçerli bir kilo giriniz.")]
    public decimal Weight { get; set; } // KG

    [Display(Name = "Hedefiniz")]
    public string Goal { get; set; } = "Fit Olmak"; 
    
    // Sonuçları göstermek için
    public string? AIResponse { get; set; }
    public string? BMIResult { get; set; }
}