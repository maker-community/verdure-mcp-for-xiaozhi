using Microsoft.EntityFrameworkCore;
using Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiConnectionAggregate;
using Verdure.McpPlatform.Domain.SeedWork;

namespace Verdure.McpPlatform.Infrastructure.Data;

/// <summary>
/// MCP Platform database context
/// </summary>
public class McpPlatformContext : DbContext, IUnitOfWork
{
    public DbSet<XiaozhiConnection> XiaozhiConnections { get; set; }
    public DbSet<McpServiceBinding> McpServiceBindings { get; set; }

    public McpPlatformContext(DbContextOptions<McpPlatformContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(McpPlatformContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await SaveChangesAsync(cancellationToken);
        return true;
    }
}
