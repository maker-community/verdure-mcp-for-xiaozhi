using Microsoft.EntityFrameworkCore;
using Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiMcpEndpointAggregate;
using Verdure.McpPlatform.Domain.SeedWork;
using Verdure.McpPlatform.Infrastructure.Data;

namespace Verdure.McpPlatform.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for XiaozhiMcpEndpoint aggregate
/// </summary>
public class XiaozhiMcpEndpointRepository : IXiaozhiMcpEndpointRepository
{
    private readonly McpPlatformContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public XiaozhiMcpEndpointRepository(McpPlatformContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public XiaozhiMcpEndpoint Add(XiaozhiMcpEndpoint connection)
    {
        return _context.XiaozhiMcpEndpoints.Add(connection).Entity;
    }

    public void Update(XiaozhiMcpEndpoint connection)
    {
        _context.Entry(connection).State = EntityState.Modified;
    }

    public void Delete(XiaozhiMcpEndpoint connection)
    {
        _context.XiaozhiMcpEndpoints.Remove(connection);
    }

    public async Task<XiaozhiMcpEndpoint?> GetAsync(string connectionId)
    {
        var connection = await _context.XiaozhiMcpEndpoints
            .Include(s => s.ServiceBindings)
            .FirstOrDefaultAsync(s => s.Id == connectionId);

        return connection;
    }

    public async Task<IEnumerable<XiaozhiMcpEndpoint>> GetByUserIdAsync(string userId)
    {
        return await _context.XiaozhiMcpEndpoints
            .AsNoTracking()
            .Include(s => s.ServiceBindings)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<XiaozhiMcpEndpoint>> GetEnabledServersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.XiaozhiMcpEndpoints
            .AsNoTracking()
            .Include(s => s.ServiceBindings)
            .Where(s => s.IsEnabled)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<McpServiceBinding?> GetServiceBindingAsync(string bindingId)
    {
        return await _context.McpServiceBindings
            .FirstOrDefaultAsync(b => b.Id == bindingId);
    }

    public async Task<IEnumerable<McpServiceBinding>> GetServiceBindingsByConnectionIdAsync(string connectionId)
    {
        return await _context.McpServiceBindings
            .AsNoTracking()
            .Where(b => b.XiaozhiMcpEndpointId == connectionId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<McpServiceBinding>> GetServiceBindingsByUserIdAsync(string userId)
    {
        return await _context.McpServiceBindings
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<McpServiceBinding>> GetActiveServiceBindingsAsync()
    {
        return await _context.McpServiceBindings
            .AsNoTracking()
            .Where(b => b.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<McpServiceBinding>> GetActiveServiceBindingsByUserIdAsync(string userId)
    {
        return await _context.McpServiceBindings
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.IsActive)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }
}
