using Microsoft.AspNetCore.Identity;

namespace Verdure.McpPlatform.Infrastructure.Identity;

/// <summary>
/// Application user entity extending IdentityUser
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
}
