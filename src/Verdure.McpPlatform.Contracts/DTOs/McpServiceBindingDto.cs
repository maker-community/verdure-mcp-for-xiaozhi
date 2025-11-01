namespace Verdure.McpPlatform.Contracts.DTOs;

/// <summary>
/// Data transfer object for MCP Binding
/// </summary>
public record McpServiceBindingDto
{
    /// <summary>
    /// Unique identifier (Guid Version 7 as string)
    /// </summary>
    public string Id { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string NodeAddress { get; init; } = string.Empty;
    /// <summary>
    /// Foreign key to XiaozhiConnection (Guid Version 7 as string)
    /// </summary>
    public string XiaozhiConnectionId { get; init; } = string.Empty;
    /// <summary>
    /// Optional reference to McpServiceConfig (Guid Version 7 as string)
    /// </summary>
    public string? McpServiceConfigId { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public List<string> SelectedToolNames { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
