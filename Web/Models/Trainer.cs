using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Models;

public class Trainer
{
    public int Id { get; set; }
    
    // User ile 1-1
    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual AppUser User { get; set; } = null!;

    public int GymBranchId { get; set; }
    public virtual GymBranch GymBranch { get; set; } = null!;

    [MaxLength(1000)] public string? Biography { get; set; }
    [MaxLength(255)] public string? ProfileImageUrl { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal Rating { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateOnly HireDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow); // SQL DATE

    public virtual ICollection<TrainerService> TrainerServices { get; set; } = new List<TrainerService>();
    public virtual ICollection<TrainerSpecialization> Specializations { get; set; } = new List<TrainerSpecialization>();
}