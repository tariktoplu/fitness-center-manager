namespace Web.Models;

public class TrainerAvailability
{
    public int Id { get; set; }
    public int TrainerId { get; set; }
    public virtual Trainer Trainer { get; set; } = null!;
    
    public int DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsAvailable { get; set; } = true;
}