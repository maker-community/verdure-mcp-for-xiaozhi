using System.Text.Json.Serialization;

namespace Verdure.McpPlatform.Contracts.Models;

/// <summary>
/// Base class for authentication configurations
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(BearerTokenAuthConfig), "bearer")]
[JsonDerivedType(typeof(BasicAuthConfig), "basic")]
[JsonDerivedType(typeof(ApiKeyAuthConfig), "apikey")]
[JsonDerivedType(typeof(OAuth2AuthConfig), "oauth2")]
public abstract class AuthenticationConfig
{
    /// <summary>
    /// Type of authentication (bearer, basic, apikey, oauth2)
    /// </summary>
    public abstract string Type { get; }
}

/// <summary>
/// Bearer Token Authentication Configuration
/// </summary>
public class BearerTokenAuthConfig : AuthenticationConfig
{
    public override string Type => "bearer";

    /// <summary>
    /// The bearer token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Custom header name (default: Authorization)
    /// </summary>
    public string? HeaderName { get; set; }
}

/// <summary>
/// Basic Authentication Configuration
/// </summary>
public class BasicAuthConfig : AuthenticationConfig
{
    public override string Type => "basic";

    /// <summary>
    /// Username for basic auth
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for basic auth
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// API Key Authentication Configuration
/// </summary>
public class ApiKeyAuthConfig : AuthenticationConfig
{
    public override string Type => "apikey";

    /// <summary>
    /// The API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Header name for the API key (e.g., X-API-Key)
    /// </summary>
    public string HeaderName { get; set; } = "X-API-Key";

    /// <summary>
    /// Optional: Prefix for the API key value (e.g., "ApiKey ")
    /// </summary>
    public string? Prefix { get; set; }
}

/// <summary>
/// OAuth 2.0 Authentication Configuration
/// </summary>
public class OAuth2AuthConfig : AuthenticationConfig
{
    public override string Type => "oauth2";

    /// <summary>
    /// OAuth client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth client secret (optional for PKCE)
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Authorization endpoint URL
    /// </summary>
    public string? AuthorizationEndpoint { get; set; }

    /// <summary>
    /// Token endpoint URL
    /// </summary>
    public string? TokenEndpoint { get; set; }

    /// <summary>
    /// Redirect URI for OAuth callback
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// OAuth scopes (space-separated)
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Grant type (authorization_code, client_credentials, etc.)
    /// </summary>
    public string GrantType { get; set; } = "authorization_code";

    /// <summary>
    /// Whether to use PKCE (Proof Key for Code Exchange)
    /// </summary>
    public bool UsePkce { get; set; } = true;

    /// <summary>
    /// Access token (stored after successful auth)
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Refresh token (stored after successful auth)
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token expiry time
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }
}
