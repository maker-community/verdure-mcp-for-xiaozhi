using Verdure.McpPlatform.Api.Services.ConnectionState;
using Verdure.McpPlatform.Api.Services.WebSocket;
using Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiConnectionAggregate;

namespace Verdure.McpPlatform.Api.Services.BackgroundServices;

/// <summary>
/// Background service that monitors connection states and attempts to reconnect failed connections
/// Runs on all instances but uses distributed coordination to avoid duplicate reconnection attempts
/// </summary>
public class ConnectionMonitorHostedService : BackgroundService
{
    private readonly ILogger<ConnectionMonitorHostedService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _heartbeatTimeout;
    private readonly TimeSpan _reconnectCooldown;

    public ConnectionMonitorHostedService(
        ILogger<ConnectionMonitorHostedService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

        // Read configuration with defaults
        _checkInterval = TimeSpan.FromSeconds(
            configuration.GetValue<int>("ConnectionMonitor:CheckIntervalSeconds", 30));
        _heartbeatTimeout = TimeSpan.FromSeconds(
            configuration.GetValue<int>("ConnectionMonitor:HeartbeatTimeoutSeconds", 90));
        _reconnectCooldown = TimeSpan.FromSeconds(
            configuration.GetValue<int>("ConnectionMonitor:ReconnectCooldownSeconds", 60));

        _logger.LogInformation(
            "Connection monitor initialized - CheckInterval: {CheckInterval}, HeartbeatTimeout: {HeartbeatTimeout}, ReconnectCooldown: {ReconnectCooldown}",
            _checkInterval,
            _heartbeatTimeout,
            _reconnectCooldown);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Connection monitor service starting");

        // Wait a bit before starting to allow services to initialize
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        // Check for enabled servers that should be connected on startup
        await CheckAndStartEnabledServersAsync(stoppingToken);

        // Start monitoring loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorConnectionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in connection monitoring cycle");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Service is stopping
                break;
            }
        }

        _logger.LogInformation("Connection monitor service stopping");
    }

    /// <summary>
    /// Check for enabled servers and start connections on service startup
    /// </summary>
    private async Task CheckAndStartEnabledServersAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Checking for enabled servers to connect on startup");

            using var scope = _serviceScopeFactory.CreateScope();
            var serverRepository = scope.ServiceProvider.GetRequiredService<IXiaozhiConnectionRepository>();
            var connectionStateService = scope.ServiceProvider.GetRequiredService<IConnectionStateService>();
            var sessionManager = scope.ServiceProvider.GetRequiredService<McpSessionManager>();

            // Get all enabled servers from database
            var enabledServers = await serverRepository.GetEnabledServersAsync(cancellationToken);

            _logger.LogInformation("Found {Count} enabled servers", enabledServers.Count());

            foreach (var server in enabledServers)
            {
                try
                {
                    // Check if server is already connected by any instance
                    var connectionState = await connectionStateService.GetConnectionStateAsync(
                        server.Id,
                        cancellationToken);

                    if (connectionState != null && connectionState.Status == ConnectionStatus.Connected)
                    {
                        _logger.LogInformation(
                            "Server {ServerId} ({ServerName}) is already connected on instance {InstanceId}",
                            server.Id,
                            server.Name,
                            connectionState.InstanceId);
                        continue;
                    }

                    // Try to start connection
                    _logger.LogInformation(
                        "Attempting to start connection for enabled server {ServerId} ({ServerName})",
                        server.Id,
                        server.Name);

                    var started = await sessionManager.StartSessionAsync(server.Id, cancellationToken);

                    if (started)
                    {
                        _logger.LogInformation(
                            "Successfully started connection for server {ServerId} ({ServerName})",
                            server.Id,
                            server.Name);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to start connection for server {ServerId} ({ServerName}) - may be handled by another instance",
                            server.Id,
                            server.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error starting connection for server {ServerId} ({ServerName}) on startup",
                        server.Id,
                        server.Name);
                }

                // Small delay between startup connections
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }

            _logger.LogInformation("Completed startup connection checks");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking enabled servers on startup");
        }
    }

    /// <summary>
    /// Monitor existing connections and attempt to reconnect failed ones
    /// </summary>
    private async Task MonitorConnectionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var connectionStateService = scope.ServiceProvider.GetRequiredService<IConnectionStateService>();
        var sessionManager = scope.ServiceProvider.GetRequiredService<McpSessionManager>();
        var serverRepository = scope.ServiceProvider.GetRequiredService<IXiaozhiConnectionRepository>();

        // Update heartbeats for local connections
        await UpdateLocalConnectionHeartbeatsAsync(sessionManager, connectionStateService, cancellationToken);

        // Get stale connections (no heartbeat for timeout period)
        var staleConnections = await connectionStateService.GetStaleConnectionsAsync(
            _heartbeatTimeout,
            cancellationToken);

        if (staleConnections.Any())
        {
            _logger.LogWarning("Found {Count} stale connections", staleConnections.Count);

            foreach (var staleConnection in staleConnections)
            {
                try
                {
                    _logger.LogWarning(
                        "Stale connection detected for server {ServerId} ({ServerName}) on instance {InstanceId}, last heartbeat: {LastHeartbeat}",
                        staleConnection.ServerId,
                        staleConnection.ServerName,
                        staleConnection.InstanceId,
                        staleConnection.LastHeartbeat);

                    // Clean up stale connection state
                    await connectionStateService.UnregisterConnectionAsync(
                        staleConnection.ServerId,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error cleaning up stale connection for server {ServerId}",
                        staleConnection.ServerId);
                }
            }
        }

        // Check for disconnected servers that should be reconnected
        var allStates = await connectionStateService.GetAllConnectionStatesAsync(cancellationToken);
        var disconnectedStates = allStates.Where(
            s => s.Status == ConnectionStatus.Disconnected || s.Status == ConnectionStatus.Failed).ToList();

        foreach (var disconnectedState in disconnectedStates)
        {
            try
            {
                // Check if enough time has passed since last disconnect (cooldown)
                var timeSinceDisconnect = DateTime.UtcNow - (disconnectedState.LastDisconnectedTime ?? DateTime.MinValue);
                if (timeSinceDisconnect < _reconnectCooldown)
                {
                    _logger.LogDebug(
                        "Skipping reconnect for server {ServerId} - cooldown period not elapsed ({TimeRemaining}s remaining)",
                        disconnectedState.ServerId,
                        (_reconnectCooldown - timeSinceDisconnect).TotalSeconds);
                    continue;
                }

                // Verify server is still enabled
                var server = await serverRepository.GetAsync(disconnectedState.ServerId);
                if (server == null || !server.IsEnabled)
                {
                    _logger.LogInformation(
                        "Server {ServerId} is disabled or not found, removing from connection state",
                        disconnectedState.ServerId);
                    await connectionStateService.UnregisterConnectionAsync(
                        disconnectedState.ServerId,
                        cancellationToken);
                    continue;
                }

                // Attempt reconnection
                _logger.LogInformation(
                    "Attempting to reconnect server {ServerId} ({ServerName}), attempt #{Attempts}",
                    disconnectedState.ServerId,
                    disconnectedState.ServerName,
                    disconnectedState.ReconnectAttempts + 1);

                await connectionStateService.IncrementReconnectAttemptsAsync(
                    disconnectedState.ServerId,
                    cancellationToken);

                var reconnected = await sessionManager.StartSessionAsync(
                    disconnectedState.ServerId,
                    cancellationToken);

                if (reconnected)
                {
                    _logger.LogInformation(
                        "Successfully reconnected server {ServerId} ({ServerName})",
                        disconnectedState.ServerId,
                        disconnectedState.ServerName);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to reconnect server {ServerId} ({ServerName}) - may be handled by another instance",
                        disconnectedState.ServerId,
                        disconnectedState.ServerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error attempting to reconnect server {ServerId}",
                    disconnectedState.ServerId);
            }
        }
    }

    /// <summary>
    /// Update heartbeats for all locally managed connections
    /// </summary>
    private async Task UpdateLocalConnectionHeartbeatsAsync(
        McpSessionManager sessionManager,
        IConnectionStateService connectionStateService,
        CancellationToken cancellationToken)
    {
        try
        {
            var localSessions = sessionManager.GetAllSessions();

            foreach (var session in localSessions)
            {
                if (session.Value.IsConnected)
                {
                    await connectionStateService.UpdateHeartbeatAsync(
                        session.Key,
                        cancellationToken);
                }
            }

            if (localSessions.Count > 0)
            {
                _logger.LogTrace(
                    "Updated heartbeats for {Count} local connections",
                    localSessions.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating local connection heartbeats");
        }
    }
}
