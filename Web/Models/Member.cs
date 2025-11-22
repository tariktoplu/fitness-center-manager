using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Models;

public class Member
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual AppUser User { get; set; } = null!;

    public DateOnly? BirthDate { get; set; } // SQL DATE
    [MaxLength(10)] public string? Gender { get; set; }
    [MaxLength(100)] public string? EmergencyContact { get; set; }
    [MaxLength(20)] public string? EmergencyPhone { get; set; }
    [MaxLength(255)] public string? ProfileImageUrl { get; set; }
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}