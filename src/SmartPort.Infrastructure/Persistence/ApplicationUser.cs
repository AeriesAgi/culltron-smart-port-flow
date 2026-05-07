using Microsoft.AspNetCore.Identity;

namespace SmartPort.Infrastructure.Persistence;

/// <summary>
/// Extended Identity user with port-specific profile fields.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? Organisation { get; set; }
    public string? Terminal { get; set; }
    public string? ContactNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? TimeZone { get; set; } = "Africa/Johannesburg";

    public string FullName => $"{FirstName} {LastName}".Trim();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
