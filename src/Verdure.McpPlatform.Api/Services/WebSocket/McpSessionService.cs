using ModelContextProtocol.Client;
using System.Net.WebSockets;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
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
    private readonly IMcpClientService _mcpClientService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly McpSessionConfiguration _config;
    private readonly ReconnectionSettings _reconnectionSettings;

    private readonly JsonSerializerOptions _jsonOptions;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    // Reconnection state
    private int _reconnectAttempt = 0;
    private int _currentBackoffMs;

    // Session state
    private ClientWebSocket? _webSocket;
    private bool _isRunning = false;

    // ✅ Ping timeout monitoring
    private DateTime _lastPingReceivedTime = DateTime.UtcNow;
    private readonly TimeSpan _pingTimeout = TimeSpan.FromSeconds(120); // 120秒未收到 ping 视为断开（小智每60秒发一次）
    private readonly object _pingLock = new();

    // Connection status events
    public event Func<Task>? OnConnected;
    public event Func<string, Task>? OnConnectionFailed;
    public event Func<Task>? OnDisconnected;

    public string ServerId => _config.ServerId;
    public string ServerName => _config.ServerName;
    public bool IsConnected => _webSocket?.State == WebSocketState.Open && !IsPingTimeout;
    public int ConnectedClientsCount => 0; // transient clients are created per-call, not kept here
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
        IMcpClientService mcpClientService,
        IServiceScopeFactory serviceScopeFactory,
        ILoggerFactory loggerFactory)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _reconnectionSettings = reconnectionSettings ?? throw new ArgumentNullException(nameof(reconnectionSettings));
        _mcpClientService = mcpClientService ?? throw new ArgumentNullException(nameof(mcpClientService));
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
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
            // ⚠️ CRITICAL: Connect to MCP services FIRST, before WebSocket
            // This ensures all backend services are ready before we tell Xiaozhi we're online

            // Note: Do NOT pre-create persistent MCP clients here.
            // Keep the WebSocket connection lightweight and create transient MCP clients only when needed (e.g. tools/call).
            _logger.LogInformation("Server {ServerId}: Skipping eager MCP client creation ({Count} configured), will create transient clients on demand", ServerId, _config.McpServices.Count);

            // ✅ NOW connect to WebSocket - all backend services are ready
            _webSocket = new ClientWebSocket();
            _logger.LogInformation("Server {ServerId}: Connecting to {Endpoint}", ServerId, _config.WebSocketEndpoint);

            await _webSocket.ConnectAsync(new Uri(_config.WebSocketEndpoint), cancellationToken);
            _logger.LogInformation("Server {ServerId}: WebSocket connected", ServerId);

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
                "Server {ServerId}: Initialized last ping time (expecting periodic pings)",
                ServerId);

            // ✅ Notify connection success (we already checked MCP client count above)
            _logger.LogInformation(
                "Server {ServerId}: Connection established (configured services: {TotalCount})",
                ServerId, _config.McpServices.Count);

            try
            {
                await (OnConnected?.Invoke() ?? Task.CompletedTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnected callback for server {ServerId}", ServerId);
            }

            // Run bidirectional communication + ping timeout monitor
            var communicationTasks = new List<Task>
            {
                PipeWebSocketToMcpAsync(cancellationToken),
                PipeMcpToWebSocketAsync(cancellationToken)
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
        // No persistent MCP clients are kept in this session anymore.

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
        if (_webSocket == null) return;

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
    // Ping timeout monitor removed: pings are acknowledged but the session does not continuously poll.

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

        // For performance and scale: do NOT iterate and ping all MCP clients on every ping.
        // WebSocket itself proves connectivity; respond healthy by default.

        var response = new
        {
            jsonrpc = "2.0",
            id = id,
            result = new
            {
                healthy = true,
                connectedClients = 0 // transient clients are created on demand
            }
        };

        await SendWebSocketResponseAsync(response, cancellationToken);

        _logger.LogDebug("Server {ServerId}: Ping handled quickly; reported healthy", ServerId);
    }

    private async Task HandleToolsListAsync(int? id, CancellationToken cancellationToken)
    {
        // ✅ 直接检查配置，不依赖 MCP 客户端状态
        if (_config.McpServices.Count == 0)
        {
            _logger.LogWarning("Server {ServerId}: No MCP services configured for tools/list request", ServerId);
            await SendErrorResponseAsync(id, -32603, "No MCP services configured",
                "No MCP service bindings configured for this endpoint", cancellationToken);
            return;
        }

        try
        {
            // 🚀 优化：直接从配置的工具数据获取，不依赖 MCP 客户端连接状态！
            var allTools = new List<object>();

            // ✅ 直接遍历配置中的所有服务
            foreach (var serviceConfig in _config.McpServices)
            {
                try
                {
                    // 直接从 SelectedTools 获取完整的工具信息
                    foreach (var tool in serviceConfig.SelectedTools)
                    {
                        // 解析存储的 InputSchema JSON
                        var properties = new Dictionary<string, object>();
                        var required = Array.Empty<string>();

                        if (!string.IsNullOrEmpty(tool.InputSchema))
                        {
                            try
                            {
                                var schemaDoc = JsonDocument.Parse(tool.InputSchema);
                                if (schemaDoc.RootElement.TryGetProperty("properties", out var propsElement))
                                {
                                    properties = JsonElementToObject(propsElement) as Dictionary<string, object> ?? new Dictionary<string, object>();
                                }

                                if (schemaDoc.RootElement.TryGetProperty("required", out var reqElement))
                                {
                                    required = reqElement.EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to parse InputSchema for tool {ToolName}", tool.Name);
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

                    _logger.LogDebug(
                        "Server {ServerId}: Loaded {Count} tools from binding for service {ServiceName}",
                        ServerId, serviceConfig.SelectedTools.Count, serviceConfig.ServiceName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing tools for service {ServiceName}", serviceConfig.ServiceName);
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
                ServerId, toolName, JsonSerializer.Serialize(arguments, _jsonOptions));

            // Find services that have this tool configured
            var candidateServices = _config.McpServices
                .Where(s => s.SelectedTools != null && s.SelectedTools.Any(t => t.Name == toolName))
                .ToList();

            if (candidateServices.Count == 0)
            {
                _logger.LogWarning("Server {ServerId}: Tool {ToolName} not found in any configured service", ServerId, toolName);
                await SendErrorResponseAsync(id, -32601, "Method not found", $"Tool {toolName} not configured", cancellationToken);
                return;
            }

            Exception? lastException = null;
            object? finalResult = null;

            var userContextHeaders = await GetUserContextHeadersAsync();

            // Try candidate services one by one, creating a transient McpClient per attempt
            foreach (var service in candidateServices)
            {
                McpClient? transientClient = null;
                try
                {
                    transientClient = await _mcpClientService.CreateMcpClientAsync(
                        $"McpService_{service.ServiceName}",
                        service.NodeAddress,
                        service.Protocol,
                        service.AuthenticationType,
                        service.AuthenticationConfig,
                        additionalHeaders: userContextHeaders,
                        cancellationToken: cancellationToken);

                    finalResult = await transientClient.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken);

                    // Success - dispose client and break
                    await transientClient.DisposeAsync();
                    transientClient = null;
                    break;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Server {ServerId}: Tool {ToolName} call failed on service {ServiceName}, trying next", ServerId, toolName, service.ServiceName);
                }
                finally
                {
                    if (transientClient != null)
                    {
                        try { await transientClient.DisposeAsync(); } catch { }
                    }
                }
            }

            if (finalResult != null)
            {
                var response = new
                {
                    jsonrpc = "2.0",
                    id = id,
                    result = finalResult
                };

                await SendWebSocketResponseAsync(response, cancellationToken);
                return;
            }

            throw lastException ?? new Exception($"Tool {toolName} call failed on all candidate services");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Server {ServerId}: Error calling tool: {Error}", ServerId, ex.Message);
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

    // Recovery methods removed: clients are created on demand per tools/call and disposed immediately.

    /// <summary>
    /// Get user context headers for MCP client requests
    /// Creates a new service scope to avoid DbContext disposal issues
    /// </summary>
    private async Task<Dictionary<string, string>?> GetUserContextHeadersAsync()
    {
        try
        {
            // Create a new scope to get a fresh IUserInfoService instance
            using var scope = _serviceScopeFactory.CreateScope();
            var userInfoService = scope.ServiceProvider.GetRequiredService<IUserInfoService>();
            
            var userInfoMap = await userInfoService.GetUsersByIdsAsync(new[] { _config.UserId });
            if (userInfoMap.TryGetValue(_config.UserId, out var userInfo))
            {
                var headers = new Dictionary<string, string>
                {
                    ["X-User-Id"] = userInfo.UserId
                };

                if (!string.IsNullOrEmpty(userInfo.Email))
                {
                    headers["X-User-Email"] = userInfo.Email;
                }

                _logger.LogDebug(
                    "Server {ServerId}: Adding user context headers: UserId={UserId}, Email={Email}",
                    ServerId,
                    userInfo.UserId,
                    userInfo.Email ?? "(not set)");

                return headers;
            }
            else
            {
                _logger.LogWarning(
                    "Server {ServerId}: User {UserId} not found, user context headers will not be added",
                    ServerId,
                    _config.UserId);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Server {ServerId}: Error fetching user information, user context headers will not be added",
                ServerId);
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cancellationTokenSource.Dispose();
    }
}
