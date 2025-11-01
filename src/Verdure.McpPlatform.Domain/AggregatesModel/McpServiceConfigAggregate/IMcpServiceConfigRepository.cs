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
    Task<IEnumerable<McpServiceConfig>> GetByUserAsync(string userId);
    Task<IEnumerable<McpServiceConfig>> GetPublicServicesAsync();
    Task<bool> DeleteAsync(string id);
}
