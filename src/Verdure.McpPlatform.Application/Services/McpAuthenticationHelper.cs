using Microsoft.Extensions.Logging;
using ModelContextProtocol.Authentication;
using System.Text;
using System.Text.Json;
using Verdure.McpPlatform.Contracts.Models;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Helper class for building MCP authentication configurations
/// Shared by McpClientService (tool synchronization) and McpSessionService (WebSocket connections)
/// 
/// Supports:
/// - Bearer Token: Direct header injection
/// - Basic Auth: Base64 encoded credentials
/// - API Key: Custom header with optional prefix
/// - OAuth 2.0: Uses SDK's built-in OAuth support
/// </summary>
public static class McpAuthenticationHelper
{
    /// <summary>
    /// Builds authentication headers for Bearer, Basic, or API Key authentication
    /// </summary>
    /// <param name="authenticationType">Type of authentication (bearer, basic, apikey)</param>
    /// <param name="authenticationConfig">JSON configuration string</param>
    /// <param name="logger">Optional logger for debugging</param>
    /// <returns>Dictionary of authentication headers</returns>
    /// <exception cref="InvalidOperationException">When authentication configuration is invalid</exception>
    public static Dictionary<string, string> BuildAuthenticationHeaders(
        string authenticationType,
        string authenticationConfig,
        ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(authenticationType))
        {
            throw new ArgumentException("Authentication type cannot be null or empty", nameof(authenticationType));
        }

        if (string.IsNullOrEmpty(authenticationConfig))
        {
            throw new ArgumentException("Authentication config cannot be null or empty", nameof(authenticationConfig));
        }

        try
        {
            var authType = authenticationType.ToLowerInvariant();

            return authType switch
            {
                "bearer" => BuildBearerTokenHeaders(authenticationConfig, logger),
                "basic" => BuildBasicAuthHeaders(authenticationConfig, logger),
                "apikey" => BuildApiKeyHeaders(authenticationConfig, logger),
                _ => throw new InvalidOperationException(
                    $"Unsupported authentication type for headers: {authenticationType}. " +
                    $"Use BuildOAuth2Options for OAuth 2.0.")
            };
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to build authentication headers for type {AuthType}", authenticationType);
            throw new InvalidOperationException(
                $"Failed to configure authentication: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Builds OAuth 2.0 options for SDK's built-in OAuth support
    /// </summary>
    /// <param name="authenticationConfig">JSON configuration string</param>
    /// <param name="logger">Optional logger for debugging</param>
    /// <returns>ClientOAuthOptions for the SDK</returns>
    /// <exception cref="InvalidOperationException">When OAuth configuration is invalid</exception>
    public static ClientOAuthOptions BuildOAuth2Options(
        string authenticationConfig,
        ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(authenticationConfig))
        {
            throw new ArgumentException("OAuth configuration cannot be null or empty", nameof(authenticationConfig));
        }

        try
        {
            var authConfig = JsonSerializer.Deserialize<OAuth2AuthConfig>(
                authenticationConfig,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (authConfig == null)
            {
                throw new InvalidOperationException("Failed to deserialize OAuth 2.0 configuration");
            }

            // Validate required OAuth configuration
            if (string.IsNullOrEmpty(authConfig.ClientId))
            {
                throw new InvalidOperationException("OAuth 2.0 Client ID is required");
            }

            if (string.IsNullOrEmpty(authConfig.RedirectUri))
            {
                throw new InvalidOperationException("OAuth 2.0 Redirect URI is required");
            }

            logger?.LogDebug("Configuring OAuth 2.0 with Client ID: {ClientId}", authConfig.ClientId);

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
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to build OAuth 2.0 options");
            throw new InvalidOperationException(
                $"Failed to configure OAuth 2.0 authentication: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Checks if the authentication type requires OAuth 2.0
    /// </summary>
    public static bool IsOAuth2(string? authenticationType)
    {
        return !string.IsNullOrEmpty(authenticationType) &&
               authenticationType.Equals("oauth2", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if authentication is configured
    /// </summary>
    public static bool IsAuthenticationConfigured(string? authenticationType, string? authenticationConfig)
    {
        return !string.IsNullOrEmpty(authenticationType) && !string.IsNullOrEmpty(authenticationConfig);
    }

    #region Private Helper Methods

    /// <summary>
    /// Build Bearer Token authentication headers
    /// </summary>
    private static Dictionary<string, string> BuildBearerTokenHeaders(
        string authenticationConfig,
        ILogger? logger)
    {
        var authConfig = JsonSerializer.Deserialize<BearerTokenAuthConfig>(
            authenticationConfig,
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

        logger?.LogDebug("Applied Bearer token authentication to header {HeaderName}", headerName);

        return new Dictionary<string, string>
        {
            [headerName] = headerValue
        };
    }

    /// <summary>
    /// Build Basic authentication headers
    /// </summary>
    private static Dictionary<string, string> BuildBasicAuthHeaders(
        string authenticationConfig,
        ILogger? logger)
    {
        var authConfig = JsonSerializer.Deserialize<BasicAuthConfig>(
            authenticationConfig,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (authConfig == null ||
            string.IsNullOrEmpty(authConfig.Username) ||
            string.IsNullOrEmpty(authConfig.Password))
        {
            throw new InvalidOperationException("Username and password are required for Basic authentication");
        }

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{authConfig.Username}:{authConfig.Password}"));

        logger?.LogDebug("Applied Basic authentication for user {Username}", authConfig.Username);

        return new Dictionary<string, string>
        {
            ["Authorization"] = $"Basic {credentials}"
        };
    }

    /// <summary>
    /// Build API Key authentication headers
    /// </summary>
    private static Dictionary<string, string> BuildApiKeyHeaders(
        string authenticationConfig,
        ILogger? logger)
    {
        var authConfig = JsonSerializer.Deserialize<ApiKeyAuthConfig>(
            authenticationConfig,
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

        logger?.LogDebug("Applied API Key authentication to header {HeaderName}", authConfig.HeaderName);

        return new Dictionary<string, string>
        {
            [authConfig.HeaderName] = headerValue
        };
    }

    #endregion
}
