namespace Verdure.McpPlatform.Contracts.DTOs;

/// <summary>
/// 用户基本信息（用于公共展示）
/// </summary>
public record UserBasicInfo
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// 邮箱地址
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// 头像URL（可选，未来可支持 Gravatar）
    /// </summary>
    public string? AvatarUrl { get; init; }
}
