using System.Collections.Concurrent;
using Verdure.McpPlatform.Api.Services.ConnectionState;
using Verdure.McpPlatform.Api.Services.DistributedLock;
using Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiMcpEndpointAggregate;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;

namespace Verdure.McpPlatform.Api.Services.WebSocket;

/// <summary>
/// Manages multiple MCP sessions (WebSocket connections)
/// Provides session lifecycle management and monitoring
/// Supports distributed deployment with Redis-based coordination
/// </summary>
public class McpSessionManager : IAsyncDisposable
{
    private readonly ILogger<McpSessionManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IDistributedLockService _lockService;
    private readonly IConnectionStateService _connectionStateService;
    private readonly ReconnectionSettings _reconnectionSettings;
    
    private readonly ConcurrentDictionary<string, McpSessionService> _sessions = new();
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    
    public McpSessionManager(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory serviceScopeFactory,
        IDistributedLockService lockService,
        IConnectionStateService connectionStateService)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));
        _connectionStateService = connectionStateService ?? throw new ArgumentNullException(nameof(connectionStateService));
        _logger = loggerFactory.CreateLogger<McpSessionManager>();
        
        // Default reconnection settings
        _reconnectionSettings = new ReconnectionSettings
        {
            Enabled = true,
            MaxAttempts = 10,
            InitialBackoffMs = 1000,
            MaxBackoffMs = 30000
        };
    }

    /// <summary>
    /// Start a session for a specific server
    /// Uses distributed lock to ensure only one instance creates the connection
    /// </summary>
    public async Task<bool> StartSessionAsync(string serverId, CancellationToken cancellationToken = default)
    {
        await _sessionLock.WaitAsync(cancellationToken);
        try
        {
            // Check if session already exists locally
            if (_sessions.ContainsKey(serverId))
            {
                _logger.LogWarning("Session for server {ServerId} already exists locally", serverId);
                return false;
            }

            // Check if another instance already owns this connection
            var existingState = await _connectionStateService.GetConnectionStateAsync(serverId, cancellationToken);
            if (existingState != null && existingState.Status == ConnectionStatus.Connected)
            {
                _logger.LogInformation(
                    "Server {ServerId} is already connected on instance {InstanceId}",
                    serverId,
                    existingState.InstanceId);
                return false;
            }

            // Try to acquire distributed lock for this connection
            var lockKey = $"mcp:lock:connection:{serverId}";
            await using var lockHandle = await _lockService.AcquireLockAsync(
                lockKey,
                expiryTime: TimeSpan.FromMinutes(5),
                waitTime: TimeSpan.FromSeconds(10),
                retryTime: TimeSpan.FromSeconds(1),
                cancellationToken);

            if (lockHandle == null || !lockHandle.IsAcquired)
            {
                _logger.LogWarning(
                    "Failed to acquire distributed lock for server {ServerId}, another instance may be starting it",
                    serverId);
                return false;
            }

            _logger.LogInformation("Acquired distributed lock for server {ServerId}", serverId);

            // Double-check connection state after acquiring lock
            existingState = await _connectionStateService.GetConnectionStateAsync(serverId, cancellationToken);
            if (existingState != null && existingState.Status == ConnectionStatus.Connected)
            {
                _logger.LogInformation(
                    "Server {ServerId} was connected by another instance while waiting for lock",
                    serverId);
                return false;
            }

            // Get server configuration using a scoped service
            using var scope = _serviceScopeFactory.CreateScope();
            var serverRepository = scope.ServiceProvider.GetRequiredService<IXiaozhiMcpEndpointRepository>();
            var configRepository = scope.ServiceProvider.GetRequiredService<IMcpServiceConfigRepository>();
            
            var server = await serverRepository.GetAsync(serverId);
            if (server == null)
            {
                _logger.LogError("Server {ServerId} not found", serverId);
                return false;
            }

            if (!server.IsEnabled)
            {
                _logger.LogWarning("Server {ServerId} is not enabled", serverId);
                return false;
            }

            // Build session configuration with McpServiceConfig lookup
            var mcpServiceEndpoints = new List<McpServiceEndpoint>();
            foreach (var binding in server.ServiceBindings.Where(b => b.IsActive))
            {
                var serviceConfig = await configRepository.GetByIdAsync(binding.McpServiceConfigId);
                if (serviceConfig != null)
                {
                    mcpServiceEndpoints.Add(new McpServiceEndpoint
                    {
                        BindingId = binding.Id,
                        ServiceName = serviceConfig.Name,
                        NodeAddress = serviceConfig.Endpoint,
                        SelectedToolNames = binding.SelectedToolNames.ToList(),
                        // ✅ Pass authentication configuration from McpServiceConfig
                        AuthenticationType = serviceConfig.AuthenticationType,
                        AuthenticationConfig = serviceConfig.AuthenticationConfig,
                        Protocol = serviceConfig.Protocol
                    });
                    
                    _logger.LogDebug(
                        "Added service endpoint {ServiceName} with authentication: {AuthType}",
                        serviceConfig.Name,
                        serviceConfig.AuthenticationType ?? "none");
                }
                else
                {
                    _logger.LogWarning("McpServiceConfig {ConfigId} not found for binding {BindingId}", 
                        binding.McpServiceConfigId, binding.Id);
                }
            }

            var config = new McpSessionConfiguration
            {
                ServerId = server.Id,
                ServerName = server.Name,
                WebSocketEndpoint = server.Address,
                McpServices = mcpServiceEndpoints
            };

            // Create new session
            var session = new McpSessionService(
                config,
                _reconnectionSettings,
                _loggerFactory);

            // Add to dictionary
            if (!_sessions.TryAdd(serverId, session))
            {
                _logger.LogError("Failed to add session for server {ServerId}", serverId);
                return false;
            }

            _logger.LogInformation("Created session for server {ServerId} ({ServerName})", 
                server.Id, server.Name);

            // Register connection in Redis
            await _connectionStateService.RegisterConnectionAsync(
                serverId,
                server.Name,
                server.Address,
                cancellationToken);

            // Start session in background
            _ = Task.Run(async () =>
            {
                try
                {
                    // Update connection state to Connecting
                    await _connectionStateService.UpdateConnectionStatusAsync(
                        serverId,
                        ConnectionStatus.Connecting,
                        CancellationToken.None);

                    // Update server status to connected
                    using var backgroundScope = _serviceScopeFactory.CreateScope();
                    var backgroundRepository = backgroundScope.ServiceProvider.GetRequiredService<IXiaozhiMcpEndpointRepository>();
                    
                    var serverToUpdate = await backgroundRepository.GetAsync(serverId);
                    if (serverToUpdate != null)
                    {
                        serverToUpdate.SetConnected();
                        await backgroundRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
                    }

                    // Update connection state to Connected
                    await _connectionStateService.UpdateConnectionStatusAsync(
                        serverId,
                        ConnectionStatus.Connected,
                        CancellationToken.None);

                    // Reset reconnect attempts on successful connection
                    await _connectionStateService.ResetReconnectAttemptsAsync(
                        serverId,
                        CancellationToken.None);
                    
                    await session.StartAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Session for server {ServerId} failed", serverId);
                    
                    // Update connection state to Failed
                    await _connectionStateService.UpdateConnectionStatusAsync(
                        serverId,
                        ConnectionStatus.Failed,
                        CancellationToken.None);

                    // Update server status to disconnected
                    try
                    {
                        using var errorScope = _serviceScopeFactory.CreateScope();
                        var errorRepository = errorScope.ServiceProvider.GetRequiredService<IXiaozhiMcpEndpointRepository>();
                        
                        var serverToUpdate = await errorRepository.GetAsync(serverId);
                        if (serverToUpdate != null)
                        {
                            serverToUpdate.SetDisconnected();
                            await errorRepository.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
                        }
                    }
                    catch (Exception updateEx)
                    {
                        _logger.LogError(updateEx, "Failed to update server status for {ServerId}", serverId);
                    }
                    
                    // Remove failed session
                    _sessions.TryRemove(serverId, out _);
                    
                    // Unregister from Redis
                    await _connectionStateService.UnregisterConnectionAsync(serverId, CancellationToken.None);
                }
            }, cancellationToken);

            return true;
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    /// <summary>
    /// Stop a specific session
    /// Unregisters from Redis connection state
    /// </summary>
    public async Task<bool> StopSessionAsync(string serverId)
    {
        // Check if this instance owns the connection
        var isOwned = await _connectionStateService.IsOwnedByThisInstanceAsync(serverId);
        if (!isOwned)
        {
            _logger.LogWarning(
                "Cannot stop session for server {ServerId} - not owned by this instance",
                serverId);
        }

        if (!_sessions.TryRemove(serverId, out var session))
        {
            _logger.LogWarning("Session for server {ServerId} not found locally", serverId);
            
            // Still try to unregister from Redis if owned
            if (isOwned)
            {
                await _connectionStateService.UnregisterConnectionAsync(serverId);
            }
            return false;
        }

        _logger.LogInformation("Stopping session for server {ServerId}", serverId);
        
        try
        {
            await session.StopAsync();
            await session.DisposeAsync();
            
            // Update server status using a scoped service
            using var scope = _serviceScopeFactory.CreateScope();
            var serverRepository = scope.ServiceProvider.GetRequiredService<IXiaozhiMcpEndpointRepository>();
            
            var server = await serverRepository.GetAsync(serverId);
            if (server != null)
            {
                server.SetDisconnected();
                await serverRepository.UnitOfWork.SaveEntitiesAsync();
            }

            // Unregister from Redis
            await _connectionStateService.UnregisterConnectionAsync(serverId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping session for server {ServerId}", serverId);
            return false;
        }
    }

    /// <summary>
    /// Restart a session (useful when bindings are updated)
    /// </summary>
    public async Task<bool> RestartSessionAsync(string serverId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restarting session for server {ServerId}", serverId);
        
        await StopSessionAsync(serverId);
        await Task.Delay(500, cancellationToken); // Brief delay
        return await StartSessionAsync(serverId, cancellationToken);
    }

    /// <summary>
    /// Stop all sessions
    /// </summary>
    public async Task StopAllSessionsAsync()
    {
        _logger.LogInformation("Stopping all sessions ({Count})", _sessions.Count);

        var stopTasks = _sessions.Keys.Select(serverId => StopSessionAsync(serverId));
        await Task.WhenAll(stopTasks);

        _logger.LogInformation("All sessions stopped");
    }

    /// <summary>
    /// Get a specific session
    /// </summary>
    public McpSessionService? GetSession(string serverId)
    {
        _sessions.TryGetValue(serverId, out var session);
        return session;
    }

    /// <summary>
    /// Check if a session exists and is connected
    /// </summary>
    public bool IsSessionConnected(string serverId)
    {
        return _sessions.TryGetValue(serverId, out var session) && session.IsConnected;
    }

    /// <summary>
    /// Get all active sessions
    /// </summary>
    public IReadOnlyDictionary<string, McpSessionService> GetAllSessions()
    {
        return _sessions;
    }

    /// <summary>
    /// Get session statistics
    /// </summary>
    public SessionStatistics GetStatistics()
    {
        var sessions = _sessions.Values.ToList();
        
        return new SessionStatistics
        {
            TotalSessions = sessions.Count,
            ConnectedSessions = sessions.Count(s => s.IsConnected),
            DisconnectedSessions = sessions.Count(s => !s.IsConnected),
            TotalReconnectAttempts = sessions.Sum(s => s.ReconnectAttempts),
            Sessions = sessions.Select(s => new SessionInfo
            {
                ServerId = s.ServerId,
                ServerName = s.ServerName,
                IsConnected = s.IsConnected,
                LastConnectedTime = s.LastConnectedTime,
                LastDisconnectedTime = s.LastDisconnectedTime,
                ReconnectAttempts = s.ReconnectAttempts
            }).ToList()
        };
    }

    public async ValueTask DisposeAsync()
    {
        await StopAllSessionsAsync();
        _sessionLock.Dispose();
    }
}

/// <summary>
/// Statistics about all sessions
/// </summary>
public class SessionStatistics
{
    public int TotalSessions { get; set; }
    public int ConnectedSessions { get; set; }
    public int DisconnectedSessions { get; set; }
    public int TotalReconnectAttempts { get; set; }
    public List<SessionInfo> Sessions { get; set; } = new();
}

/// <summary>
/// Information about a single session
/// </summary>
public class SessionInfo
{
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public DateTime? LastConnectedTime { get; set; }
    public DateTime? LastDisconnectedTime { get; set; }
    public int ReconnectAttempts { get; set; }
}
