namespace Verdure.McpPlatform.Contracts.DTOs;

/// <summary>
/// Data transfer object for MCP Server
/// </summary>
public record XiaozhiMcpEndpointDto
{
    /// <summary>
    /// Unique identifier (Guid Version 7 as string)
    /// </summary>
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsConnected { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? LastConnectedAt { get; init; }
    public DateTime? LastDisconnectedAt { get; init; }
    public List<McpServiceBindingDto> ServiceBindings { get; init; } = new();
}
