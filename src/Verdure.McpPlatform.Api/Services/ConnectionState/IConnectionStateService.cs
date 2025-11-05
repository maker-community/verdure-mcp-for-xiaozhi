namespace Verdure.McpPlatform.Api.Services.ConnectionState;

/// <summary>
/// Connection state information stored in Redis
/// </summary>
public class ConnectionStateInfo
{
    /// <summary>
    /// Server ID
    /// </summary>
    public string ServerId { get; set; } = string.Empty;

    /// <summary>
    /// Instance ID that owns this connection
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// Connection status
    /// </summary>
    public ConnectionStatus Status { get; set; }

    /// <summary>
    /// Last connected time (UTC)
    /// </summary>
    public DateTime? LastConnectedTime { get; set; }

    /// <summary>
    /// Last disconnected time (UTC)
    /// </summary>
    public DateTime? LastDisconnectedTime { get; set; }

    /// <summary>
    /// Last heartbeat time (UTC)
    /// </summary>
    public DateTime LastHeartbeat { get; set; }

    /// <summary>
    /// Reconnect attempts count
    /// </summary>
    public int ReconnectAttempts { get; set; }

    /// <summary>
    /// Server name
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// WebSocket endpoint
    /// </summary>
    public string WebSocketEndpoint { get; set; } = string.Empty;
}

/// <summary>
/// Connection status enum
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// Connection is active
    /// </summary>
    Connected,

    /// <summary>
    /// Connection is disconnected
    /// </summary>
    Disconnected,

    /// <summary>
    /// Connection is being established
    /// </summary>
    Connecting,

    /// <summary>
    /// Connection failed
    /// </summary>
    Failed
}

/// <summary>
/// Service for managing connection states across multiple instances using Redis
/// </summary>
public interface IConnectionStateService
{
    /// <summary>
    /// Register a connection as active
    /// </summary>
    Task RegisterConnectionAsync(string serverId, string serverName, string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregister a connection
    /// </summary>
    Task UnregisterConnectionAsync(string serverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update connection status
    /// </summary>
    Task UpdateConnectionStatusAsync(string serverId, ConnectionStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update heartbeat for a connection
    /// </summary>
    Task UpdateHeartbeatAsync(string serverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get connection state for a specific server
    /// </summary>
    Task<ConnectionStateInfo?> GetConnectionStateAsync(string serverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all connection states
    /// </summary>
    Task<List<ConnectionStateInfo>> GetAllConnectionStatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get stale connections (heartbeat timeout)
    /// </summary>
    Task<List<ConnectionStateInfo>> GetStaleConnectionsAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a connection is owned by this instance
    /// </summary>
    Task<bool> IsOwnedByThisInstanceAsync(string serverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment reconnect attempts
    /// </summary>
    Task IncrementReconnectAttemptsAsync(string serverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset reconnect attempts
    /// </summary>
    Task ResetReconnectAttemptsAsync(string serverId, CancellationToken cancellationToken = default);
}
