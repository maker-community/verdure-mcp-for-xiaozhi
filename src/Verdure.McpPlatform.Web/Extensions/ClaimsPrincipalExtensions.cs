using System.Security.Claims;

namespace Verdure.McpPlatform.Web.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to simplify role checking
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Get the user ID from claims
    /// </summary>
    public static string GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("User ID not found in claims");
    }

    /// <summary>
    /// Get the username from claims
    /// </summary>
    public static string GetUsername(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? user.FindFirst("name")?.Value
            ?? "Unknown";
    }

    /// <summary>
    /// Get the email from claims
    /// </summary>
    public static string GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("email")?.Value
            ?? string.Empty;
    }

    /// <summary>
    /// Get all roles for the user
    /// </summary>
    public static List<string> GetRoles(this ClaimsPrincipal user)
    {
        var roleClaimType = user.Identity is ClaimsIdentity identity
            ? identity.RoleClaimType
            : ClaimTypes.Role;

        return user.FindAll(roleClaimType)
            .Select(c => c.Value)
            .ToList();
    }

    /// <summary>
    /// Check if user has any of the specified roles
    /// </summary>
    public static bool HasAnyRole(this ClaimsPrincipal user, params string[] roles)
    {
        if (user == null || !user.Identity?.IsAuthenticated == true)
            return false;

        return roles.Any(role => user.IsInRole(role));
    }

    /// <summary>
    /// Check if user has all of the specified roles
    /// </summary>
    public static bool HasAllRoles(this ClaimsPrincipal user, params string[] roles)
    {
        if (user == null || !user.Identity?.IsAuthenticated == true)
            return false;

        return roles.All(role => user.IsInRole(role));
    }

    /// <summary>
    /// Check if user is an Admin
    /// </summary>
    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("Admin");
    }

    /// <summary>
    /// Get a specific claim value
    /// </summary>
    public static string? GetClaimValue(this ClaimsPrincipal user, string claimType)
    {
        return user.FindFirst(claimType)?.Value;
    }

    /// <summary>
    /// Get all values for a claim type
    /// </summary>
    public static List<string> GetClaimValues(this ClaimsPrincipal user, string claimType)
    {
        return user.FindAll(claimType)
            .Select(c => c.Value)
            .ToList();
    }
}
