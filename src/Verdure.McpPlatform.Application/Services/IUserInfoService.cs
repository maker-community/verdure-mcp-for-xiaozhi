using Verdure.McpPlatform.Contracts.DTOs;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// 用户信息服务接口
/// </summary>
public interface IUserInfoService
{
    /// <summary>
    /// 批量获取用户基本信息
    /// </summary>
    /// <param name="userIds">用户ID列表</param>
    /// <returns>用户ID到用户信息的映射字典</returns>
    Task<Dictionary<string, UserBasicInfo>> GetUsersByIdsAsync(IEnumerable<string> userIds);
}
