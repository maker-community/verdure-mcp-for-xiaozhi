using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiMcpEndpointAggregate;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;
using Verdure.McpPlatform.Domain.SeedWork;

namespace Verdure.McpPlatform.Infrastructure.Data;

/// <summary>
/// MCP Platform database context
/// </summary>
public class McpPlatformContext : DbContext, IUnitOfWork
{
    private readonly IConfiguration? _configuration;

    public DbSet<XiaozhiMcpEndpoint> XiaozhiMcpEndpoints { get; set; }
    public DbSet<McpServiceBinding> McpServiceBindings { get; set; }
    public DbSet<McpServiceConfig> McpServiceConfigs { get; set; }
    public DbSet<McpTool> McpTools { get; set; }

    public McpPlatformContext(
        DbContextOptions<McpPlatformContext> options,
        IConfiguration? configuration = null)
        : base(options)
    {
        _configuration = configuration;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(McpPlatformContext).Assembly);
        
        // Apply global table name prefix from configuration
        var tablePrefix = _configuration?["Database:TablePrefix"] ?? "verdure_";
        ApplyTableNamePrefix(modelBuilder, tablePrefix);
        
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Apply a prefix to all table names
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    /// <param name="prefix">The prefix to add (e.g., "verdure_")</param>
    private static void ApplyTableNamePrefix(ModelBuilder modelBuilder, string prefix)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Get the current table name
            var tableName = entityType.GetTableName();
            
            if (!string.IsNullOrEmpty(tableName) && !tableName.StartsWith(prefix))
            {
                // Add prefix to table name
                entityType.SetTableName(prefix + tableName);
            }
        }
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await SaveChangesAsync(cancellationToken);
        return true;
    }
}
