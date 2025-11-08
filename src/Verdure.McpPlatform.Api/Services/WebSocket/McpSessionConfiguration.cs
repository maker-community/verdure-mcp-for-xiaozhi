namespace Verdure.McpPlatform.Api.Services.WebSocket;

/// <summary>
/// Configuration for MCP session
/// </summary>
public class McpSessionConfiguration
{
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string WebSocketEndpoint { get; set; } = string.Empty;
    public List<McpServiceEndpoint> McpServices { get; set; } = new();
}

/// <summary>
/// MCP service endpoint configuration
/// Includes authentication information for connecting to the MCP service
/// </summary>
public class McpServiceEndpoint
{
    public string BindingId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string NodeAddress { get; set; } = string.Empty;
    public List<string> SelectedToolNames { get; set; } = new();
    
    /// <summary>
    /// Authentication type (bearer, basic, apikey, oauth2)
    /// </summary>
    public string? AuthenticationType { get; set; }
    
    /// <summary>
    /// Authentication configuration JSON
    /// </summary>
    public string? AuthenticationConfig { get; set; }
    
    /// <summary>
    /// Protocol type (stdio, streamable-http, http, sse)
    /// </summary>
    public string? Protocol { get; set; }
}

/// <summary>
/// Reconnection settings for WebSocket
/// </summary>
public class ReconnectionSettings
{
    public bool Enabled { get; set; } = true;
    public int MaxAttempts { get; set; } = 10;
    public int InitialBackoffMs { get; set; } = 1000;
    public int MaxBackoffMs { get; set; } = 30000;
}
