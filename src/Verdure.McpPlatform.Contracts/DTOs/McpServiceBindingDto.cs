namespace Verdure.McpPlatform.Contracts.DTOs;

/// <summary>
/// Data transfer object for MCP Binding
/// </summary>
public record McpServiceBindingDto
{
    public int Id { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string NodeAddress { get; init; } = string.Empty;
    public int XiaozhiConnectionId { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
