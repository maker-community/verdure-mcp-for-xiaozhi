using System.Text.Json;
using StackExchange.Redis;

namespace Verdure.McpPlatform.Api.Services.ConnectionState;

/// <summary>
/// Redis-based connection state service implementation
/// </summary>
public class RedisConnectionStateService : IConnectionStateService
{
    private const string ConnectionStateKeyPrefix = "mcp:connection:";
    private const string AllConnectionsKey = "mcp:connections:all";
    
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisConnectionStateService> _logger;
    private readonly string _instanceId;

    public RedisConnectionStateService(
        IConnectionMultiplexer redis,
        ILogger<RedisConnectionStateService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = _redis.GetDatabase();
        
        // Generate unique instance ID (could also use hostname + process ID)
        _instanceId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
        
        _logger.LogInformation("Connection state service initialized with instance ID: {InstanceId}", _instanceId);
    }

    public async Task RegisterConnectionAsync(
        string serverId,
        string serverName,
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionState = new ConnectionStateInfo
            {
                ServerId = serverId,
                InstanceId = _instanceId,
                Status = ConnectionStatus.Connected,
                LastConnectedTime = DateTime.UtcNow,
                LastHeartbeat = DateTime.UtcNow,
                ServerName = serverName,
                WebSocketEndpoint = endpoint,
                ReconnectAttempts = 0
            };

            var key = GetConnectionKey(serverId);
            var json = JsonSerializer.Serialize(connectionState);

            await _database.StringSetAsync(key, json);
            await _database.SetAddAsync(AllConnectionsKey, serverId);

            _logger.LogInformation(
                "Registered connection for server {ServerId} on instance {InstanceId}",
                serverId,
                _instanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering connection for server {ServerId}", serverId);
            throw;
        }
    }

    public async Task UnregisterConnectionAsync(string serverId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetConnectionKey(serverId);
            await _database.KeyDeleteAsync(key);
            await _database.SetRemoveAsync(AllConnectionsKey, serverId);

            _logger.LogInformation("Unregistered connection for server {ServerId}", serverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering connection for server {ServerId}", serverId);
            throw;
        }
    }

    public async Task UpdateConnectionStatusAsync(
        string serverId,
        ConnectionStatus status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await GetConnectionStateAsync(serverId, cancellationToken);
            if (state == null)
            {
                _logger.LogWarning("Connection state not found for server {ServerId}", serverId);
                return;
            }

            state.Status = status;
            state.LastHeartbeat = DateTime.UtcNow;

            if (status == ConnectionStatus.Connected)
            {
                state.LastConnectedTime = DateTime.UtcNow;
            }
            else if (status == ConnectionStatus.Disconnected || status == ConnectionStatus.Failed)
            {
                state.LastDisconnectedTime = DateTime.UtcNow;
            }

            var key = GetConnectionKey(serverId);
            var json = JsonSerializer.Serialize(state);
            await _database.StringSetAsync(key, json);

            _logger.LogDebug(
                "Updated connection status for server {ServerId} to {Status}",
                serverId,
                status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating connection status for server {ServerId}", serverId);
            throw;
        }
    }

    public async Task UpdateHeartbeatAsync(string serverId, CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await GetConnectionStateAsync(serverId, cancellationToken);
            if (state == null)
            {
                return;
            }

            state.LastHeartbeat = DateTime.UtcNow;

            var key = GetConnectionKey(serverId);
            var json = JsonSerializer.Serialize(state);
            await _database.StringSetAsync(key, json);

            _logger.LogTrace("Updated heartbeat for server {ServerId}", serverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating heartbeat for server {ServerId}", serverId);
        }
    }

    public async Task<ConnectionStateInfo?> GetConnectionStateAsync(
        string serverId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetConnectionKey(serverId);
            var json = await _database.StringGetAsync(key);

            if (json.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<ConnectionStateInfo>(json!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection state for server {ServerId}", serverId);
            return null;
        }
    }

    public async Task<List<ConnectionStateInfo>> GetAllConnectionStatesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var serverIds = await _database.SetMembersAsync(AllConnectionsKey);
            var states = new List<ConnectionStateInfo>();

            foreach (var serverId in serverIds)
            {
                var state = await GetConnectionStateAsync(serverId.ToString(), cancellationToken);
                if (state != null)
                {
                    states.Add(state);
                }
            }

            return states;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all connection states");
            return new List<ConnectionStateInfo>();
        }
    }

    public async Task<List<ConnectionStateInfo>> GetStaleConnectionsAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allStates = await GetAllConnectionStatesAsync(cancellationToken);
            var cutoffTime = DateTime.UtcNow - timeout;

            return allStates
                .Where(s => s.LastHeartbeat < cutoffTime)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stale connections");
            return new List<ConnectionStateInfo>();
        }
    }

    public async Task<bool> IsOwnedByThisInstanceAsync(
        string serverId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await GetConnectionStateAsync(serverId, cancellationToken);
            return state?.InstanceId == _instanceId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking ownership for server {ServerId}", serverId);
            return false;
        }
    }

    public async Task IncrementReconnectAttemptsAsync(
        string serverId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await GetConnectionStateAsync(serverId, cancellationToken);
            if (state == null)
            {
                return;
            }

            state.ReconnectAttempts++;
            state.LastHeartbeat = DateTime.UtcNow;

            var key = GetConnectionKey(serverId);
            var json = JsonSerializer.Serialize(state);
            await _database.StringSetAsync(key, json);

            _logger.LogDebug(
                "Incremented reconnect attempts for server {ServerId} to {Attempts}",
                serverId,
                state.ReconnectAttempts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing reconnect attempts for server {ServerId}", serverId);
        }
    }

    public async Task ResetReconnectAttemptsAsync(
        string serverId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await GetConnectionStateAsync(serverId, cancellationToken);
            if (state == null)
            {
                return;
            }

            state.ReconnectAttempts = 0;
            state.LastHeartbeat = DateTime.UtcNow;

            var key = GetConnectionKey(serverId);
            var json = JsonSerializer.Serialize(state);
            await _database.StringSetAsync(key, json);

            _logger.LogDebug("Reset reconnect attempts for server {ServerId}", serverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting reconnect attempts for server {ServerId}", serverId);
        }
    }

    private static string GetConnectionKey(string serverId)
    {
        return $"{ConnectionStateKeyPrefix}{serverId}";
    }
}
