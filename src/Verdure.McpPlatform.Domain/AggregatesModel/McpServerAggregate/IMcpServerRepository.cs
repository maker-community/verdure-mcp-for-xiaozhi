using Verdure.McpPlatform.Domain.SeedWork;

namespace Verdure.McpPlatform.Domain.AggregatesModel.McpServerAggregate;

/// <summary>
/// Repository interface for McpServer aggregate
/// </summary>
public interface IMcpServerRepository : IRepository<McpServer>
{
    McpServer Add(McpServer server);
    void Update(McpServer server);
    void Delete(McpServer server);
    Task<McpServer?> GetAsync(int serverId);
    Task<IEnumerable<McpServer>> GetByUserIdAsync(string userId);
    Task<McpBinding?> GetBindingAsync(int bindingId);
    Task<IEnumerable<McpBinding>> GetBindingsByServerIdAsync(int serverId);
    Task<IEnumerable<McpBinding>> GetActiveBindingsAsync();
}
