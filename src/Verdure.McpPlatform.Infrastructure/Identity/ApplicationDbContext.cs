using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Verdure.McpPlatform.Infrastructure.Identity;

/// <summary>
/// Application database context for Identity
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IConfiguration? _configuration;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IConfiguration? configuration = null)
        : base(options)
    {
        _configuration = configuration;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply global table name prefix from configuration
        var tablePrefix = _configuration?["Database:TablePrefix"] ?? "verdure_";
        ApplyTableNamePrefix(builder, tablePrefix);
    }

    /// <summary>
    /// Apply a prefix to all table names, removing default 'AspNet' prefix
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    /// <param name="prefix">The prefix to add (e.g., "verdure_")</param>
    private static void ApplyTableNamePrefix(ModelBuilder modelBuilder, string prefix)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Get the current table name
            var tableName = entityType.GetTableName();
            
            if (!string.IsNullOrEmpty(tableName))
            {
                // Remove 'AspNet' prefix if it exists (default Identity tables)
                if (tableName.StartsWith("AspNet"))
                {
                    tableName = tableName.Substring(6); // Remove "AspNet"
                }
                
                // Add our custom prefix if not already present
                if (!tableName.StartsWith(prefix))
                {
                    entityType.SetTableName(prefix + tableName);
                }
            }
        }
    }
}
