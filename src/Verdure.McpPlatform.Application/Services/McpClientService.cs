using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service implementation for connecting to and interacting with MCP services
/// Uses ModelContextProtocol SDK 0.4.0-preview.3
/// 
/// Authentication Support:
/// - Bearer Token: Direct header injection
/// - Basic Auth: Base64 encoded credentials
/// - API Key: Custom header with optional prefix
/// - OAuth 2.0: Uses pre-obtained access tokens (frontend must handle OAuth flow)
/// 
/// User Context Propagation:
/// - Automatically adds X-User-Id and X-User-Email headers for user identification
/// </summary>
public class McpClientService : IMcpClientService
{
    private readonly ILogger<McpClientService> _logger;
    private readonly IUserInfoService _userInfoService;
    private readonly TimeSpan _toolRequestTimeout;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 支持中文等字符正常显示
    };

    public McpClientService(
        ILogger<McpClientService> logger,
        IUserInfoService userInfoService,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userInfoService = userInfoService ?? throw new ArgumentNullException(nameof(userInfoService));

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var raw = configuration["McpClient:ToolRequestTimeoutSeconds"];
        int seconds;
        if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw, out seconds) || seconds <= 0)
        {
            seconds = 5;
        }

        _toolRequestTimeout = TimeSpan.FromSeconds(seconds);
        _logger.LogDebug("MCP client tool request timeout set to {TimeoutSeconds}s", seconds);
    }

    public async Task<IEnumerable<McpToolInfo>> GetToolsAsync(McpServiceConfig config)
    {
        _logger.LogInformation(
            "Connecting to MCP service {ServiceName} at {Endpoint} using protocol {Protocol}",
            config.Name,
            config.Endpoint,
            config.Protocol);

        return config.Protocol?.ToLowerInvariant() switch
        {
            "stdio" => await GetToolsViaStdioAsync(config),
            "streamable-http" or "http" => await GetToolsViaHttpAsync(config),
            "sse" => await GetToolsViaSseAsync(config),
            _ => throw new InvalidOperationException(
                $"Unknown protocol: {config.Protocol}. Supported protocols: stdio, streamable-http, sse")
        };
    }

    private Task<IEnumerable<McpToolInfo>> GetToolsViaStdioAsync(McpServiceConfig config)
    {
        // Stdio is not yet fully supported in SDK 0.4.0-preview.3
        _logger.LogError(
            "Stdio protocol requested for service {ServiceName} but is not yet supported",
            config.Name);

        throw new NotSupportedException(
            $"Stdio protocol is not yet supported in ModelContextProtocol SDK 0.4.0-preview.3. " +
            $"Please use 'streamable-http' protocol instead for service '{config.Name}'.");
    }

    public async Task<McpClient> CreateMcpClientAsync(
        string name,
        string endpoint,
        string? protocol = null,
        string? authenticationType = null,
        string? authenticationConfig = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new InvalidOperationException("Endpoint cannot be empty");
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            throw new InvalidOperationException($"Invalid endpoint URL: {endpoint}");
        }

        _logger.LogDebug(
            "Creating MCP client for service {ServiceName} at {Endpoint} with authentication: {AuthType}",
            name,
            endpoint,
            authenticationType ?? "none");

        // Build transport options with authentication
        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = endpointUri,
            Name = name,
            OmitContentTypeCharset = true, // Remove charset to avoid issues with some servers
        };

        // Determine transport mode based on protocol
        if (protocol == "sse")
        {
            transportOptions.TransportMode = HttpTransportMode.Sse;
        }
        else if (protocol == "streamable-http" || protocol == "http")
        {
            transportOptions.TransportMode = HttpTransportMode.StreamableHttp;
        }

        // Configure authentication based on type
        Dictionary<string, string>? headers = null;
        
        if (!string.IsNullOrEmpty(authenticationType) &&
            !string.IsNullOrEmpty(authenticationConfig))
        {
            var authType = authenticationType.ToLowerInvariant();

            if (authType == "oauth2")
            {
                // Use SDK's OAuth support
                transportOptions.OAuth = McpAuthenticationHelper.BuildOAuth2Options(
                    authenticationConfig,
                    _logger);
                
                _logger.LogInformation(
                    "Applied OAuth 2.0 authentication for service {ServiceName}",
                    name);
            }
            else
            {
                // Use AdditionalHeaders for Bearer, Basic, and API Key
                headers = McpAuthenticationHelper.BuildAuthenticationHeaders(
                    authenticationType,
                    authenticationConfig,
                    _logger);
                
                _logger.LogInformation(
                    "Applied {AuthType} authentication for service {ServiceName}",
                    authType, name);
            }
        }
        else
        {
            _logger.LogDebug(
                "No authentication configured for service {ServiceName}",
                name);
        }
        
        // Merge additional headers (user context) with authentication headers
        if (additionalHeaders != null && additionalHeaders.Count > 0)
        {
            headers ??= new Dictionary<string, string>();
            foreach (var kvp in additionalHeaders)
            {
                headers[kvp.Key] = kvp.Value;
            }
            
            _logger.LogDebug(
                "Added {Count} additional headers for service {ServiceName}",
                additionalHeaders.Count,
                name);
        }
        
        // Apply merged headers to transport options
        if (headers != null && headers.Count > 0)
        {
            transportOptions.AdditionalHeaders = headers;
        }

        // 🔧 Create HttpClient with minimal configuration
        // SDK manages SSE connection lifetime via stream, not connection pooling
        // We set request timeout for fast failure on unresponsive tools.
        // Timeout value is read from configuration key 'McpClient:ToolRequestTimeoutSeconds'
        // and defaults to 5 seconds when not configured.
        var httpClient = new HttpClient()
        {
            // Individual request timeout (not connection lifetime)
            Timeout = _toolRequestTimeout
        };

        // Create transport and client
        var transport = new HttpClientTransport(transportOptions, httpClient, ownsHttpClient: true);
        var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Successfully created MCP client for service {ServiceName} with {AuthType} authentication",
            name,
            authenticationType ?? "none");

        return client;
    }

    public async Task<McpClient> CreateMcpClientAsync(
        McpServiceConfig config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (string.IsNullOrEmpty(config.Endpoint))
        {
            throw new InvalidOperationException("Endpoint cannot be empty");
        }

        // Query user information for context propagation
        Dictionary<string, string>? userContextHeaders = null;
        try
        {
            var userInfoMap = await _userInfoService.GetUsersByIdsAsync(new[] { config.UserId });
            if (userInfoMap.TryGetValue(config.UserId, out var userInfo))
            {
                userContextHeaders = new Dictionary<string, string>();
                
                // Add user ID header
                userContextHeaders["X-User-Id"] = userInfo.UserId;
                
                // Add user email header if available
                if (!string.IsNullOrEmpty(userInfo.Email))
                {
                    userContextHeaders["X-User-Email"] = userInfo.Email;
                }
                
                _logger.LogDebug(
                    "Adding user context headers for service {ServiceName}: UserId={UserId}, Email={Email}",
                    config.Name,
                    userInfo.UserId,
                    userInfo.Email ?? "(not set)");
            }
            else
            {
                _logger.LogWarning(
                    "User {UserId} not found for service {ServiceName}, user context headers will not be added",
                    config.UserId,
                    config.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching user information for service {ServiceName}, user context headers will not be added",
                config.Name);
        }

        // Delegate to parameter-based overload with user context
        return await CreateMcpClientAsync(
            config.Name,
            config.Endpoint,
            config.Protocol,
            config.AuthenticationType,
            config.AuthenticationConfig,
            userContextHeaders,
            cancellationToken);
    }

    private async Task<IEnumerable<McpToolInfo>> GetToolsViaHttpAsync(McpServiceConfig config)
    {
        McpClient? client = null;

        try
        {
            // Use the unified CreateMcpClientAsync method
            client = await CreateMcpClientAsync(config);

            // List available tools
            var toolsResult = await client.ListToolsAsync();

            if (toolsResult == null || toolsResult.Count == 0)
            {
                _logger.LogWarning("No tools returned from MCP service {ServiceName}", config.Name);
                return Enumerable.Empty<McpToolInfo>();
            }

            var tools = toolsResult.Select(tool => new McpToolInfo
            {
                Name = tool.Name ?? "Unknown",
                Description = tool.Description,
                // Serialize the JsonSchema to string for storage
                InputSchema = tool.JsonSchema.ValueKind != System.Text.Json.JsonValueKind.Undefined 
                    ? JsonSerializer.Serialize(tool.JsonSchema, _jsonSerializerOptions)
                    : null
            }).ToList();

            _logger.LogInformation(
                "Successfully retrieved {Count} tools from MCP service {ServiceName}",
                tools.Count,
                config.Name);

            return tools;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error connecting to MCP service {ServiceName} via HTTP",
                config.Name);
            throw new InvalidOperationException(
                $"Failed to retrieve tools from MCP service '{config.Name}': {ex.Message}",
                ex);
        }
        finally
        {
            if (client != null)
            {
                await client.DisposeAsync();
            }
        }
    }

    private async Task<IEnumerable<McpToolInfo>> GetToolsViaSseAsync(McpServiceConfig config)
    {
        // SSE is not fully supported in SDK 0.4.0-preview.3
        _logger.LogError(
            "SSE protocol requested for service {ServiceName} but full support is limited in SDK 0.4.0",
            config.Name);

        // For now, fall back to HTTP
        _logger.LogWarning(
            "Falling back to HTTP protocol for service {ServiceName}",
            config.Name);

        return await GetToolsViaHttpAsync(config);
    }
}