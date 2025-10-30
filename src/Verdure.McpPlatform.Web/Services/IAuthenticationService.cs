namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Service for managing user authentication
/// </summary>
public interface IAuthenticationService
{
    Task LoginAsync(string token);
    Task LogoutAsync();
    Task<string?> GetTokenAsync();
}
