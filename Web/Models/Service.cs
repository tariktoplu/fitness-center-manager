using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public class Service
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public virtual ServiceCategory Category { get; set; } = null!;

    [MaxLength(100)] public string Name { get; set; } = null!;
    [MaxLength(500)] public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}