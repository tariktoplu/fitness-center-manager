using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public class Specialization
{
    public int Id { get; set; }
    [MaxLength(50)] public string Name { get; set; } = null!;
    [MaxLength(255)] public string? Description { get; set; }
}