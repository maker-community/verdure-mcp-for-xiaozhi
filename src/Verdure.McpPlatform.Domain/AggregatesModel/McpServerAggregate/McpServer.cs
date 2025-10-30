using Verdure.McpPlatform.Domain.SeedWork;

namespace Verdure.McpPlatform.Domain.AggregatesModel.McpServerAggregate;

/// <summary>
/// MCP Server aggregate root - represents a user's MCP server configuration
/// </summary>
public class McpServer : Entity, IAggregateRoot
{
    public string Name { get; private set; }
    public string Address { get; private set; }
    public string UserId { get; private set; }
    public string? Description { get; private set; }
    public bool IsEnabled { get; private set; }
    public bool IsConnected { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastConnectedAt { get; private set; }
    public DateTime? LastDisconnectedAt { get; private set; }

    private readonly List<McpBinding> _bindings;
    public IReadOnlyCollection<McpBinding> Bindings => _bindings.AsReadOnly();

    protected McpServer()
    {
        _bindings = new List<McpBinding>();
        Name = string.Empty;
        Address = string.Empty;
        UserId = string.Empty;
    }

    public McpServer(string name, string address, string userId, string? description = null) : this()
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Address = address ?? throw new ArgumentNullException(nameof(address));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Description = description;
        IsEnabled = false; // Disabled by default until user enables
        IsConnected = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateInfo(string name, string address, string? description = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Address = address ?? throw new ArgumentNullException(nameof(address));
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public McpBinding AddBinding(string serviceName, string nodeAddress, string? description = null)
    {
        var binding = new McpBinding(serviceName, nodeAddress, Id, description);
        _bindings.Add(binding);
        return binding;
    }

    public void RemoveBinding(McpBinding binding)
    {
        _bindings.Remove(binding);
    }

    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        IsConnected = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetConnected()
    {
        IsConnected = true;
        LastConnectedAt = DateTime.UtcNow;
    }

    public void SetDisconnected()
    {
        IsConnected = false;
        LastDisconnectedAt = DateTime.UtcNow;
    }
}
