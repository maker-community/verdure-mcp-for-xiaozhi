using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Verdure.McpPlatform.Application.Services;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Infrastructure.Identity;

namespace Verdure.McpPlatform.Infrastructure.Services;

/// <summary>
/// 用户信息服务实现
/// </summary>
public class UserInfoService : IUserInfoService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserInfoService> _logger;

    public UserInfoService(
        UserManager<ApplicationUser> userManager,
        ILogger<UserInfoService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 批量获取用户基本信息
    /// </summary>
    public async Task<Dictionary<string, UserBasicInfo>> GetUsersByIdsAsync(IEnumerable<string> userIds)
    {
        var uniqueUserIds = userIds.Distinct().ToList();
        
        if (!uniqueUserIds.Any())
        {
            _logger.LogDebug("No user IDs provided for batch lookup");
            return new Dictionary<string, UserBasicInfo>();
        }

        _logger.LogInformation(
            "Fetching {Count} unique users for display",
            uniqueUserIds.Count);

        try
        {
            // 批量查询用户 - 避免 N+1 问题
            var users = await _userManager.Users
                .Where(u => uniqueUserIds.Contains(u.Id))
                .ToListAsync();

            var result = new Dictionary<string, UserBasicInfo>();

            foreach (var user in users)
            {
                result[user.Id] = new UserBasicInfo
                {
                    UserId = user.Id,
                    DisplayName = user.DisplayName,
                    UserName = user.UserName,
                    Email = user.Email,
                    AvatarUrl = null // 未来可以支持 Gravatar 或其他头像服务
                };
            }

            _logger.LogInformation(
                "Successfully fetched {Count} users out of {Requested} requested",
                result.Count,
                uniqueUserIds.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users by IDs");
            throw;
        }
    }
}
