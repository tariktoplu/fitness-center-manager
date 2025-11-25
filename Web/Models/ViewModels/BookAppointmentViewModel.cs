using System.ComponentModel.DataAnnotations;

namespace Web.Models.ViewModels;

public class BookAppointmentViewModel
{
    [Required(ErrorMessage = "Lütfen bir eğitmen seçiniz.")]
    [Display(Name = "Eğitmen")]
    public int TrainerId { get; set; }

    [Required(ErrorMessage = "Lütfen bir hizmet seçiniz.")]
    [Display(Name = "Hizmet / Ders")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Tarih seçiniz.")]
    [DataType(DataType.Date)]
    [Display(Name = "Randevu Tarihi")]
    public DateOnly AppointmentDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

    [Required(ErrorMessage = "Saat seçiniz.")]
    [Display(Name = "Başlangıç Saati")]
    // DataType.Time attribute'ünü KALDIRIYORUZ, bazen formatı bozabiliyor.
    public TimeOnly StartTime { get; set; } = new TimeOnly(09, 00); // Varsayılan 09:00
}