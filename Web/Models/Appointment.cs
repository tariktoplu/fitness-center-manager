using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public class Appointment
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public virtual Member Member { get; set; } = null!;

    public int TrainerId { get; set; }
    public virtual Trainer Trainer { get; set; } = null!;

    public int GymServiceId { get; set; }
    public virtual GymService GymService { get; set; } = null!;

    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal TotalPrice { get; set; }
    
    [MaxLength(20)] public string Status { get; set; } = "Pending";
    [MaxLength(500)] public string? Notes { get; set; }
    [MaxLength(255)] public string? CancellationReason { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}