using System.Net.Http.Json;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Implementation of MCP binding client service
/// </summary>
public class McpServiceBindingClientService : IMcpServiceBindingClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpServiceBindingClientService> _logger;
    private const string ApiEndpoint = "api/mcp-bindings";

    public McpServiceBindingClientService(
        HttpClient httpClient,
        ILogger<McpServiceBindingClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<McpServiceBindingDto>> GetBindingsByServerAsync(string serverId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<McpServiceBindingDto>>(
                $"{ApiEndpoint}/server/{serverId}");
            return response ?? Enumerable.Empty<McpServiceBindingDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get bindings for server {ServerId}", serverId);
            throw;
        }
    }

    public async Task<IEnumerable<McpServiceBindingDto>> GetActiveBindingsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<McpServiceBindingDto>>(
                $"{ApiEndpoint}/active");
            return response ?? Enumerable.Empty<McpServiceBindingDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get active bindings");
            throw;
        }
    }

    public async Task<McpServiceBindingDto?> GetBindingAsync(string id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<McpServiceBindingDto>($"{ApiEndpoint}/{id}");
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

    public async Task<McpServiceBindingDto> CreateBindingAsync(CreateMcpServiceBindingRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiEndpoint, request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<McpServiceBindingDto>() 
                ?? throw new InvalidOperationException("Failed to deserialize binding response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to create binding");
            throw;
        }
    }

    public async Task UpdateBindingAsync(string id, UpdateMcpServiceBindingRequest request)
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

    public async Task ActivateBindingAsync(string id)
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

    public async Task DeactivateBindingAsync(string id)
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

    public async Task DeleteBindingAsync(string id)
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
