using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
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
/// </summary>
public class McpClientService : IMcpClientService
{
    private readonly ILogger<McpClientService> _logger;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 支持中文等字符正常显示
    };

    public McpClientService(ILogger<McpClientService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                transportOptions.AdditionalHeaders = McpAuthenticationHelper.BuildAuthenticationHeaders(
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

        // 🔧 Create HttpClient with minimal configuration
        // SDK manages SSE connection lifetime via stream, not connection pooling
        // We only set request timeout for fast failure on unresponsive tools
        var httpClient = new HttpClient()
        {
            // Individual request timeout (not connection lifetime)
            // Set to 10 seconds for fast failure on unavailable tools
            // This applies to tool calls, not the SSE stream itself
            Timeout = TimeSpan.FromSeconds(5)
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

        // Delegate to parameter-based overload
        return await CreateMcpClientAsync(
            config.Name,
            config.Endpoint,
            config.Protocol,
            config.AuthenticationType,
            config.AuthenticationConfig,
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