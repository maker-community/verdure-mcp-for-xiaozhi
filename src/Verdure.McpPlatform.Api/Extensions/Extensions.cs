using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
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

        // Add JWT Bearer Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var jwtKey = configuration["Jwt:Key"];
            var jwtIssuer = configuration["Jwt:Issuer"];
            var jwtAudience = configuration["Jwt:Audience"];

            if (!string.IsNullOrEmpty(jwtKey))
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
                    ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(jwtKey))
                };
            }
            else
            {
                // For development: accept any valid JWT token
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = false,
                    SignatureValidator = (token, parameters) =>
                    {
                        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token);
                        return jwt;
                    }
                };
            }

            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
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
