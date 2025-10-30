using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Verdure.McpPlatform.Api.Services;
using Verdure.McpPlatform.Api.Settings;
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

        var oidcSettings = new OidcSettings();
        configuration.GetSection("Oidc").Bind(oidcSettings);

        if (!oidcSettings.IsValid())
        {
            throw new InvalidOperationException("OIDC configuration is invalid. Please check Authority, Realm, and ClientId settings.");
        }

        services.Configure<OidcSettings>(configuration.GetSection("Oidc"));

        var issuer = oidcSettings.GetIssuerUrl();

        // Add JWT Bearer Authentication（设置为默认认证方案）
        services.AddAuthentication(options =>
         {
             // 设置 JWT Bearer 为默认认证和挑战方案
             options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
             options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
             options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
         })
         .AddJwtBearer(options =>
         {
             options.Authority = issuer;
             options.Audience = oidcSettings.Audience;
             options.RequireHttpsMetadata = builder.Environment.IsDevelopment() ? oidcSettings.RequireHttpsMetadata : true;

             options.TokenValidationParameters = new TokenValidationParameters
             {
                 ValidateIssuer = true,
                 ValidateAudience = !string.IsNullOrEmpty(oidcSettings.Audience),
                 ValidateLifetime = true,
                 ValidateIssuerSigningKey = true,
                 ValidIssuer = issuer,
                 ValidAudience = oidcSettings.Audience,
                 ClockSkew = TimeSpan.FromMinutes(oidcSettings.ClockSkewMinutes),
                 // 映射标准Claims
                 NameClaimType = ClaimTypes.Name,
                 RoleClaimType = ClaimTypes.Role
             };

             // 配置事件处理
             options.Events = new JwtBearerEvents
             {
                 OnTokenValidated = context =>
                 {
                     return Task.CompletedTask;
                 },
                 OnAuthenticationFailed = context =>
                 {
                     // 记录认证失败的详细信息
                     var logger = context.HttpContext.RequestServices
                         .GetRequiredService<ILogger<Program>>();
                     logger.LogWarning("JWT authentication failed: {Message}", context.Exception.Message);
                     return Task.CompletedTask;
                 },
                 OnChallenge = context =>
                 {
                     // 阻止默认的重定向行为，直接返回 401
                     context.HandleResponse();
                     context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                     context.Response.ContentType = "application/json";
                     
                     var result = System.Text.Json.JsonSerializer.Serialize(new
                     {
                         error = "Unauthorized",
                         message = "Invalid or missing JWT token"
                     });
                     
                     return context.Response.WriteAsync(result);
                 }
             };
         });

        services.AddAuthorization();

        // Register repositories
        services.AddScoped<IMcpServerRepository, McpServerRepository>();

        // Register application services
        services.AddScoped<IMcpServerService, McpServerService>();
        services.AddScoped<IMcpBindingService, McpBindingService>();

        // Register identity service
        services.AddScoped<IIdentityService, IdentityService>();

        // Register WebSocket session manager as singleton
        services.AddSingleton<Verdure.McpPlatform.Api.Services.WebSocket.McpSessionManager>();

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
