using System.Security.Claims;

namespace Verdure.McpPlatform.Api.Services;

/// <summary>
/// Service for getting current user identity
/// </summary>
public interface IIdentityService
{
    string GetUserIdentity();
    string GetUserName();
}

/// <summary>
/// Implementation of Identity Service
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentityService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public string GetUserIdentity()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("User not authenticated");
    }

    public string GetUserName()
    {
        return _httpContextAccessor.HttpContext?.User.Identity?.Name
            ?? throw new UnauthorizedAccessException("User not authenticated");
    }
}
