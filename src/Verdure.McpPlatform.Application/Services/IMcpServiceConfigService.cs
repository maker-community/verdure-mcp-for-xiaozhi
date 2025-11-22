using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Models;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service interface for MCP Service Configuration operations
/// </summary>
public interface IMcpServiceConfigService
{
    Task<McpServiceConfigDto> CreateAsync(CreateMcpServiceConfigRequest request, string userId);
    Task<McpServiceConfigDto?> GetByIdAsync(string id, string userId);
    Task<IEnumerable<McpServiceConfigDto>> GetByUserAsync(string userId);
    Task<PagedResult<McpServiceConfigDto>> GetByUserPagedAsync(string userId, PagedRequest request);
    Task<IEnumerable<McpServiceConfigDto>> GetPublicServicesAsync();
    Task<PagedResult<McpServiceConfigDto>> GetPublicServicesPagedAsync(PagedRequest request);
    Task UpdateAsync(string id, UpdateMcpServiceConfigRequest request, string userId);
    Task DeleteAsync(string id, string userId);
    Task SyncToolsAsync(string id, string userId);
    Task<IEnumerable<McpToolDto>> GetToolsAsync(string serviceId, string userId);
    
    /// <summary>
    /// Sync tools for all MCP services - Admin only
    /// Returns list of failed service names
    /// </summary>
    Task<SyncAllToolsResultDto> SyncAllToolsAsync();
    
    /// <summary>
    /// Set a service as public - Admin only
    /// </summary>
    Task SetPublicAsync(string id, string adminUserId);
    
    /// <summary>
    /// Set a service as private - Admin only
    /// </summary>
    Task SetPrivateAsync(string id, string adminUserId);
}
