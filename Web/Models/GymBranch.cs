using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public class GymBranch
{
    public int Id { get; set; }
    [MaxLength(100)] public string Name { get; set; } = null!;
    [MaxLength(255)] public string Address { get; set; } = null!;
    [MaxLength(50)] public string City { get; set; } = null!;
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    [MaxLength(100)] public string? Email { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    [MaxLength(255)] public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<GymWorkingHour> WorkingHours { get; set; } = new List<GymWorkingHour>();
    public virtual ICollection<GymService> GymServices { get; set; } = new List<GymService>();
}