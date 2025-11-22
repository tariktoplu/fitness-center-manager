namespace Web.Models;

public class GymWorkingHour
{
    public int Id { get; set; }
    public int GymBranchId { get; set; }
    public virtual GymBranch GymBranch { get; set; } = null!;
    
    public int DayOfWeek { get; set; } // 0-6
    public TimeOnly OpeningTime { get; set; } // SQL TIME karşılığı
    public TimeOnly ClosingTime { get; set; } // SQL TIME karşılığı
    public bool IsClosed { get; set; } = false;
}