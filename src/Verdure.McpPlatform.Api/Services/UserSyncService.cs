using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Verdure.McpPlatform.Infrastructure.Identity;

namespace Verdure.McpPlatform.Api.Services;

/// <summary>
/// 用户同步服务接口
/// </summary>
public interface IUserSyncService
{
    /// <summary>
    /// 从 JWT Claims 同步用户信息到 Identity Database
    /// </summary>
    Task<UserSyncResult> SyncUserFromJwtClaimsAsync(
        ClaimsPrincipal principal, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 用户同步结果
/// </summary>
public record UserSyncResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public bool IsNewUser { get; init; }
}

/// <summary>
/// 用户同步服务实现
/// </summary>
public class UserSyncService : IUserSyncService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserSyncService> _logger;

    public UserSyncService(
        UserManager<ApplicationUser> userManager,
        ILogger<UserSyncService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<UserSyncResult> SyncUserFromJwtClaimsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. 提取用户 ID (sub claim)
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Cannot sync user: No 'sub' claim found in JWT token");
                return new UserSyncResult
                {
                    Success = false,
                    Message = "No user ID found in JWT token"
                };
            }

            // 2. 提取其他用户信息
            var email = principal.FindFirst(ClaimTypes.Email)?.Value
                ?? principal.FindFirst("email")?.Value;

            var userName = principal.FindFirst(ClaimTypes.Name)?.Value
                ?? principal.FindFirst("preferred_username")?.Value
                ?? email; // 如果没有 name，使用 email

            var displayName = principal.FindFirst("name")?.Value
                ?? principal.FindFirst("given_name")?.Value
                ?? userName;

            // 3. 检查用户是否存在
            var user = await _userManager.FindByIdAsync(userId);
            var isNewUser = user == null;

            if (isNewUser)
            {
                // 3a. 创建新用户
                user = new ApplicationUser
                {
                    Id = userId, // 使用 Keycloak 的 sub 作为 ID
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true, // Keycloak 已验证
                    DisplayName = displayName,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation(
                        "Created new user {UserId} ({UserName}) from Keycloak JWT claims",
                        userId, userName);

                    return new UserSyncResult
                    {
                        Success = true,
                        Message = "User created successfully",
                        UserId = userId,
                        IsNewUser = true
                    };
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError(
                        "Failed to create user {UserId}: {Errors}",
                        userId, errors);

                    return new UserSyncResult
                    {
                        Success = false,
                        Message = $"Failed to create user: {errors}",
                        UserId = userId,
                        IsNewUser = true
                    };
                }
            }
            else
            {
                // 3b. 更新现有用户信息（可选）
                var updated = false;

                if (user.Email != email && !string.IsNullOrEmpty(email))
                {
                    user.Email = email;
                    updated = true;
                }

                if (user.DisplayName != displayName && !string.IsNullOrEmpty(displayName))
                {
                    user.DisplayName = displayName;
                    updated = true;
                }

                if (updated)
                {
                    var result = await _userManager.UpdateAsync(user);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation(
                            "Updated user {UserId} ({UserName}) from Keycloak JWT claims",
                            userId, userName);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to update user {UserId}: {Errors}",
                            userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    _logger.LogDebug("User {UserId} information unchanged, skipping update", userId);
                }

                return new UserSyncResult
                {
                    Success = true,
                    Message = updated ? "User updated successfully" : "User already exists, no update needed",
                    UserId = userId,
                    IsNewUser = false
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user from JWT claims");
            return new UserSyncResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }
}
