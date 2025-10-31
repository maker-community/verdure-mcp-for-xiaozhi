using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Service for managing MCP servers through the API
/// </summary>
public interface IXiaozhiConnectionClientService
{
    Task<IEnumerable<XiaozhiConnectionDto>> GetServersAsync();
    Task<XiaozhiConnectionDto?> GetServerAsync(int id);
    Task<XiaozhiConnectionDto> CreateServerAsync(CreateXiaozhiConnectionRequest request);
    Task UpdateServerAsync(int id, UpdateXiaozhiConnectionRequest request);
    Task DeleteServerAsync(int id);
    Task EnableServerAsync(int id);
    Task DisableServerAsync(int id);
}
