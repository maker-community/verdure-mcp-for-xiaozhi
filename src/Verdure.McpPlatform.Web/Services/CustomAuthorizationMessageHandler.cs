using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Net;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Custom authorization message handler that configures itself automatically
/// and handles authentication expiration by redirecting to login
/// </summary>
public class CustomAuthorizationMessageHandler : AuthorizationMessageHandler
{
    private readonly NavigationManager _navigation;

    public CustomAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation,
        IConfiguration configuration)
        : base(provider, navigation)
    {
        _navigation = navigation;
        
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

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            // If we get 401 Unauthorized, redirect to login
            // This handles token expiration gracefully
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Force logout and redirect to login page
                _navigation.NavigateTo("authentication/login", forceLoad: true);
            }

            return response;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            // Token is not available (expired or missing)
            // Redirect to login page for re-authentication
            ex.Redirect();
            throw;
        }
    }
}
