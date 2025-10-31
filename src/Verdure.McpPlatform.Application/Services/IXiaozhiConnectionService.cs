using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service interface for MCP Server operations
/// </summary>
public interface IXiaozhiConnectionService
{
    Task<XiaozhiConnectionDto> CreateAsync(CreateXiaozhiConnectionRequest request, string userId);
    Task<XiaozhiConnectionDto?> GetByIdAsync(string id, string userId);
    Task<IEnumerable<XiaozhiConnectionDto>> GetByUserAsync(string userId);
    Task UpdateAsync(string id, UpdateXiaozhiConnectionRequest request, string userId);
    Task DeleteAsync(string id, string userId);
    Task EnableAsync(string id, string userId);
    Task DisableAsync(string id, string userId);
}
