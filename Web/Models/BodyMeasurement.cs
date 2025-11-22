using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public class BodyMeasurement
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public virtual Member Member { get; set; } = null!;

    public DateOnly MeasurementDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public decimal Height { get; set; } // CM
    public decimal Weight { get; set; } // KG
    public decimal? BodyFatPercentage { get; set; }
    
    // Bu alan veritabanında otomatik hesaplanacak, set edilemez.
    public decimal BMI { get; private set; } 

    public decimal? ChestCm { get; set; }
    public decimal? WaistCm { get; set; }
    public decimal? HipsCm { get; set; }
    public decimal? ArmCm { get; set; }
    public decimal? ThighCm { get; set; }
    [MaxLength(20)] public string? BodyType { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
}

public class AIRecommendation
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public virtual Member Member { get; set; } = null!;

    [MaxLength(50)] public string RequestType { get; set; } = null!;
    public string? InputData { get; set; }
    [MaxLength(255)] public string? InputImageUrl { get; set; }
    public string? Recommendation { get; set; }
    [MaxLength(255)] public string? GeneratedImageUrl { get; set; }
    [MaxLength(50)] public string? AIModel { get; set; }
    public bool IsSuccessful { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}