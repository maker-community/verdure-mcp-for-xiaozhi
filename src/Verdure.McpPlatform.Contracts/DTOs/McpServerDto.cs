namespace Verdure.McpPlatform.Contracts.DTOs;

/// <summary>
/// Data transfer object for MCP Server
/// </summary>
public record McpServerDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsConnected { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? LastConnectedAt { get; init; }
    public DateTime? LastDisconnectedAt { get; init; }
    public List<McpBindingDto> Bindings { get; init; } = new();
}
