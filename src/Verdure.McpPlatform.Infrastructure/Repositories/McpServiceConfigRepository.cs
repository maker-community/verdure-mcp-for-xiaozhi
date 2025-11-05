using Microsoft.EntityFrameworkCore;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;
using Verdure.McpPlatform.Domain.SeedWork;
using Verdure.McpPlatform.Infrastructure.Data;

namespace Verdure.McpPlatform.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for McpServiceConfig aggregate
/// </summary>
public class McpServiceConfigRepository : IMcpServiceConfigRepository
{
    private readonly McpPlatformContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public McpServiceConfigRepository(McpPlatformContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public McpServiceConfig Add(McpServiceConfig mcpServiceConfig)
    {
        return _context.McpServiceConfigs.Add(mcpServiceConfig).Entity;
    }

    public void Update(McpServiceConfig mcpServiceConfig)
    {
        _context.Entry(mcpServiceConfig).State = EntityState.Modified;
    }

    public async Task<McpServiceConfig?> GetByIdAsync(string id)
    {
        var config = await _context.McpServiceConfigs
            .Include(s => s.Tools)
            .FirstOrDefaultAsync(s => s.Id == id);

        return config;
    }

    public async Task<IEnumerable<McpServiceConfig>> GetByUserAsync(string userId)
    {
        return await _context.McpServiceConfigs
            .AsNoTracking()
            .Include(s => s.Tools)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<McpServiceConfig>> GetPublicServicesAsync()
    {
        return await _context.McpServiceConfigs
            .AsNoTracking()
            .Include(s => s.Tools)
            .Where(s => s.IsPublic)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var config = await _context.McpServiceConfigs.FindAsync(id);
        if (config == null)
            return false;

        _context.McpServiceConfigs.Remove(config);
        return true;
    }

    public async Task<IEnumerable<McpTool>> GetToolsByUserIdAsync(string userId)
    {
        return await _context.McpTools
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<McpTool>> GetToolsByServiceConfigIdAsync(string serviceConfigId)
    {
        return await _context.McpTools
            .AsNoTracking()
            .Where(t => t.McpServiceConfigId == serviceConfigId)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }
}
