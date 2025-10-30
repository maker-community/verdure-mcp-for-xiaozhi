using System.ComponentModel.DataAnnotations;

namespace Verdure.McpPlatform.Contracts.Requests;

/// <summary>
/// Request to update an existing MCP Binding
/// </summary>
public record UpdateMcpBindingRequest
{
    [Required(ErrorMessage = "Service name is required")]
    [StringLength(200, ErrorMessage = "Service name cannot exceed 200 characters")]
    public string ServiceName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Node address is required")]
    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(500, ErrorMessage = "Node address cannot exceed 500 characters")]
    public string NodeAddress { get; init; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; init; }
}
