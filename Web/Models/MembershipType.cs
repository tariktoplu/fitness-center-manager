using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public class MembershipType
{
    public int Id { get; set; }
    [MaxLength(50)] public string Name { get; set; } = null!;
    [MaxLength(255)] public string? Description { get; set; }
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public int? MaxSessionsPerMonth { get; set; }
    public bool IsActive { get; set; } = true;
}