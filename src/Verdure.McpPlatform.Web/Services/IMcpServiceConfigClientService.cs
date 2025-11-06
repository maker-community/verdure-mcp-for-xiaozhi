using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Models;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Client service interface for MCP Service Configuration management
/// </summary>
public interface IMcpServiceConfigClientService
{
    /// <summary>
    /// Get all MCP services for the current user
    /// </summary>
    Task<IEnumerable<McpServiceConfigDto>> GetServicesAsync();

    /// <summary>
    /// Get paged MCP services for the current user
    /// </summary>
    Task<PagedResult<McpServiceConfigDto>> GetServicesPagedAsync(PagedRequest request);

    /// <summary>
    /// Get all public MCP services (available for other users to use)
    /// </summary>
    Task<IEnumerable<McpServiceConfigDto>> GetPublicServicesAsync();

    /// <summary>
    /// Get a specific MCP service by ID
    /// </summary>
    Task<McpServiceConfigDto?> GetServiceAsync(string id);

    /// <summary>
    /// Get all tools for a specific MCP service
    /// </summary>
    Task<IEnumerable<McpToolDto>> GetToolsAsync(string id);

    /// <summary>
    /// Create a new MCP service configuration
    /// </summary>
    Task<McpServiceConfigDto> CreateServiceAsync(CreateMcpServiceConfigRequest request);

    /// <summary>
    /// Update an existing MCP service configuration
    /// </summary>
    Task UpdateServiceAsync(string id, UpdateMcpServiceConfigRequest request);

    /// <summary>
    /// Delete an MCP service configuration
    /// </summary>
    Task DeleteServiceAsync(string id);

    /// <summary>
    /// Sync tools from the MCP service endpoint
    /// </summary>
    Task SyncToolsAsync(string id);
}
