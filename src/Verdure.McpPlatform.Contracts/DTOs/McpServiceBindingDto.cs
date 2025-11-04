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
    /// <summary>
    /// Foreign key to XiaozhiConnection (Guid Version 7 as string)
    /// </summary>
    public string XiaozhiConnectionId { get; init; } = string.Empty;
    /// <summary>
    /// Reference to McpServiceConfig (Guid Version 7 as string) - required
    /// </summary>
    public string McpServiceConfigId { get; init; } = string.Empty;
    /// <summary>
    /// MCP Service Configuration details (populated from McpServiceConfig)
    /// </summary>
    public McpServiceConfigDto? McpServiceConfig { get; init; }
    /// <summary>
    /// Service name (from McpServiceConfig for convenience)
    /// </summary>
    public string ServiceName { get; init; } = string.Empty;
    /// <summary>
    /// Service endpoint (from McpServiceConfig for convenience)
    /// </summary>
    public string NodeAddress { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public List<string> SelectedToolNames { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
