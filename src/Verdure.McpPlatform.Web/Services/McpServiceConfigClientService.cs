using System.Net.Http.Json;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Models;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Client service implementation for MCP Service Configuration management
/// </summary>
public class McpServiceConfigClientService : IMcpServiceConfigClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpServiceConfigClientService> _logger;

    public McpServiceConfigClientService(
        HttpClient httpClient,
        ILogger<McpServiceConfigClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<McpServiceConfigDto>> GetServicesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<McpServiceConfigDto>>(
                "api/mcp-services");
            return response ?? Enumerable.Empty<McpServiceConfigDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get MCP services");
            throw;
        }
    }

    public async Task<PagedResult<McpServiceConfigDto>> GetServicesPagedAsync(PagedRequest request)
    {
        try
        {
            var queryString = $"?Page={request.Page}&PageSize={request.PageSize}";
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                queryString += $"&SearchTerm={Uri.EscapeDataString(request.SearchTerm)}";
            }
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                queryString += $"&SortBy={request.SortBy}&SortOrder={request.SortOrder}";
            }

            var response = await _httpClient.GetFromJsonAsync<PagedResult<McpServiceConfigDto>>(
                $"api/mcp-services/paged{queryString}");
            return response ?? PagedResult<McpServiceConfigDto>.Empty(request.Page, request.PageSize);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get paged MCP services");
            throw;
        }
    }

    public async Task<IEnumerable<McpServiceConfigDto>> GetPublicServicesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<McpServiceConfigDto>>(
                "api/mcp-services/public");
            return response ?? Enumerable.Empty<McpServiceConfigDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get public MCP services");
            throw;
        }
    }

    public async Task<McpServiceConfigDto?> GetServiceAsync(string id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<McpServiceConfigDto>(
                $"api/mcp-services/{id}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get MCP service {ServiceId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<McpToolDto>> GetToolsAsync(string id)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<McpToolDto>>(
                $"api/mcp-services/{id}/tools");
            return response ?? Enumerable.Empty<McpToolDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get tools for MCP service {ServiceId}", id);
            throw;
        }
    }

    public async Task<McpServiceConfigDto> CreateServiceAsync(CreateMcpServiceConfigRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/mcp-services", request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<McpServiceConfigDto>();
            return result ?? throw new InvalidOperationException("Failed to create MCP service");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to create MCP service");
            throw;
        }
    }

    public async Task UpdateServiceAsync(string id, UpdateMcpServiceConfigRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/mcp-services/{id}", request);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to update MCP service {ServiceId}", id);
            throw;
        }
    }

    public async Task DeleteServiceAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/mcp-services/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to delete MCP service {ServiceId}", id);
            throw;
        }
    }

    public async Task SyncToolsAsync(string id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/mcp-services/{id}/sync-tools", null);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to sync tools for MCP service {ServiceId}", id);
            throw;
        }
    }
}
