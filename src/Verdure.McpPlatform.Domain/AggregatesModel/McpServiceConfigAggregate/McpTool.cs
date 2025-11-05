using Verdure.McpPlatform.Domain.SeedWork;

namespace Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;

/// <summary>
/// MCP Tool entity - represents a tool available in an MCP service
/// </summary>
public class McpTool : Entity
{
    public string Name { get; private set; }
    public string McpServiceConfigId { get; private set; }
    public string UserId { get; private set; }
    public string? Description { get; private set; }
    public string? InputSchema { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected McpTool()
    {
        Name = string.Empty;
        McpServiceConfigId = string.Empty;
        UserId = string.Empty;
    }

    public McpTool(string name, string mcpServiceConfigId, string userId, string? description = null, string? inputSchema = null)
    {
        GenerateId(); // Generate Guid Version 7 ID
        Name = name ?? throw new ArgumentNullException(nameof(name));
        McpServiceConfigId = mcpServiceConfigId ?? throw new ArgumentNullException(nameof(mcpServiceConfigId));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Description = description;
        InputSchema = inputSchema;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateInfo(string name, string userId, string? description = null, string? inputSchema = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Description = description;
        InputSchema = inputSchema;
        UpdatedAt = DateTime.UtcNow;
    }
}
