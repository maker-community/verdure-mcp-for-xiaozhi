using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Verdure.McpPlatform.Api.Services;

namespace Verdure.McpPlatform.Api.Apis;

/// <summary>
/// User synchronization API endpoints
/// </summary>
public static class UserApi
{
    public static RouteGroupBuilder MapUserApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/users")
            .WithTags("Users");

        // 用户同步接口 - 需要认证
        api.MapPost("/sync", SyncCurrentUserAsync)
            .RequireAuthorization()
            .WithName("SyncCurrentUser")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Sync current user from JWT claims to Identity database";
                operation.Description = "This endpoint should be called by the frontend after successful login to ensure user exists in the Identity database.";
                return operation;
            })
            .Produces<UserSyncResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // 获取当前用户信息接口
        api.MapGet("/me", GetCurrentUserAsync)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get current authenticated user information";
                operation.Description = "Returns the user information from the Identity database.";
                return operation;
            })
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        return api;
    }

    /// <summary>
    /// 同步当前用户
    /// </summary>
    private static async Task<IResult> SyncCurrentUserAsync(
        ClaimsPrincipal user,
        IUserSyncService userSyncService,
        ILogger<Program> logger)
    {
        try
        {
            // 调用用户同步服务
            var result = await userSyncService.SyncUserFromJwtClaimsAsync(user);

            if (result.Success)
            {
                logger.LogInformation(
                    "User sync successful: UserId={UserId}, IsNewUser={IsNewUser}",
                    result.UserId, result.IsNewUser);

                return Results.Ok(new UserSyncResponse
                {
                    Success = true,
                    Message = result.Message,
                    UserId = result.UserId,
                    IsNewUser = result.IsNewUser
                });
            }
            else
            {
                logger.LogWarning(
                    "User sync failed: {Message}",
                    result.Message);

                return Results.Problem(
                    detail: result.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "User sync failed");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in user sync endpoint");
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal server error");
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal user,
        IIdentityService identityService,
        Microsoft.AspNetCore.Identity.UserManager<Infrastructure.Identity.ApplicationUser> userManager,
        ILogger<Program> logger)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            var appUser = await userManager.FindByIdAsync(userId);

            if (appUser == null)
            {
                logger.LogWarning("User {UserId} not found in Identity database", userId);
                return Results.NotFound(new ProblemDetails
                {
                    Title = "User not found",
                    Detail = "User has not been synced to the Identity database. Please call /api/users/sync first.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Results.Ok(new CurrentUserResponse
            {
                UserId = appUser.Id,
                UserName = appUser.UserName,
                Email = appUser.Email,
                DisplayName = appUser.DisplayName,
                EmailConfirmed = appUser.EmailConfirmed,
                CreatedAt = appUser.CreatedAt
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access to user info");
            return Results.Unauthorized();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting current user");
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal server error");
        }
    }
}

/// <summary>
/// 用户同步响应
/// </summary>
public record UserSyncResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public bool IsNewUser { get; init; }
}

/// <summary>
/// 当前用户信息响应
/// </summary>
public record CurrentUserResponse
{
    public string UserId { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public string? Email { get; init; }
    public string? DisplayName { get; init; }
    public bool EmailConfirmed { get; init; }
    public DateTime CreatedAt { get; init; }
}
