namespace Web.Models;

public class GymService
{
    public int Id { get; set; }
    public int GymBranchId { get; set; }
    public virtual GymBranch GymBranch { get; set; } = null!;
    
    public int ServiceId { get; set; }
    public virtual Service Service { get; set; } = null!;
    
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
}