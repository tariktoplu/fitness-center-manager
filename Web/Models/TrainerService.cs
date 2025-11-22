namespace Web.Models;


public class TrainerService
{
    public int Id { get; set; }
    public int TrainerId { get; set; }
    public virtual Trainer Trainer { get; set; } = null!;
    
    public int ServiceId { get; set; }
    public virtual Service Service { get; set; } = null!;
}