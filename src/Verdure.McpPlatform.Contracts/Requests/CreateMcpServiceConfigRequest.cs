using System.ComponentModel.DataAnnotations;

namespace Verdure.McpPlatform.Contracts.Requests;

/// <summary>
/// Request to create a new MCP Service Configuration
/// </summary>
public class CreateMcpServiceConfigRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Endpoint { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool IsPublic { get; set; } = false;

    [StringLength(50)]
    public string? AuthenticationType { get; set; }

    [StringLength(2000)]
    public string? AuthenticationConfig { get; set; }

    [StringLength(50)]
    public string? Protocol { get; set; }
}
