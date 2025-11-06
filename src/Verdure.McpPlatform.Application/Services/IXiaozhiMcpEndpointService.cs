using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Models;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service interface for MCP Server operations
/// </summary>
public interface IXiaozhiMcpEndpointService
{
    Task<XiaozhiMcpEndpointDto> CreateAsync(CreateXiaozhiMcpEndpointRequest request, string userId);
    Task<XiaozhiMcpEndpointDto?> GetByIdAsync(string id, string userId);
    Task<IEnumerable<XiaozhiMcpEndpointDto>> GetByUserAsync(string userId);
    Task<PagedResult<XiaozhiMcpEndpointDto>> GetByUserPagedAsync(string userId, PagedRequest request);
    Task UpdateAsync(string id, UpdateXiaozhiMcpEndpointRequest request, string userId);
    Task DeleteAsync(string id, string userId);
    Task EnableAsync(string id, string userId);
    Task DisableAsync(string id, string userId);
}
