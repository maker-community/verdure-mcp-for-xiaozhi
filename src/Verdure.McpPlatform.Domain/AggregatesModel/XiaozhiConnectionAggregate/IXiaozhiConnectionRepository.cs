using Verdure.McpPlatform.Domain.SeedWork;

namespace Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiConnectionAggregate;

/// <summary>
/// Repository interface for XiaozhiConnection aggregate
/// Manages connections to Xiaozhi AI's MCP endpoints and their service bindings
/// </summary>
public interface IXiaozhiConnectionRepository : IRepository<XiaozhiConnection>
{
    XiaozhiConnection Add(XiaozhiConnection connection);
    void Update(XiaozhiConnection connection);
    void Delete(XiaozhiConnection connection);
    Task<XiaozhiConnection?> GetAsync(string connectionId);
    Task<IEnumerable<XiaozhiConnection>> GetByUserIdAsync(string userId);
    Task<McpServiceBinding?> GetServiceBindingAsync(string bindingId);
    Task<IEnumerable<McpServiceBinding>> GetServiceBindingsByConnectionIdAsync(string connectionId);
    Task<IEnumerable<McpServiceBinding>> GetActiveServiceBindingsAsync();
}
