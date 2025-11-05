using Microsoft.EntityFrameworkCore;
using Verdure.McpPlatform.Infrastructure.Data;
using Verdure.McpPlatform.Infrastructure.Identity;

namespace Verdure.McpPlatform.Api.Extensions;

/// <summary>
/// Extension methods for configuring database providers
/// </summary>
internal static class DatabaseExtensions
{
    /// <summary>
    /// Add McpPlatformContext with configured database provider
    /// </summary>
    public static IServiceCollection AddMcpPlatformDbContext(
        this IHostApplicationBuilder builder,
        string connectionStringName = "mcpdb")
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        var provider = configuration["Database:Provider"] ?? "SQLite";
        var connectionString = configuration.GetConnectionString(connectionStringName);

        switch (provider.ToUpperInvariant())
        {
            case "POSTGRESQL":
            case "POSTGRES":
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException(
                        $"Connection string '{connectionStringName}' is required for PostgreSQL provider.");
                }
                
                // Use Aspire PostgreSQL support if available
                try
                {
                    builder.AddNpgsqlDbContext<McpPlatformContext>(connectionStringName);
                }
                catch
                {
                    // Fallback to direct PostgreSQL if Aspire is not available
                    services.AddDbContext<McpPlatformContext>((serviceProvider, options) =>
                    {
                        var config = serviceProvider.GetRequiredService<IConfiguration>();
                        options.UseNpgsql(connectionString);
                    });
                }
                break;

            case "SQLITE":
                var sqliteConnectionString = connectionString ?? "Data Source=Data/mcpplatform.db";
                services.AddDbContext<McpPlatformContext>((serviceProvider, options) =>
                {
                    var config = serviceProvider.GetRequiredService<IConfiguration>();
                    options.UseSqlite(sqliteConnectionString);
                });
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported database provider: {provider}. " +
                    $"Supported providers: PostgreSQL, SQLite");
        }

        return services;
    }

    /// <summary>
    /// Add ApplicationDbContext (Identity) with configured database provider
    /// </summary>
    public static IServiceCollection AddIdentityDbContext(
        this IHostApplicationBuilder builder,
        string connectionStringName = "identitydb")
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        var provider = configuration["Database:Provider"] ?? "SQLite";
        var connectionString = configuration.GetConnectionString(connectionStringName);

        switch (provider.ToUpperInvariant())
        {
            case "POSTGRESQL":
            case "POSTGRES":
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException(
                        $"Connection string '{connectionStringName}' is required for PostgreSQL provider.");
                }
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(connectionString));
                break;

            case "SQLITE":
                var sqliteConnectionString = connectionString ?? "Data Source=Data/identity.db";
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite(sqliteConnectionString));
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported database provider: {provider}. " +
                    $"Supported providers: PostgreSQL, SQLite");
        }

        return services;
    }

    /// <summary>
    /// Get the configured database provider name
    /// </summary>
    public static string GetDatabaseProvider(this IConfiguration configuration)
    {
        return configuration["Database:Provider"] ?? "SQLite";
    }

    /// <summary>
    /// Check if the configured provider is PostgreSQL
    /// </summary>
    public static bool IsPostgreSQL(this IConfiguration configuration)
    {
        var provider = configuration.GetDatabaseProvider().ToUpperInvariant();
        return provider == "POSTGRESQL" || provider == "POSTGRES";
    }

    /// <summary>
    /// Check if the configured provider is SQLite
    /// </summary>
    public static bool IsSQLite(this IConfiguration configuration)
    {
        return configuration.GetDatabaseProvider().ToUpperInvariant() == "SQLITE";
    }
}
