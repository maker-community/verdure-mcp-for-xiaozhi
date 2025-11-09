using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Custom authorization message handler that configures itself automatically
/// </summary>
public class CustomAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public CustomAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation,
        IConfiguration configuration)
        : base(provider, navigation)
    {
        var configured = configuration["ApiBaseAddress"];
        
        // Treat empty or whitespace ApiBaseAddress as unset so we use the NavigationManager's BaseUri
        var apiBaseAddress = string.IsNullOrWhiteSpace(configured) 
            ? navigation.BaseUri.TrimEnd('/') 
            : configured.TrimEnd('/');

        // Only configure if we have a valid base address
        if (!string.IsNullOrWhiteSpace(apiBaseAddress))
        {
            ConfigureHandler(
                authorizedUrls: new[] { apiBaseAddress },
                scopes: new[] { "openid", "profile", "email" }
            );
        }
    }
}
