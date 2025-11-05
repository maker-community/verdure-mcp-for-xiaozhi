using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Service for managing MCP servers through the API
/// </summary>
public interface IXiaozhiMcpEndpointClientService
{
    Task<IEnumerable<XiaozhiMcpEndpointDto>> GetServersAsync();
    Task<XiaozhiMcpEndpointDto?> GetServerAsync(string id);
    Task<XiaozhiMcpEndpointDto> CreateServerAsync(CreateXiaozhiMcpEndpointRequest request);
    Task UpdateServerAsync(string id, UpdateXiaozhiMcpEndpointRequest request);
    Task DeleteServerAsync(string id);
    Task EnableServerAsync(string id);
    Task DisableServerAsync(string id);
}
