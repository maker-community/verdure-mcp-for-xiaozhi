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
        var apiBaseAddress = configuration["ApiBaseAddress"] ?? navigation.BaseUri;
        
        ConfigureHandler(
            authorizedUrls: new[] { apiBaseAddress },
            scopes: new[] { "openid", "profile", "email" }
        );
    }
}
