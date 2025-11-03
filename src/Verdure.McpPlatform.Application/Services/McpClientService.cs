using Microsoft.Extensions.Logging;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Client;
using System.Text;
using System.Text.Json;
using Verdure.McpPlatform.Contracts.Models;
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
                    transportOptions.OAuth = BuildOAuth2Options(config);
                }
                else
                {
                    // Use AdditionalHeaders for Bearer, Basic, and API Key
                    transportOptions.AdditionalHeaders = BuildAuthenticationHeaders(config);
                }
            }

            // Create transport and client
            var transport = new HttpClientTransport(transportOptions);
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
    /// Builds authentication headers based on the service configuration
    /// </summary>
    /// <param name="config">The MCP service configuration</param>
    /// <returns>Dictionary of authentication headers</returns>
    private Dictionary<string, string> BuildAuthenticationHeaders(McpServiceConfig config)
    {
        try
        {
            var authType = config.AuthenticationType!.ToLowerInvariant();

            return authType switch
            {
                "bearer" => BuildBearerTokenHeaders(config),
                "basic" => BuildBasicAuthHeaders(config),
                "apikey" => BuildApiKeyHeaders(config),
                _ => throw new InvalidOperationException($"Unsupported authentication type: {config.AuthenticationType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to build authentication headers for service {ServiceName}",
                config.Name);
            throw new InvalidOperationException(
                $"Failed to configure authentication for service '{config.Name}': {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Build Bearer Token authentication headers
    /// </summary>
    private Dictionary<string, string> BuildBearerTokenHeaders(McpServiceConfig config)
    {
        var authConfig = JsonSerializer.Deserialize<BearerTokenAuthConfig>(
            config.AuthenticationConfig!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (authConfig == null || string.IsNullOrEmpty(authConfig.Token))
        {
            throw new InvalidOperationException("Bearer token is required but not configured");
        }

        var headerName = string.IsNullOrEmpty(authConfig.HeaderName)
            ? "Authorization"
            : authConfig.HeaderName;

        var headerValue = headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
            ? $"Bearer {authConfig.Token}"
            : authConfig.Token;

        _logger.LogDebug(
            "Applied Bearer token authentication to header {HeaderName} for service {ServiceName}",
            headerName,
            config.Name);

        return new Dictionary<string, string>
        {
            [headerName] = headerValue
        };
    }

    /// <summary>
    /// Build Basic authentication headers
    /// </summary>
    private Dictionary<string, string> BuildBasicAuthHeaders(McpServiceConfig config)
    {
        var authConfig = JsonSerializer.Deserialize<BasicAuthConfig>(
            config.AuthenticationConfig!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (authConfig == null ||
            string.IsNullOrEmpty(authConfig.Username) ||
            string.IsNullOrEmpty(authConfig.Password))
        {
            throw new InvalidOperationException("Username and password are required for Basic authentication");
        }

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{authConfig.Username}:{authConfig.Password}"));

        _logger.LogDebug(
            "Applied Basic authentication for user {Username} to service {ServiceName}",
            authConfig.Username,
            config.Name);

        return new Dictionary<string, string>
        {
            ["Authorization"] = $"Basic {credentials}"
        };
    }

    /// <summary>
    /// Build API Key authentication headers
    /// </summary>
    private Dictionary<string, string> BuildApiKeyHeaders(McpServiceConfig config)
    {
        var authConfig = JsonSerializer.Deserialize<ApiKeyAuthConfig>(
            config.AuthenticationConfig!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (authConfig == null ||
            string.IsNullOrEmpty(authConfig.ApiKey) ||
            string.IsNullOrEmpty(authConfig.HeaderName))
        {
            throw new InvalidOperationException("API key and header name are required for API Key authentication");
        }

        var headerValue = string.IsNullOrEmpty(authConfig.Prefix)
            ? authConfig.ApiKey
            : $"{authConfig.Prefix}{authConfig.ApiKey}";

        _logger.LogDebug(
            "Applied API Key authentication to header {HeaderName} for service {ServiceName}",
            authConfig.HeaderName,
            config.Name);

        return new Dictionary<string, string>
        {
            [authConfig.HeaderName] = headerValue
        };
    }

    /// <summary>
    /// Build OAuth 2.0 options for SDK's built-in OAuth support
    /// </summary>
    private ClientOAuthOptions BuildOAuth2Options(McpServiceConfig config)
    {
        var authConfig = JsonSerializer.Deserialize<OAuth2AuthConfig>(
            config.AuthenticationConfig!,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (authConfig == null)
        {
            throw new InvalidOperationException("OAuth 2.0 configuration is required");
        }

        // Validate required OAuth configuration
        if (string.IsNullOrEmpty(authConfig.ClientId))
        {
            throw new InvalidOperationException(
                $"OAuth 2.0 Client ID is required for service '{config.Name}'");
        }

        if (string.IsNullOrEmpty(authConfig.RedirectUri))
        {
            throw new InvalidOperationException(
                $"OAuth 2.0 Redirect URI is required for service '{config.Name}'");
        }

        _logger.LogDebug(
            "Configuring OAuth 2.0 for service {ServiceName} with Client ID: {ClientId}",
            config.Name,
            authConfig.ClientId);

        var oauthOptions = new ClientOAuthOptions
        {
            RedirectUri = new Uri(authConfig.RedirectUri),
            ClientId = authConfig.ClientId,
            ClientSecret = authConfig.ClientSecret
        };

        // Add scopes if specified
        if (!string.IsNullOrEmpty(authConfig.Scope))
        {
            oauthOptions.Scopes = authConfig.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        return oauthOptions;
    }
}
