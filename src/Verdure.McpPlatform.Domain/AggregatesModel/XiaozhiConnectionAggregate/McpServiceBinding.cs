using Verdure.McpPlatform.Domain.SeedWork;

namespace Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiConnectionAggregate;

/// <summary>
/// MCP Service Binding entity - represents an external MCP service that is bound to a Xiaozhi connection
/// Each binding configures one external MCP service node that will be injected into Xiaozhi AI
/// </summary>
public class McpServiceBinding : Entity
{
    public string ServiceName { get; private set; }
    public string NodeAddress { get; private set; }
    public int XiaozhiConnectionId { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected McpServiceBinding()
    {
        ServiceName = string.Empty;
        NodeAddress = string.Empty;
    }

    public McpServiceBinding(string serviceName, string nodeAddress, int xiaozhiConnectionId, string? description = null)
    {
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        NodeAddress = nodeAddress ?? throw new ArgumentNullException(nameof(nodeAddress));
        XiaozhiConnectionId = xiaozhiConnectionId;
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
