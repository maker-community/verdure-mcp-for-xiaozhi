namespace Verdure.McpPlatform.Api.Services.WebSocket;

/// <summary>
/// Configuration for MCP session
/// </summary>
public class McpSessionConfiguration
{
    public int ServerId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string WebSocketEndpoint { get; set; } = string.Empty;
    public List<McpServiceEndpoint> McpServices { get; set; } = new();
}

/// <summary>
/// MCP service endpoint configuration
/// </summary>
public class McpServiceEndpoint
{
    public int BindingId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string NodeAddress { get; set; } = string.Empty;
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
