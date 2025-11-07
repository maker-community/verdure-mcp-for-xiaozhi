using Verdure.McpPlatform.Domain.SeedWork;

namespace Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;

/// <summary>
/// Repository interface for MCP Service Configuration aggregate
/// </summary>
public interface IMcpServiceConfigRepository : IRepository<McpServiceConfig>
{
    McpServiceConfig Add(McpServiceConfig mcpServiceConfig);
    void Update(McpServiceConfig mcpServiceConfig);
    Task<McpServiceConfig?> GetByIdAsync(string id);
    Task<IEnumerable<McpServiceConfig>> GetByIdsAsync(IEnumerable<string> ids);
    Task<IEnumerable<McpServiceConfig>> GetByUserAsync(string userId);
    Task<(IEnumerable<McpServiceConfig> Items, int TotalCount)> GetByUserPagedAsync(
        string userId,
        int skip,
        int take,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = true);
    Task<IEnumerable<McpServiceConfig>> GetPublicServicesAsync();
    Task<(IEnumerable<McpServiceConfig> Items, int TotalCount)> GetPublicServicesPagedAsync(
        int skip,
        int take,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = true);
    Task<bool> DeleteAsync(string id);
    Task<IEnumerable<McpTool>> GetToolsByUserIdAsync(string userId);
    Task<IEnumerable<McpTool>> GetToolsByServiceConfigIdAsync(string serviceConfigId);
}
