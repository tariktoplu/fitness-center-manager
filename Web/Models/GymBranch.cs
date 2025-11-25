using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public class GymBranch
{
    public int Id { get; set; }
    
    [MaxLength(100)] 
    public string Name { get; set; } = null!;
    
    [MaxLength(255)] 
    public string Address { get; set; } = null!;
    
    [MaxLength(50)] 
    public string City { get; set; } = null!;
    
    [MaxLength(20)] 
    public string? PhoneNumber { get; set; }
    
    [MaxLength(100)] 
    public string? Email { get; set; }
    
    [MaxLength(500)] 
    public string? Description { get; set; }
    
    [MaxLength(255)] 
    public string? ImageUrl { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- İLİŞKİLER (EKSİK OLAN KISIM BURASIYDI) ---
    
    // 1. Çalışma Saatleri
    public virtual ICollection<GymWorkingHour> WorkingHours { get; set; } = new List<GymWorkingHour>();
    
    // 2. Hizmetler
    public virtual ICollection<GymService> GymServices { get; set; } = new List<GymService>();
    
    // 3. Antrenörler (Hatanın Sebebi Bu Satırın Eksikliğiydi)
    public virtual ICollection<Trainer> Trainers { get; set; } = new List<Trainer>();
}