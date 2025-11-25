namespace Web.Models.ViewModels;

public class TrainerAvailabilityViewModel
{
    public int TrainerId { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public List<TrainerAvailabilityItem> WeeklySchedule { get; set; } = new();
}

public class TrainerAvailabilityItem
{
    public int DayOfWeek { get; set; } // 0: Pazar, 1: Pzt...
    public string DayName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } // Çalışıyor mu?
    public TimeSpan StartTime { get; set; } = new TimeSpan(09, 00, 0);
    public TimeSpan EndTime { get; set; } = new TimeSpan(18, 00, 0);
}