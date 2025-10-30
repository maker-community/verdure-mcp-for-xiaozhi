using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Verdure.McpPlatform.Api.Services;
using Verdure.McpPlatform.Application.Services;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServerAggregate;
using Verdure.McpPlatform.Infrastructure.Data;
using Verdure.McpPlatform.Infrastructure.Identity;
using Verdure.McpPlatform.Infrastructure.Repositories;

namespace Verdure.McpPlatform.Api.Extensions;

/// <summary>
/// Extension methods for configuring application services
/// </summary>
internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // Add HTTP Context Accessor
        services.AddHttpContextAccessor();

        // Add database context with Aspire PostgreSQL support
        var connectionString = configuration.GetConnectionString("mcpdb");
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            // Use Aspire PostgreSQL if available
            builder.AddNpgsqlDbContext<McpPlatformContext>("mcpdb");
        }
        else
        {
            // Fallback to SQLite for development
            services.AddDbContext<McpPlatformContext>(options =>
                options.UseSqlite("Data Source=mcpplatform.db"));
        }

        // Add Identity database context
        var identityConnectionString = configuration.GetConnectionString("identitydb");
        if (!string.IsNullOrEmpty(identityConnectionString))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(identityConnectionString));
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite("Data Source=identity.db"));
        }

        // Add Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Register repositories
        services.AddScoped<IMcpServerRepository, McpServerRepository>();

        // Register application services
        services.AddScoped<IMcpServerService, McpServerService>();
        services.AddScoped<IMcpBindingService, McpBindingService>();

        // Register identity service
        services.AddScoped<IIdentityService, IdentityService>();

        // Add CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }

    public static async Task ApplyDatabaseMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        
        var mcpContext = scope.ServiceProvider.GetRequiredService<McpPlatformContext>();
        await mcpContext.Database.EnsureCreatedAsync();

        var identityContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await identityContext.Database.EnsureCreatedAsync();
    }
}
