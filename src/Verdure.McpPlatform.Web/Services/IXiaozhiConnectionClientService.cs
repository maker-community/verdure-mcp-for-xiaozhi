using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Service for managing MCP servers through the API
/// </summary>
public interface IXiaozhiConnectionClientService
{
    Task<IEnumerable<XiaozhiConnectionDto>> GetServersAsync();
    Task<XiaozhiConnectionDto?> GetServerAsync(string id);
    Task<XiaozhiConnectionDto> CreateServerAsync(CreateXiaozhiConnectionRequest request);
    Task UpdateServerAsync(string id, UpdateXiaozhiConnectionRequest request);
    Task DeleteServerAsync(string id);
    Task EnableServerAsync(string id);
    Task DisableServerAsync(string id);
}
