using Verdure.McpPlatform.Domain.SeedWork;

namespace Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;

/// <summary>
/// MCP Service Configuration aggregate root - represents a configured MCP service with its tools
/// This is independent from XiaozhiMcpEndpoint and represents the actual MCP service node
/// </summary>
public class McpServiceConfig : Entity, IAggregateRoot
{
    public string Name { get; private set; }
    public string Endpoint { get; private set; }
    public string UserId { get; private set; }
    public string? Description { get; private set; }
    public bool IsPublic { get; private set; }
    public string? AuthenticationType { get; private set; }
    public string? AuthenticationConfig { get; private set; }
    public string? Protocol { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastSyncedAt { get; private set; }

    private readonly List<McpTool> _tools;
    public IReadOnlyCollection<McpTool> Tools => _tools.AsReadOnly();

    protected McpServiceConfig()
    {
        _tools = new List<McpTool>();
        Name = string.Empty;
        Endpoint = string.Empty;
        UserId = string.Empty;
    }

    public McpServiceConfig(
        string name, 
        string endpoint, 
        string userId, 
        string? description = null,
        string? authenticationType = null,
        string? authenticationConfig = null,
        string? protocol = null) : this()
    {
        GenerateId(); // Generate Guid Version 7 ID
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Description = description;
        IsPublic = false; // Default to private
        AuthenticationType = authenticationType;
        AuthenticationConfig = authenticationConfig;
        Protocol = protocol ?? "stdio"; // Default to stdio
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateInfo(
        string name, 
        string endpoint, 
        string? description = null,
        string? authenticationType = null,
        string? authenticationConfig = null,
        string? protocol = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        Description = description;
        // IsPublic is not updated here - use SetPublic/SetPrivate methods instead
        AuthenticationType = authenticationType;
        AuthenticationConfig = authenticationConfig;
        Protocol = protocol;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTools(IEnumerable<McpTool> tools)
    {
        _tools.Clear();
        _tools.AddRange(tools);
        LastSyncedAt = DateTime.UtcNow;
    }

    public McpTool AddTool(string name, string? description, string? inputSchema)
    {
        var tool = new McpTool(name, Id, UserId, description, inputSchema);
        _tools.Add(tool);
        return tool;
    }

    public void RemoveTool(McpTool tool)
    {
        _tools.Remove(tool);
    }

    public void SetPublic()
    {
        IsPublic = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPrivate()
    {
        IsPublic = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update only the authentication configuration (used when refreshing OAuth tokens)
    /// </summary>
    public void UpdateAuthenticationConfig(string authenticationConfig)
    {
        AuthenticationConfig = authenticationConfig;
        UpdatedAt = DateTime.UtcNow;
    }
}
