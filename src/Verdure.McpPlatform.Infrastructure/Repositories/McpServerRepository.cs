using Microsoft.EntityFrameworkCore;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServerAggregate;
using Verdure.McpPlatform.Domain.SeedWork;
using Verdure.McpPlatform.Infrastructure.Data;

namespace Verdure.McpPlatform.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for McpServer aggregate
/// </summary>
public class McpServerRepository : IMcpServerRepository
{
    private readonly McpPlatformContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public McpServerRepository(McpPlatformContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public McpServer Add(McpServer server)
    {
        return _context.McpServers.Add(server).Entity;
    }

    public void Update(McpServer server)
    {
        _context.Entry(server).State = EntityState.Modified;
    }

    public void Delete(McpServer server)
    {
        _context.McpServers.Remove(server);
    }

    public async Task<McpServer?> GetAsync(int serverId)
    {
        var server = await _context.McpServers
            .Include(s => s.Bindings)
            .FirstOrDefaultAsync(s => s.Id == serverId);

        return server;
    }

    public async Task<IEnumerable<McpServer>> GetByUserIdAsync(string userId)
    {
        return await _context.McpServers
            .AsNoTracking()
            .Include(s => s.Bindings)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<McpBinding?> GetBindingAsync(int bindingId)
    {
        return await _context.McpBindings
            .FirstOrDefaultAsync(b => b.Id == bindingId);
    }

    public async Task<IEnumerable<McpBinding>> GetBindingsByServerIdAsync(int serverId)
    {
        return await _context.McpBindings
            .AsNoTracking()
            .Where(b => b.McpServerId == serverId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<McpBinding>> GetActiveBindingsAsync()
    {
        return await _context.McpBindings
            .AsNoTracking()
            .Where(b => b.IsActive)
            .ToListAsync();
    }
}
