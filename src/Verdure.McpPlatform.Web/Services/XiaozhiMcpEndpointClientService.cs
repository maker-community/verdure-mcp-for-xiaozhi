using System.Net.Http.Json;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Implementation of MCP server client service
/// </summary>
public class XiaozhiMcpEndpointClientService : IXiaozhiMcpEndpointClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<XiaozhiMcpEndpointClientService> _logger;
    private const string ApiEndpoint = "api/xiaozhi-mcp-endpoints";

    public XiaozhiMcpEndpointClientService(
        HttpClient httpClient,
        ILogger<XiaozhiMcpEndpointClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<XiaozhiMcpEndpointDto>> GetServersAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<XiaozhiMcpEndpointDto>>(ApiEndpoint);
            return response ?? Enumerable.Empty<XiaozhiMcpEndpointDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get MCP servers");
            throw;
        }
    }

    public async Task<XiaozhiMcpEndpointDto?> GetServerAsync(string id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<XiaozhiMcpEndpointDto>($"{ApiEndpoint}/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get MCP server {ServerId}", id);
            throw;
        }
    }

    public async Task<XiaozhiMcpEndpointDto> CreateServerAsync(CreateXiaozhiMcpEndpointRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiEndpoint, request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<XiaozhiMcpEndpointDto>() 
                ?? throw new InvalidOperationException("Failed to deserialize server response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to create MCP server");
            throw;
        }
    }

    public async Task UpdateServerAsync(string id, UpdateXiaozhiMcpEndpointRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ApiEndpoint}/{id}", request);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to update MCP server {ServerId}", id);
            throw;
        }
    }

    public async Task DeleteServerAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{ApiEndpoint}/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to delete MCP server {ServerId}", id);
            throw;
        }
    }

    public async Task EnableServerAsync(string id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{ApiEndpoint}/{id}/enable", null);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to enable MCP server {ServerId}", id);
            throw;
        }
    }

    public async Task DisableServerAsync(string id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{ApiEndpoint}/{id}/disable", null);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to disable MCP server {ServerId}", id);
            throw;
        }
    }
}
