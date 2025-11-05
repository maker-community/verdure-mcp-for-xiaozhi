using Microsoft.EntityFrameworkCore;
using Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiConnectionAggregate;
using Verdure.McpPlatform.Domain.SeedWork;
using Verdure.McpPlatform.Infrastructure.Data;

namespace Verdure.McpPlatform.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for XiaozhiConnection aggregate
/// </summary>
public class XiaozhiConnectionRepository : IXiaozhiConnectionRepository
{
    private readonly McpPlatformContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public XiaozhiConnectionRepository(McpPlatformContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public XiaozhiConnection Add(XiaozhiConnection connection)
    {
        return _context.XiaozhiConnections.Add(connection).Entity;
    }

    public void Update(XiaozhiConnection connection)
    {
        _context.Entry(connection).State = EntityState.Modified;
    }

    public void Delete(XiaozhiConnection connection)
    {
        _context.XiaozhiConnections.Remove(connection);
    }

    public async Task<XiaozhiConnection?> GetAsync(string connectionId)
    {
        var connection = await _context.XiaozhiConnections
            .Include(s => s.ServiceBindings)
            .FirstOrDefaultAsync(s => s.Id == connectionId);

        return connection;
    }

    public async Task<IEnumerable<XiaozhiConnection>> GetByUserIdAsync(string userId)
    {
        return await _context.XiaozhiConnections
            .AsNoTracking()
            .Include(s => s.ServiceBindings)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<XiaozhiConnection>> GetEnabledServersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.XiaozhiConnections
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
            .Where(b => b.XiaozhiConnectionId == connectionId)
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
}
