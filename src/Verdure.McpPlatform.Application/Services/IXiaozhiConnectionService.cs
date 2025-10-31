using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service interface for MCP Server operations
/// </summary>
public interface IXiaozhiConnectionService
{
    Task<XiaozhiConnectionDto> CreateAsync(CreateXiaozhiConnectionRequest request, string userId);
    Task<XiaozhiConnectionDto?> GetByIdAsync(int id, string userId);
    Task<IEnumerable<XiaozhiConnectionDto>> GetByUserAsync(string userId);
    Task UpdateAsync(int id, UpdateXiaozhiConnectionRequest request, string userId);
    Task DeleteAsync(int id, string userId);
    Task EnableAsync(int id, string userId);
    Task DisableAsync(int id, string userId);
}
