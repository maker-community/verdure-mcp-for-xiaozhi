using System.Net.Http.Json;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Implementation of MCP binding client service
/// </summary>
public class McpBindingClientService : IMcpBindingClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpBindingClientService> _logger;
    private const string ApiEndpoint = "api/mcp-bindings";

    public McpBindingClientService(
        HttpClient httpClient,
        ILogger<McpBindingClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<McpBindingDto>> GetBindingsByServerAsync(int serverId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<McpBindingDto>>(
                $"{ApiEndpoint}/server/{serverId}");
            return response ?? Enumerable.Empty<McpBindingDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get bindings for server {ServerId}", serverId);
            throw;
        }
    }

    public async Task<IEnumerable<McpBindingDto>> GetActiveBindingsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<McpBindingDto>>(
                $"{ApiEndpoint}/active");
            return response ?? Enumerable.Empty<McpBindingDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get active bindings");
            throw;
        }
    }

    public async Task<McpBindingDto?> GetBindingAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<McpBindingDto>($"{ApiEndpoint}/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get binding {BindingId}", id);
            throw;
        }
    }

    public async Task<McpBindingDto> CreateBindingAsync(CreateMcpBindingRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiEndpoint, request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<McpBindingDto>() 
                ?? throw new InvalidOperationException("Failed to deserialize binding response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to create binding");
            throw;
        }
    }

    public async Task UpdateBindingAsync(int id, UpdateMcpBindingRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ApiEndpoint}/{id}", request);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to update binding {BindingId}", id);
            throw;
        }
    }

    public async Task ActivateBindingAsync(int id)
    {
        try
        {
            var response = await _httpClient.PutAsync($"{ApiEndpoint}/{id}/activate", null);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to activate binding {BindingId}", id);
            throw;
        }
    }

    public async Task DeactivateBindingAsync(int id)
    {
        try
        {
            var response = await _httpClient.PutAsync($"{ApiEndpoint}/{id}/deactivate", null);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to deactivate binding {BindingId}", id);
            throw;
        }
    }

    public async Task DeleteBindingAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{ApiEndpoint}/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to delete binding {BindingId}", id);
            throw;
        }
    }
}
