using Microsoft.AspNetCore.Identity;

namespace Web.Models;

public class AppRole : IdentityRole<int>
{
    public string? Description { get; set; }
}