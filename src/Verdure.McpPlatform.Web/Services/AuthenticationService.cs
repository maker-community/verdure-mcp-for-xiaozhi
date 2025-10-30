using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Implementation of authentication service using local storage for token management
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;
    private const string TokenKey = "authToken";

    public AuthenticationService(
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider)
    {
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task LoginAsync(string token)
    {
        await _localStorage.SetItemAsync(TokenKey, token);
        
        if (_authStateProvider is CustomAuthenticationStateProvider provider)
        {
            await provider.NotifyUserAuthenticationAsync(token);
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        
        if (_authStateProvider is CustomAuthenticationStateProvider provider)
        {
            provider.NotifyUserLogout();
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>(TokenKey);
    }
}
