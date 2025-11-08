namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// User client service interface
/// </summary>
public interface IUserClientService
{
    /// <summary>
    /// Sync current user from JWT claims to Identity database
    /// This should be called after successful login
    /// </summary>
    Task<UserSyncResult> SyncCurrentUserAsync();

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    Task<CurrentUserInfo?> GetCurrentUserAsync();
}

/// <summary>
/// User sync result
/// </summary>
public record UserSyncResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public bool IsNewUser { get; init; }
}

/// <summary>
/// Current user information
/// </summary>
public record CurrentUserInfo
{
    public string UserId { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public string? Email { get; init; }
    public string? DisplayName { get; init; }
    public bool EmailConfirmed { get; init; }
    public DateTime CreatedAt { get; init; }
}
