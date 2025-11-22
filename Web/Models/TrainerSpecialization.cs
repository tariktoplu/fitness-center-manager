namespace Web.Models;

public class TrainerSpecialization
{
    public int Id { get; set; }
    public int TrainerId { get; set; }
    public virtual Trainer Trainer { get; set; } = null!;
    
    public int SpecializationId { get; set; }
    public virtual Specialization Specialization { get; set; } = null!;
    
    public int ExperienceYears { get; set; } = 0;
}