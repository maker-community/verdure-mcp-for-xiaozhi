using Verdure.McpPlatform.Domain.SeedWork;

namespace Verdure.McpPlatform.Domain.AggregatesModel.McpServerAggregate;

/// <summary>
/// MCP Binding entity - represents a binding between an MCP service and a node
/// </summary>
public class McpBinding : Entity
{
    public string ServiceName { get; private set; }
    public string NodeAddress { get; private set; }
    public int McpServerId { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected McpBinding()
    {
        ServiceName = string.Empty;
        NodeAddress = string.Empty;
    }

    public McpBinding(string serviceName, string nodeAddress, int mcpServerId, string? description = null)
    {
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        NodeAddress = nodeAddress ?? throw new ArgumentNullException(nameof(nodeAddress));
        McpServerId = mcpServerId;
        Description = description;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateInfo(string serviceName, string nodeAddress, string? description = null)
    {
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        NodeAddress = nodeAddress ?? throw new ArgumentNullException(nameof(nodeAddress));
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
