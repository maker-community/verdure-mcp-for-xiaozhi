using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using Verdure.McpPlatform.Contracts.Models;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;
using System.Net.Http.Headers;

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
/// Compatibility Enhancements:
/// - Removes charset parameter from Content-Type header for better compatibility
///   (Some MCP servers like ModelScope reject 'application/json; charset=utf-8')
/// </summary>
public class McpClientService : IMcpClientService
{
    private readonly ILogger<McpClientService> _logger;

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

    private async Task<IEnumerable<McpToolInfo>> GetToolsViaHttpAsync(McpServiceConfig config)
    {
        McpClient? client = null;

        try
        {
            if (string.IsNullOrEmpty(config.Endpoint))
            {
                throw new InvalidOperationException("Endpoint cannot be empty for HTTP protocol");
            }

            if (!Uri.TryCreate(config.Endpoint, UriKind.Absolute, out var endpointUri))
            {
                throw new InvalidOperationException($"Invalid endpoint URL: {config.Endpoint}");
            }

            _logger.LogDebug(
                "Creating HTTP transport for endpoint: {Endpoint} with authentication: {AuthType}",
                config.Endpoint,
                config.AuthenticationType ?? "none");

            // Build transport options with authentication
            var transportOptions = new HttpClientTransportOptions
            {
                Endpoint = endpointUri,
                Name = config.Name
            };

            if (config.Protocol == "sse")
            {
                transportOptions.TransportMode = HttpTransportMode.Sse;
            }
            else if (config.Protocol == "streamable-http" || config.Protocol == "http")
            {
                transportOptions.TransportMode = HttpTransportMode.StreamableHttp;
            }

            // Configure authentication based on type
            if (!string.IsNullOrEmpty(config.AuthenticationType) &&
                !string.IsNullOrEmpty(config.AuthenticationConfig))
            {
                var authType = config.AuthenticationType.ToLowerInvariant();

                if (authType == "oauth2")
                {
                    // Use SDK's OAuth support
                    transportOptions.OAuth = McpAuthenticationHelper.BuildOAuth2Options(
                        config.AuthenticationConfig,
                        _logger);
                }
                else
                {
                    // Use AdditionalHeaders for Bearer, Basic, and API Key
                    transportOptions.AdditionalHeaders = McpAuthenticationHelper.BuildAuthenticationHeaders(
                        config.AuthenticationType,
                        config.AuthenticationConfig,
                        _logger);
                }
            }

            // Create HttpClient with RemoveCharsetHttpHandler for better MCP server compatibility
            // Some servers (e.g., ModelScope) reject 'Content-Type: application/json; charset=utf-8'
            // and require 'Content-Type: application/json' without charset parameter
            var socketsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(3)
            };

            var charsetRemoverHandler = new RemoveCharsetHttpHandler(_logger)
            {
                InnerHandler = socketsHandler
            };

            var httpClient = new HttpClient(charsetRemoverHandler)
            {
                // Set to 30 seconds for better user experience with tool listing
                Timeout = TimeSpan.FromSeconds(30)
            };

            // Create transport and client
            var transport = new HttpClientTransport(transportOptions, httpClient, ownsHttpClient: true);
            client = await McpClient.CreateAsync(transport);

            _logger.LogInformation(
                "Successfully connected to MCP service {ServiceName} via HTTP with {AuthType} authentication",
                config.Name,
                config.AuthenticationType ?? "none");

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
                // Note: Schema information will be available in future SDK versions
                InputSchema = null
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

    /// <summary>
    /// Custom HttpMessageHandler that removes charset parameter from Content-Type header
    /// to ensure compatibility with MCP servers that strictly parse Content-Type
    /// 
    /// Background:
    /// - C# HttpClient's StringContent automatically adds 'charset=utf-8' to Content-Type
    /// - Some MCP servers (e.g., ModelScope) reject requests with charset parameter
    /// - Standard: 'application/json; charset=utf-8' is valid per HTTP spec
    /// - Reality: Some servers only accept 'application/json' without charset
    /// 
    /// This handler ensures maximum compatibility across different MCP server implementations.
    /// </summary>
    private class RemoveCharsetHttpHandler : DelegatingHandler
    {
        private readonly ILogger _logger;

        public RemoveCharsetHttpHandler(ILogger logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // If request has Content with Content-Type, remove charset parameter
            if (request.Content?.Headers.ContentType != null)
            {
                var contentType = request.Content.Headers.ContentType;
                var originalContentType = contentType.ToString();

                // Check if charset parameter exists
                var hasCharset = contentType.Parameters.Any(p =>
                    p.Name.Equals("charset", StringComparison.OrdinalIgnoreCase));

                if (hasCharset)
                {
                    // Create new MediaTypeHeaderValue without charset parameter
                    var newContentType = new MediaTypeHeaderValue(contentType.MediaType!);

                    // Copy all parameters except charset
                    foreach (var param in contentType.Parameters)
                    {
                        if (!param.Name.Equals("charset", StringComparison.OrdinalIgnoreCase))
                        {
                            newContentType.Parameters.Add(param);
                        }
                    }

                    request.Content.Headers.ContentType = newContentType;

                    _logger.LogDebug(
                        "Removed charset parameter from Content-Type for better MCP server compatibility: {Original} -> {Modified}",
                        originalContentType,
                        newContentType);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
