using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Verdure.McpPlatform.Api.Services;
using Verdure.McpPlatform.Api.Services.BackgroundServices;
using Verdure.McpPlatform.Api.Services.ConnectionState;
using Verdure.McpPlatform.Api.Services.DistributedLock;
using Verdure.McpPlatform.Api.Services.WebSocket;
using Verdure.McpPlatform.Application.Services;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;
using Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiMcpEndpointAggregate;
using Verdure.McpPlatform.Infrastructure.Data;
using Verdure.McpPlatform.Infrastructure.Identity;
using Verdure.McpPlatform.Infrastructure.Repositories;
using Verdure.McpPlatform.Infrastructure.Services;

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

        // Add database contexts with configured provider
        builder.AddMcpPlatformDbContext("mcpdb");
        builder.AddIdentityDbContext("identitydb");

        // Add Identity (仅用于用户管理，不用于认证)
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Add Keycloak authentication with role mapping
        services.AddKeycloakAuthentication(configuration, builder.Environment);

        // Register repositories
        services.AddScoped<IXiaozhiMcpEndpointRepository, XiaozhiMcpEndpointRepository>();
        services.AddScoped<IMcpServiceConfigRepository, McpServiceConfigRepository>();

        // Register application services
        services.AddScoped<IXiaozhiMcpEndpointService, XiaozhiMcpEndpointService>();
        services.AddScoped<IMcpServiceBindingService, McpServiceBindingService>();
        services.AddScoped<IMcpServiceConfigService, McpServiceConfigService>();
        services.AddScoped<IMcpClientService, McpClientService>();

        // Register identity service
        services.AddScoped<IIdentityService, IdentityService>();

        // Register user info service (for displaying creator information)
        services.AddScoped<IUserInfoService, UserInfoService>();

        // Register user sync service
        services.AddScoped<IUserSyncService, UserSyncService>();

        // Register Redis connection
        var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
            ?? "localhost:6379";
        services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
        {
            var configuration = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
            configuration.AbortOnConnectFail = false;
            configuration.ConnectTimeout = 5000;
            configuration.SyncTimeout = 5000;
            return StackExchange.Redis.ConnectionMultiplexer.Connect(configuration);
        });

        // Register distributed services
        services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
        services.AddSingleton<IConnectionStateService, RedisConnectionStateService>();

        // Register WebSocket session manager as singleton
        services.AddSingleton<McpSessionManager>();

        // Register background services
        services.AddHostedService<ConnectionMonitorHostedService>();

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
