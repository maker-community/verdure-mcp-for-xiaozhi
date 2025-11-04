using System.ComponentModel.DataAnnotations;

namespace Verdure.McpPlatform.Contracts.Requests;

/// <summary>
/// Request to update an existing MCP Binding
/// </summary>
public record UpdateMcpServiceBindingRequest
{
    [Required(ErrorMessage = "MCP Service Config ID is required")]
    [StringLength(36, MinimumLength = 36, ErrorMessage = "MCP Service Config ID must be a valid GUID")]
    public string McpServiceConfigId { get; init; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; init; }

    public List<string>? SelectedToolNames { get; init; }
}
