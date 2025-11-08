using System.Net.Http.Json;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// User client service implementation
/// </summary>
public class UserClientService : IUserClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserClientService> _logger;

    public UserClientService(
        HttpClient httpClient,
        ILogger<UserClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserSyncResult> SyncCurrentUserAsync()
    {
        try
        {
            _logger.LogInformation("Syncing current user to Identity database");

            var response = await _httpClient.PostAsync("api/users/sync", null);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UserSyncResult>();
                
                if (result != null)
                {
                    _logger.LogInformation(
                        "User sync successful: UserId={UserId}, IsNewUser={IsNewUser}",
                        result.UserId, result.IsNewUser);
                    
                    return result;
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "User sync failed: StatusCode={StatusCode}, Content={Content}",
                response.StatusCode, errorContent);

            return new UserSyncResult
            {
                Success = false,
                Message = $"Sync failed: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user");
            return new UserSyncResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public async Task<CurrentUserInfo?> GetCurrentUserAsync()
    {
        try
        {
            _logger.LogDebug("Getting current user information");

            var response = await _httpClient.GetAsync("api/users/me");

            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<CurrentUserInfo>();
                _logger.LogDebug("Got user information: UserId={UserId}", user?.UserId);
                return user;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("User not found in Identity database, needs sync");
                return null;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Failed to get user info: StatusCode={StatusCode}, Content={Content}",
                response.StatusCode, errorContent);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }
}
