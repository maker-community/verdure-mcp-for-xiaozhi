using System.Net.WebSockets;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ModelContextProtocol.Client;
using Verdure.McpPlatform.Application.Services;

namespace Verdure.McpPlatform.Api.Services.WebSocket;

/// <summary>
/// Manages a single MCP WebSocket session
/// Each instance handles one MCP server node connection
/// </summary>
public class McpSessionService : IAsyncDisposable
{
    private readonly ILogger<McpSessionService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly McpSessionConfiguration _config;
    private readonly ReconnectionSettings _reconnectionSettings;
    
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    // Reconnection state
    private int _reconnectAttempt = 0;
    private int _currentBackoffMs;
    
    // Session state
    private ClientWebSocket? _webSocket;
    private readonly List<McpClient> _mcpClients = new();
    private bool _isRunning = false;
    
    // ✅ Ping timeout monitoring
    private DateTime _lastPingReceivedTime = DateTime.UtcNow;
    private readonly TimeSpan _pingTimeout = TimeSpan.FromSeconds(90); // 90秒未收到 ping 视为断开
    private readonly object _pingLock = new();
    
    // Connection status events
    public event Func<Task>? OnConnected;
    public event Func<string, Task>? OnConnectionFailed;
    public event Func<Task>? OnDisconnected;
    
    public string ServerId => _config.ServerId;
    public string ServerName => _config.ServerName;
    public bool IsConnected => _webSocket?.State == WebSocketState.Open && _mcpClients.Count > 0 && !IsPingTimeout;
    public int ConnectedClientsCount => _mcpClients.Count;
    public int TotalConfiguredClients => _config.McpServices.Count;
    public DateTime? LastConnectedTime { get; private set; }
    public DateTime? LastDisconnectedTime { get; private set; }
    public DateTime LastPingReceivedTime 
    { 
        get 
        {
            lock (_pingLock)
            {
                return _lastPingReceivedTime;
            }
        }
    }
    public int ReconnectAttempts => _reconnectAttempt;
    
    /// <summary>
    /// Check if ping timeout has occurred
    /// </summary>
    public bool IsPingTimeout
    {
        get
        {
            lock (_pingLock)
            {
                return DateTime.UtcNow - _lastPingReceivedTime > _pingTimeout;
            }
        }
    }

