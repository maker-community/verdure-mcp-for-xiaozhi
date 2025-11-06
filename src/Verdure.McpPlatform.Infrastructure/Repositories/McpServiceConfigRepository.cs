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

    public async Task<(IEnumerable<McpServiceConfig> Items, int TotalCount)> GetByUserPagedAsync(
        string userId,
        int skip,
        int take,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = true)
    {
        var query = _context.McpServiceConfigs
            .AsNoTracking()
            .Include(s => s.Tools)
            .Where(s => s.UserId == userId);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(searchLower) ||
                s.Endpoint.ToLower().Contains(searchLower) ||
                (s.Description != null && s.Description.ToLower().Contains(searchLower)));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = sortBy?.ToLower() switch
        {
            "name" => sortDescending
                ? query.OrderByDescending(s => s.Name)
                : query.OrderBy(s => s.Name),
            "endpoint" => sortDescending
                ? query.OrderByDescending(s => s.Endpoint)
                : query.OrderBy(s => s.Endpoint),
            "createdat" => sortDescending
                ? query.OrderByDescending(s => s.CreatedAt)
                : query.OrderBy(s => s.CreatedAt),
            "lastsynced" => sortDescending
                ? query.OrderByDescending(s => s.LastSyncedAt)
                : query.OrderBy(s => s.LastSyncedAt),
            _ => sortDescending
                ? query.OrderByDescending(s => s.CreatedAt)
                : query.OrderBy(s => s.CreatedAt)
        };

        // Apply pagination
        var items = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return (items, totalCount);
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
