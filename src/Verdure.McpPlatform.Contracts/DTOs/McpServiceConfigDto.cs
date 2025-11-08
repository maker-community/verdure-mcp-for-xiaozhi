namespace Verdure.McpPlatform.Contracts.DTOs;

/// <summary>
/// Data transfer object for MCP Service Configuration
/// </summary>
public record McpServiceConfigDto
{
    /// <summary>
    /// Unique identifier (Guid Version 7 as string)
    /// </summary>
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsPublic { get; init; }
    public string? LogoUrl { get; init; }
    public string? AuthenticationType { get; init; }
    public string? AuthenticationConfig { get; init; }
    public string? Protocol { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? LastSyncedAt { get; init; }
    public List<McpToolDto> Tools { get; init; } = new();
    
    /// <summary>
    /// 创建者信息（仅在公共服务列表中填充）
    /// </summary>
    public UserBasicInfo? Creator { get; init; }
}