    public McpSessionService(
        McpSessionConfiguration config,
        ReconnectionSettings reconnectionSettings,
        ILoggerFactory loggerFactory)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _reconnectionSettings = reconnectionSettings ?? throw new ArgumentNullException(nameof(reconnectionSettings));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        
        _logger = loggerFactory.CreateLogger<McpSessionService>();
        _currentBackoffMs = reconnectionSettings.InitialBackoffMs;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    /// <summary>
    /// Start the session with automatic reconnection
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("Session for server {ServerId} is already running", ServerId);
            return;
        }

        _isRunning = true;
        _logger.LogInformation("Starting session for server {ServerId} ({ServerName})", ServerId, ServerName);

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);
        
        try
        {
            await ConnectWithRetryAsync(linkedCts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session for server {ServerId} failed to start", ServerId);
            throw;
        }
    }

    /// <summary>
    /// Stop the session gracefully
    /// </summary>
    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping session for server {ServerId}", ServerId);
        _isRunning = false;
        _cancellationTokenSource.Cancel();
        
        await CleanupConnectionAsync();
    }

    /// <summary>
    /// Connect with retry mechanism
    /// </summary>
    private async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
    {
        while (_isRunning && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait before retry (skip on first attempt)
                if (_reconnectAttempt > 0)
                {
                    // Check if we've exceeded max attempts
                    if (_reconnectionSettings.MaxAttempts > 0 && _reconnectAttempt >= _reconnectionSettings.MaxAttempts)
                    {
                        _logger.LogWarning("Session for server {ServerId} reached max reconnection attempts ({MaxAttempts})", 
                            ServerId, _reconnectionSettings.MaxAttempts);
                        break;
                    }

                    var jitter = Random.Shared.NextDouble() * 0.1;
                    var waitTime = (int)(_currentBackoffMs * (1 + jitter));

                    _logger.LogInformation("Server {ServerId}: Waiting {WaitTimeMs}ms before reconnection attempt {AttemptNumber}...",
                        ServerId, waitTime, _reconnectAttempt);

                    await Task.Delay(waitTime, cancellationToken);
                }

                // Attempt to connect
                await ConnectAsync(cancellationToken);

                // If we reach here without exception, connection was closed normally
                if (cancellationToken.IsCancellationRequested || !_isRunning)
                {
                    _logger.LogInformation("Server {ServerId} shutdown requested", ServerId);
                    break;
                }

                _logger.LogInformation("Server {ServerId} connection closed, will retry", ServerId);
                _reconnectAttempt++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Server {ServerId} cancelled", ServerId);
                break;
            }
            catch (Exception ex)
            {
                _reconnectAttempt++;
                _logger.LogWarning("Server {ServerId} connection failed (attempt {AttemptNumber}): {Error}",
                    ServerId, _reconnectAttempt, ex.Message);

                // Exponential backoff
                _currentBackoffMs = Math.Min(_currentBackoffMs * 2, _reconnectionSettings.MaxBackoffMs);
            }
        }
    }

    /// <summary>
    /// Establish connection and maintain it
    /// </summary>
    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Create WebSocket
            _webSocket = new ClientWebSocket();
            _logger.LogInformation("Server {ServerId}: Connecting to {Endpoint}", ServerId, _config.WebSocketEndpoint);
            
            await _webSocket.ConnectAsync(new Uri(_config.WebSocketEndpoint), cancellationToken);
            _logger.LogInformation("Server {ServerId}: WebSocket connected", ServerId);

            // Create MCP clients for each service
            foreach (var service in _config.McpServices)
            {
                try
                {
                    var transportOptions = new HttpClientTransportOptions
                    {
                        Endpoint = new Uri(service.NodeAddress),
                        Name = $"McpService_{service.ServiceName}",
                    };

                    if (service.Protocol == "sse")
                    {
                        transportOptions.TransportMode = HttpTransportMode.Sse;
                    }
                    else if (service.Protocol == "streamable-http" || service.Protocol == "http")
                    {
                        transportOptions.TransportMode = HttpTransportMode.StreamableHttp;
                    }

                    // ✅ Apply authentication configuration if present
                    if (McpAuthenticationHelper.IsAuthenticationConfigured(
                        service.AuthenticationType, 
                        service.AuthenticationConfig))
                    {
                        var authType = service.AuthenticationType!.ToLowerInvariant();

                        if (authType == "oauth2")
                        {
                            // Use SDK's OAuth support
                            transportOptions.OAuth = McpAuthenticationHelper.BuildOAuth2Options(
                                service.AuthenticationConfig!,
                                _logger);
                            
                            _logger.LogInformation(
                                "Server {ServerId}: Applied OAuth 2.0 authentication for service {ServiceName}",
                                ServerId, service.ServiceName);
                        }
                        else
                        {
                            // Use AdditionalHeaders for Bearer, Basic, and API Key
                            transportOptions.AdditionalHeaders = McpAuthenticationHelper.BuildAuthenticationHeaders(
                                service.AuthenticationType!,
                                service.AuthenticationConfig!,
                                _logger);
                            
                            _logger.LogInformation(
                                "Server {ServerId}: Applied {AuthType} authentication for service {ServiceName}",
                                ServerId, authType, service.ServiceName);
                        }
                    }
                    else
                    {
                        _logger.LogDebug(
                            "Server {ServerId}: No authentication configured for service {ServiceName}",
                            ServerId, service.ServiceName);
                    }

                    // 🔧 Create HttpClient with extended timeout to prevent premature disconnection
                    // Default HttpClient.Timeout is 100 seconds, which causes 404 errors after ~2 minutes
                    var httpClient = new HttpClient(new SocketsHttpHandler
                    {
                        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(3)
                    })
                    {
                        // Set to 30 seconds for better user experience
                        // Combined with Xiaozhi's periodic ping to maintain session
                        Timeout = TimeSpan.FromSeconds(30)
                    };

                    var transport = new HttpClientTransport(transportOptions, httpClient, ownsHttpClient: true);
                    var mcpClient = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
                    _mcpClients.Add(mcpClient);
                    
                    _logger.LogInformation("Server {ServerId}: MCP client connected to service {ServiceName} at {NodeAddress}", 
                        ServerId, service.ServiceName, service.NodeAddress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to MCP service {ServiceName} at {NodeAddress}", 
                        service.ServiceName, service.NodeAddress);
                }
            }

            // Reset reconnection state on successful connection
            _reconnectAttempt = 0;
            _currentBackoffMs = _reconnectionSettings.InitialBackoffMs;
            LastConnectedTime = DateTime.UtcNow;
            
            // ✅ Initialize last ping time on connection
            lock (_pingLock)
            {
                _lastPingReceivedTime = DateTime.UtcNow;
            }
            _logger.LogDebug(
                "Server {ServerId}: Initialized ping timeout monitor, expecting ping within {Timeout}s",
                ServerId, _pingTimeout.TotalSeconds);

            // Notify connection success if at least one MCP client connected
            if (_mcpClients.Count > 0)
            {
                _logger.LogInformation(
                    "Server {ServerId}: Connection established with {ConnectedCount}/{TotalCount} MCP services",
                    ServerId, _mcpClients.Count, _config.McpServices.Count);
                
                try
                {
                    await (OnConnected?.Invoke() ?? Task.CompletedTask);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OnConnected callback for server {ServerId}", ServerId);
                }
            }
            else
            {
                var errorMsg = $"No MCP clients connected (0/{_config.McpServices.Count})";
                _logger.LogWarning("Server {ServerId}: {Message}", ServerId, errorMsg);
                
                try
                {
                    await (OnConnectionFailed?.Invoke(errorMsg) ?? Task.CompletedTask);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OnConnectionFailed callback for server {ServerId}", ServerId);
                }
                
                // Still throw to trigger retry mechanism
                throw new InvalidOperationException(errorMsg);
            }

            // Run bidirectional communication + ping timeout monitor
            var communicationTasks = new List<Task>
            {
                PipeWebSocketToMcpAsync(cancellationToken),
                PipeMcpToWebSocketAsync(cancellationToken),
                MonitorPingTimeoutAsync(cancellationToken)  // ✅ 新增 Ping 超时监控
            };

            var completedTask = await Task.WhenAny(communicationTasks);

            LastDisconnectedTime = DateTime.UtcNow;
            
            // ✅ Notify disconnection
            _logger.LogWarning(
                "Server {ServerId}: WebSocket connection ended, triggering disconnect notification",
                ServerId);
            
            try
            {
                await (OnDisconnected?.Invoke() ?? Task.CompletedTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnected callback for server {ServerId}", ServerId);
            }

            if (completedTask.IsFaulted)
            {
                _logger.LogWarning("Server {ServerId} task faulted: {Error}", 
                    ServerId, completedTask.Exception?.GetBaseException().Message);
                throw completedTask.Exception?.GetBaseException() ?? new Exception("Task faulted");
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogError("Server {ServerId} WebSocket error: {Error}", ServerId, ex.Message);
            throw;
        }
        finally
        {
            await CleanupConnectionAsync();
        }
    }

    /// <summary>
    /// Cleanup WebSocket and MCP clients
    /// </summary>
    private async Task CleanupConnectionAsync()
    {
        foreach (var mcpClient in _mcpClients)
        {
            try
            {
                await mcpClient.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error disposing MCP client: {Error}", ex.Message);
            }
        }
        _mcpClients.Clear();

        if (_webSocket != null)
        {
            try
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                        "Session cleanup", CancellationToken.None);
                }
                _webSocket.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error closing WebSocket: {Error}", ex.Message);
            }
            _webSocket = null;
        }
    }

    /// <summary>
    /// Pipe WebSocket messages to MCP
    /// </summary>
    private async Task PipeWebSocketToMcpAsync(CancellationToken cancellationToken)
    {
        if (_webSocket == null || _mcpClients.Count == 0) return;

        var buffer = new byte[4096];

        try
        {
            while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogWarning(
                        "Server {ServerId}: WebSocket closed by server with status {CloseStatus}, description: {CloseDescription}",
                        ServerId, 
                        result.CloseStatus,
                        result.CloseStatusDescription ?? "none");
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                _logger.LogDebug("Server {ServerId} << Received: {Message}", ServerId, message);

                await ProcessWebSocketMessageAsync(message, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Server {ServerId}: WebSocket to MCP pipe cancelled", ServerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Server {ServerId}: Error in WebSocket to MCP pipe: {Error}, WebSocket State: {State}",
                ServerId, ex.Message, _webSocket?.State);
            throw;
        }
    }

    /// <summary>
    /// Pipe MCP responses to WebSocket (placeholder for now)
    /// </summary>
    private async Task PipeMcpToWebSocketAsync(CancellationToken cancellationToken)
    {
        if (_webSocket == null) return;

        try
        {
            while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Server {ServerId}: MCP to WebSocket pipe cancelled", ServerId);
        }
        catch (Exception ex)
        {
            _logger.LogError("Server {ServerId}: Error in MCP to WebSocket pipe: {Error}", ServerId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Monitor ping timeout - if no ping received within timeout period, consider connection dead
    /// </summary>
    private async Task MonitorPingTimeoutAsync(CancellationToken cancellationToken)
    {
        if (_webSocket == null) return;

        try
        {
            _logger.LogInformation(
                "Server {ServerId}: Starting ping timeout monitor (timeout: {Timeout}s)",
                ServerId, _pingTimeout.TotalSeconds);

            // Check every 10 seconds
            var checkInterval = TimeSpan.FromSeconds(10);

            while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(checkInterval, cancellationToken);

                // Check if ping timeout occurred
                TimeSpan timeSinceLastPing;
                lock (_pingLock)
                {
                    timeSinceLastPing = DateTime.UtcNow - _lastPingReceivedTime;
                }

                if (timeSinceLastPing > _pingTimeout)
                {
                    _logger.LogError(
                        "Server {ServerId}: Ping timeout detected! Last ping received {Seconds}s ago (timeout: {Timeout}s)",
                        ServerId, 
                        timeSinceLastPing.TotalSeconds,
                        _pingTimeout.TotalSeconds);

                    // Throw exception to trigger disconnection and reconnection
                    throw new TimeoutException(
                        $"No ping received from Xiaozhi for {timeSinceLastPing.TotalSeconds:F1} seconds, " +
                        $"exceeding timeout of {_pingTimeout.TotalSeconds} seconds");
                }

                // Log trace message every minute
                if (timeSinceLastPing.TotalSeconds > 60 && timeSinceLastPing.TotalSeconds % 60 < 10)
                {
                    _logger.LogTrace(
                        "Server {ServerId}: Ping health check - last ping {Seconds}s ago",
                        ServerId, timeSinceLastPing.TotalSeconds);
                }
            }

            _logger.LogInformation("Server {ServerId}: Ping timeout monitor stopped", ServerId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Server {ServerId}: Ping timeout monitor cancelled", ServerId);
        }
        catch (TimeoutException tex)
        {
            _logger.LogError(
                "Server {ServerId}: Ping timeout monitor detected dead connection: {Error}",
                ServerId, tex.Message);
            throw; // Re-throw to trigger reconnection
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Server {ServerId}: Error in ping timeout monitor: {Error}",
                ServerId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Process incoming WebSocket message
    /// </summary>
    private async Task ProcessWebSocketMessageAsync(string message, CancellationToken cancellationToken)
    {
        if (_webSocket == null) return;

        try
        {
            var jsonDoc = JsonDocument.Parse(message);
            var method = jsonDoc.RootElement.GetProperty("method").GetString();
            var id = jsonDoc.RootElement.TryGetProperty("id", out var idElement) ? idElement.GetInt32() : (int?)null;

            switch (method)
            {
                case "initialize":
                    await HandleInitializeAsync(id, jsonDoc, cancellationToken);
                    break;
                case "notifications/initialized":
                    await HandleInitializedNotificationAsync(cancellationToken);
                    break;
                case "tools/list":
                    await HandleToolsListAsync(id, cancellationToken);
                    break;
                case "tools/call":
                    await HandleToolsCallAsync(id, jsonDoc, cancellationToken);
                    break;
                case "ping":
                    await HandlePingAsync(id, cancellationToken);
                    break;
                default:
                    _logger.LogWarning("Server {ServerId}: Unknown method: {Method}", ServerId, method);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Server {ServerId}: Error processing message: {Error}", ServerId, ex.Message);
        }
    }

    private async Task HandleInitializeAsync(int? id, JsonDocument request, CancellationToken cancellationToken)
    {
        var response = new
        {
            jsonrpc = "2.0",
            id = id,
            result = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new
                {
                    experimental = new { },
                    prompts = new { listChanged = false },
                    resources = new { subscribe = false, listChanged = false },
                    tools = new { listChanged = false }
                },
                serverInfo = new
                {
                    name = _config.ServerName,
                    version = "1.0.0"
                }
            }
        };

        await SendWebSocketResponseAsync(response, cancellationToken);
    }

    private Task HandleInitializedNotificationAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Server {ServerId}: MCP session initialized", ServerId);
        return Task.CompletedTask;
    }

    private async Task HandlePingAsync(int? id, CancellationToken cancellationToken)
    {
        // ✅ Update last ping received time
        lock (_pingLock)
        {
            _lastPingReceivedTime = DateTime.UtcNow;
        }
        
        _logger.LogTrace(
            "Server {ServerId}: Ping received from Xiaozhi, updated last ping time",
            ServerId);
        
        // ✅ First, check WebSocket state
        if (_webSocket?.State != WebSocketState.Open)
        {
            _logger.LogError(
                "Server {ServerId}: Received ping but WebSocket is not open (state: {State})",
                ServerId, _webSocket?.State);
            
            // Throw exception to trigger reconnection
            throw new InvalidOperationException($"WebSocket state is {_webSocket?.State}, not Open");
        }
        
        var startTime = DateTime.UtcNow;
        
        _logger.LogDebug(
            "Server {ServerId}: Received ping request (id: {RequestId}) from Xiaozhi, forwarding to {Count} MCP client(s)",
            ServerId, id, _mcpClients.Count);

        // ✅ 向所有 MCP 客户端发送 ping 以保持连接活跃
        var pingTasks = _mcpClients.Select(async (mcpClient, index) =>
        {
            try
            {
                var clientStartTime = DateTime.UtcNow;
                
                // 使用 SDK 的 PingAsync 方法保持 HTTP 会话活跃
                await mcpClient.PingAsync(cancellationToken);
                
                var duration = (DateTime.UtcNow - clientStartTime).TotalMilliseconds;
                
                _logger.LogTrace(
                    "Server {ServerId}: Ping to MCP client {ClientIndex} succeeded in {Duration}ms",
                    ServerId, index, duration);
                
                return (success: true, index, duration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Server {ServerId}: Ping to MCP client {ClientIndex} failed: {Error}",
                    ServerId, index, ex.Message);
                return (success: false, index, duration: 0.0);
            }
        });

        // 等待所有 ping 完成
        var results = await Task.WhenAll(pingTasks);
        
        var successCount = results.Count(r => r.success);
        var totalDuration = (DateTime.UtcNow - startTime).TotalMilliseconds;
        var avgDuration = results.Where(r => r.success).Average(r => r.duration);
        
        _logger.LogInformation(
            "Server {ServerId}: Ping completed - {Success}/{Total} clients responded successfully, " +
            "total time: {TotalTime}ms, avg response: {AvgTime:F2}ms",
            ServerId, successCount, _mcpClients.Count, totalDuration, avgDuration);
        
        // 响应给小智
        var response = new
        {
            jsonrpc = "2.0",
            id = id,
            result = new 
            { 
                // 可选：返回健康状态
                healthy = results.Any(r => r.success),
                connectedClients = successCount
            }
        };

        await SendWebSocketResponseAsync(response, cancellationToken);
        
        _logger.LogDebug(
            "Server {ServerId}: Ping response sent to Xiaozhi (id: {RequestId})",
            ServerId, id);
    }

    private async Task HandleToolsListAsync(int? id, CancellationToken cancellationToken)
    {
        if (_mcpClients.Count == 0)
        {
            _logger.LogWarning("Server {ServerId}: No MCP clients available for tools/list request", ServerId);
            await SendErrorResponseAsync(id, -32603, "No MCP services available", 
                "No active MCP service bindings configured for this endpoint", cancellationToken);
            return;
        }

        try
        {
            // Collect tools from all MCP clients
            var allTools = new List<object>();
            
            for (int i = 0; i < _mcpClients.Count; i++)
            {
                var mcpClient = _mcpClients[i];
                var serviceConfig = i < _config.McpServices.Count ? _config.McpServices[i] : null;
                
                try
                {
                    var toolsResponse = await mcpClient.ListToolsAsync(null, cancellationToken);

                    foreach (var tool in toolsResponse)
                    {
                        // If service has selected tools, only include those
                        if (serviceConfig != null && serviceConfig.SelectedToolNames.Count > 0)
                        {
                            if (!serviceConfig.SelectedToolNames.Contains(tool.Name))
                            {
                                continue; // Skip tools not in the selected list
                            }
                        }
                        
                        var properties = new Dictionary<string, object>();
                        var required = Array.Empty<string>();

                        if (tool.JsonSchema.ValueKind != JsonValueKind.Undefined)
                        {
                            if (tool.JsonSchema.TryGetProperty("properties", out var propsElement))
                            {
                                properties = JsonElementToObject(propsElement) as Dictionary<string, object> ?? new Dictionary<string, object>();
                            }

                            if (tool.JsonSchema.TryGetProperty("required", out var reqElement))
                            {
                                required = reqElement.EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
                            }
                        }

                        allTools.Add(new
                        {
                            name = tool.Name,
                            description = tool.Description,
                            inputSchema = new
                            {
                                properties = properties,
                                required = required,
                                title = $"{tool.Name}Arguments",
                                type = "object"
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching tools from MCP client");
                }
            }

            var response = new
            {
                jsonrpc = "2.0",
                id = id,
                result = new
                {
                    tools = allTools.ToArray()
                }
            };

            await SendWebSocketResponseAsync(response, cancellationToken);
        }
        catch (Exception ex)
        {
            await SendErrorResponseAsync(id, -32603, "Internal error", ex.Message, cancellationToken);
        }
    }

    private async Task HandleToolsCallAsync(int? id, JsonDocument request, CancellationToken cancellationToken)
    {
        if (_mcpClients.Count == 0)
        {
            _logger.LogWarning("Server {ServerId}: No MCP clients available for tools/call request", ServerId);
            await SendErrorResponseAsync(id, -32603, "No MCP services available", 
                "No active MCP service bindings configured for this endpoint", cancellationToken);
            return;
        }

        try
        {
            var paramsElement = request.RootElement.GetProperty("params");
            var toolName = paramsElement.GetProperty("name").GetString()!;

            var arguments = new Dictionary<string, object?>();
            if (paramsElement.TryGetProperty("arguments", out var argsElement))
            {
                foreach (var property in argsElement.EnumerateObject())
                {
                    arguments[property.Name] = JsonElementToObject(property.Value);
                }
            }

            _logger.LogInformation("Server {ServerId}: Calling tool {ToolName} with arguments: {Arguments}", 
                ServerId, toolName, JsonSerializer.Serialize(arguments));

            // Try to call the tool on each MCP client until one succeeds
            object? result = null;
            Exception? lastException = null;
            
            for (int i = 0; i < _mcpClients.Count; i++)
            {
                var mcpClient = _mcpClients[i];
                var serviceConfig = i < _config.McpServices.Count ? _config.McpServices[i] : null;
                
                // Check if tool is allowed for this service
                if (serviceConfig != null && serviceConfig.SelectedToolNames.Count > 0)
                {
                    if (!serviceConfig.SelectedToolNames.Contains(toolName))
                    {
                        continue; // Skip this client if tool is not in selected list
                    }
                }
                
                try
                {
                    result = await mcpClient.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken);
                    break; // Success
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogDebug("Tool {ToolName} not found in client, trying next", toolName);
                }
            }

            if (result != null)
            {
                var response = new
                {
                    jsonrpc = "2.0",
                    id = id,
                    result = result
                };

                await SendWebSocketResponseAsync(response, cancellationToken);
            }
            else
            {
                throw lastException ?? new Exception($"Tool {toolName} not found in any MCP service");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Server {ServerId}: Error calling tool: {Error}", ServerId, ex.Message);
            await SendErrorResponseAsync(id, -32603, "Internal error", ex.Message, cancellationToken);
        }
    }

    private async Task SendWebSocketResponseAsync(object response, CancellationToken cancellationToken)
    {
        if (_webSocket?.State != WebSocketState.Open)
        {
            _logger.LogError(
                "Server {ServerId}: Cannot send WebSocket response, state is {State}",
                ServerId, _webSocket?.State);
            
            // Throw exception to trigger reconnection
            throw new InvalidOperationException($"Cannot send response, WebSocket state is {_webSocket?.State}");
        }

        try
        {
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            _logger.LogDebug("Server {ServerId} >> Sending: {Response}", ServerId, json);

            var bytes = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
        }
        catch (WebSocketException ex)
        {
            _logger.LogError(
                "Server {ServerId}: WebSocket send failed: {Error}",
                ServerId, ex.Message);
            throw; // Re-throw to trigger reconnection
        }
    }

    private async Task SendErrorResponseAsync(int? id, int code, string message, string? data, CancellationToken cancellationToken)
    {
        var errorResponse = new
        {
            jsonrpc = "2.0",
            id = id,
            error = new
            {
                code = code,
                message = message,
                data = data
            }
        };

        await SendWebSocketResponseAsync(errorResponse, cancellationToken);
    }

    private static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => JsonElementToObject(p.Value)),
            _ => element.ToString()
        };
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cancellationTokenSource.Dispose();
    }
}
