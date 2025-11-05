using Verdure.McpPlatform.Domain.SeedWork;

namespace Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiMcpEndpointAggregate;

/// <summary>
/// Repository interface for XiaozhiMcpEndpoint aggregate
/// Manages connections to Xiaozhi AI's MCP endpoints and their service bindings
/// </summary>
public interface IXiaozhiMcpEndpointRepository : IRepository<XiaozhiMcpEndpoint>
{
    XiaozhiMcpEndpoint Add(XiaozhiMcpEndpoint connection);
    void Update(XiaozhiMcpEndpoint connection);
    void Delete(XiaozhiMcpEndpoint connection);
    Task<XiaozhiMcpEndpoint?> GetAsync(string connectionId);
    Task<IEnumerable<XiaozhiMcpEndpoint>> GetByUserIdAsync(string userId);
    Task<IEnumerable<XiaozhiMcpEndpoint>> GetEnabledServersAsync(CancellationToken cancellationToken = default);
    Task<McpServiceBinding?> GetServiceBindingAsync(string bindingId);
    Task<IEnumerable<McpServiceBinding>> GetServiceBindingsByConnectionIdAsync(string connectionId);
    Task<IEnumerable<McpServiceBinding>> GetActiveServiceBindingsAsync();
}
