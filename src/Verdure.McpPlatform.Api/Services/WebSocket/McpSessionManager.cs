using System.Collections.Concurrent;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServerAggregate;

namespace Verdure.McpPlatform.Api.Services.WebSocket;

/// <summary>
/// Manages multiple MCP sessions (WebSocket connections)
/// Provides session lifecycle management and monitoring
/// </summary>
public class McpSessionManager : IAsyncDisposable
{
    private readonly ILogger<McpSessionManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMcpServerRepository _serverRepository;
    private readonly ReconnectionSettings _reconnectionSettings;
    
    private readonly ConcurrentDictionary<int, McpSessionService> _sessions = new();
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    
    public McpSessionManager(
        ILoggerFactory loggerFactory,
        IMcpServerRepository serverRepository)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _serverRepository = serverRepository ?? throw new ArgumentNullException(nameof(serverRepository));
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
    /// </summary>
    public async Task<bool> StartSessionAsync(int serverId, CancellationToken cancellationToken = default)
    {
        await _sessionLock.WaitAsync(cancellationToken);
        try
        {
            // Check if session already exists
            if (_sessions.ContainsKey(serverId))
            {
                _logger.LogWarning("Session for server {ServerId} already exists", serverId);
                return false;
            }

            // Get server configuration
            var server = await _serverRepository.GetAsync(serverId);
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

            // Build session configuration
            var config = new McpSessionConfiguration
            {
                ServerId = server.Id,
                ServerName = server.Name,
                WebSocketEndpoint = server.Address,
                McpServices = server.Bindings
                    .Where(b => b.IsActive)
                    .Select(b => new McpServiceEndpoint
                    {
                        BindingId = b.Id,
                        ServiceName = b.ServiceName,
                        NodeAddress = b.NodeAddress
                    })
                    .ToList()
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

            // Start session in background
            _ = Task.Run(async () =>
            {
                try
                {
                    server.SetConnected();
                    await _serverRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
                    
                    await session.StartAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Session for server {ServerId} failed", serverId);
                    
                    // Update server status
                    server.SetDisconnected();
                    await _serverRepository.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
                    
                    // Remove failed session
                    _sessions.TryRemove(serverId, out _);
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
    /// </summary>
    public async Task<bool> StopSessionAsync(int serverId)
    {
        if (!_sessions.TryRemove(serverId, out var session))
        {
            _logger.LogWarning("Session for server {ServerId} not found", serverId);
            return false;
        }

        _logger.LogInformation("Stopping session for server {ServerId}", serverId);
        
        try
        {
            await session.StopAsync();
            await session.DisposeAsync();
            
            // Update server status
            var server = await _serverRepository.GetAsync(serverId);
            if (server != null)
            {
                server.SetDisconnected();
                await _serverRepository.UnitOfWork.SaveEntitiesAsync();
            }
            
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
    public async Task<bool> RestartSessionAsync(int serverId, CancellationToken cancellationToken = default)
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
    public McpSessionService? GetSession(int serverId)
    {
        _sessions.TryGetValue(serverId, out var session);
        return session;
    }

    /// <summary>
    /// Check if a session exists and is connected
    /// </summary>
    public bool IsSessionConnected(int serverId)
    {
        return _sessions.TryGetValue(serverId, out var session) && session.IsConnected;
    }

    /// <summary>
    /// Get all active sessions
    /// </summary>
    public IReadOnlyDictionary<int, McpSessionService> GetAllSessions()
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
    public int ServerId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public DateTime? LastConnectedTime { get; set; }
    public DateTime? LastDisconnectedTime { get; set; }
    public int ReconnectAttempts { get; set; }
}
