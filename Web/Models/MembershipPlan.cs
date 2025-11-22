using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public class MembershipPlan
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public virtual Member Member { get; set; } = null!;
    
    public int MembershipTypeId { get; set; }
    public virtual MembershipType MembershipType { get; set; } = null!;
    
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int? RemainingSessions { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}