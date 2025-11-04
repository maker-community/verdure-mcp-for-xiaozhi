using System.ComponentModel.DataAnnotations;

namespace Verdure.McpPlatform.Contracts.Requests;

/// <summary>
/// Request to create a new MCP Binding
/// </summary>
public record CreateMcpServiceBindingRequest
{
    [Required(ErrorMessage = "Server ID is required")]
    [StringLength(36, MinimumLength = 36, ErrorMessage = "Server ID must be a valid GUID")]
    public string ServerId { get; init; } = string.Empty;

    [Required(ErrorMessage = "MCP Service Config ID is required")]
    [StringLength(36, MinimumLength = 36, ErrorMessage = "MCP Service Config ID must be a valid GUID")]
    public string McpServiceConfigId { get; init; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; init; }

    public List<string>? SelectedToolNames { get; init; }
}
