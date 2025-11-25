namespace Web.Models.ViewModels;

public class BranchHoursViewModel
{
    public int GymBranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public List<BranchHourItem> Hours { get; set; } = new();
}

public class BranchHourItem
{
    public int DayOfWeek { get; set; } // 0: Pazar, 1: Pazartesi...
    public string DayName { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public TimeSpan OpeningTime { get; set; } = new TimeSpan(9, 0, 0);
    public TimeSpan ClosingTime { get; set; } = new TimeSpan(22, 0, 0);
}