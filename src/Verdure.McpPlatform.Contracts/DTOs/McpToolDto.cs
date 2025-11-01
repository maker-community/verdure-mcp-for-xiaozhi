namespace Verdure.McpPlatform.Contracts.DTOs;

/// <summary>
/// Data transfer object for MCP Tool
/// </summary>
public record McpToolDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string McpServiceConfigId { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? InputSchema { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
