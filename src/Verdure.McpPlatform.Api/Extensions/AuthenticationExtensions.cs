using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;
using Verdure.McpPlatform.Api.Settings;

namespace Verdure.McpPlatform.Api.Extensions;

/// <summary>
/// Extension methods for configuring authentication with Keycloak role mapping
/// </summary>
internal static class AuthenticationExtensions
{
    /// <summary>
    /// Add JWT Bearer authentication with Keycloak role mapping support
    /// </summary>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var oidcSettings = new OidcSettings();
        configuration.GetSection("Oidc").Bind(oidcSettings);

        if (!oidcSettings.IsValid())
        {
            throw new InvalidOperationException(
                "OIDC configuration is invalid. Please check Authority, Realm, and ClientId settings.");
        }

        services.Configure<OidcSettings>(configuration.GetSection("Oidc"));

        var issuer = oidcSettings.GetIssuerUrl();

        // Build list of valid issuers to support both internal (container) and external (localhost) access
        var validIssuers = new List<string> { issuer };
        
        // Add localhost variant for browser-based tokens
        // 容器内: http://keycloak:8080 -> 浏览器: http://localhost:8180
        if (issuer.Contains("keycloak:8080"))
        {
            var localhostIssuer = issuer.Replace("keycloak:8080", "localhost:8180");
            validIssuers.Add(localhostIssuer);
        }
        // Add keycloak variant for container-based tokens
        // 浏览器: http://localhost:8180 -> 容器内: http://keycloak:8080
        else if (issuer.Contains("localhost:8180"))
        {
            var keycloakIssuer = issuer.Replace("localhost:8180", "keycloak:8080");
            validIssuers.Add(keycloakIssuer);
        }

        // Log authentication configuration for debugging
        var logger = services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
        logger.LogInformation("=== Keycloak Authentication Configuration ===");
        logger.LogInformation("Authority: {Authority}", issuer);
        logger.LogInformation("Realm: {Realm}", oidcSettings.Realm);
        logger.LogInformation("ClientId: {ClientId}", oidcSettings.ClientId);
        logger.LogInformation("Audience: {Audience}", oidcSettings.Audience ?? "(not set)");
        logger.LogInformation("RequireHttpsMetadata: {RequireHttps}", oidcSettings.RequireHttpsMetadata);
        logger.LogInformation("Valid Issuers ({Count}):", validIssuers.Count);
        for (int i = 0; i < validIssuers.Count; i++)
        {
            logger.LogInformation("  [{Index}] {Issuer}", i, validIssuers[i]);
        }
        logger.LogInformation("============================================");

        // Add JWT Bearer Authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = issuer;
            options.Audience = oidcSettings.Audience;
            options.RequireHttpsMetadata = oidcSettings.RequireHttpsMetadata;
         
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = !string.IsNullOrEmpty(oidcSettings.Audience),
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuers = validIssuers, // 支持多个 Issuer
                ValidAudience = oidcSettings.Audience,
                ClockSkew = TimeSpan.FromMinutes(oidcSettings.ClockSkewMinutes),
                // Map standard claims
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };

            // Configure event handlers with role mapping
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    return MapKeycloakRolesToStandardRoles(context, oidcSettings.ClientId);
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();
                    
                    // 尝试解析 token 中的 issuer
                    var tokenIssuer = "Unknown";
                    try
                    {
                        var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
                        if (!string.IsNullOrEmpty(token))
                        {
                            // 简单解析 JWT payload (不验证签名)
                            var parts = token.Split('.');
                            if (parts.Length >= 2)
                            {
                                var payload = System.Text.Encoding.UTF8.GetString(
                                    Convert.FromBase64String(parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=')));
                                var json = JsonDocument.Parse(payload);
                                if (json.RootElement.TryGetProperty("iss", out var iss))
                                {
                                    tokenIssuer = iss.GetString() ?? "Unknown";
                                }
                            }
                        }
                    }
                    catch { /* Ignore parsing errors */ }
                    
                    logger.LogWarning(
                        "JWT authentication failed: {Message}\n" +
                        "Authority: {Authority}\n" +
                        "MetadataAddress: {MetadataAddress}\n" +
                        "Token Issuer (iss): {TokenIssuer}\n" +
                        "Valid Issuers: {ValidIssuers}\n" +
                        "Exception: {Exception}", 
                        context.Exception.Message,
                        options.Authority,
                        options.MetadataAddress,
                        tokenIssuer,
                        string.Join(", ", validIssuers),
                        context.Exception);
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    // Prevent default redirect behavior, return 401 directly
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var result = JsonSerializer.Serialize(new
                    {
                        error = "Unauthorized",
                        message = "Invalid or missing JWT token"
                    });

                    return context.Response.WriteAsync(result);
                }
            };
        });

        // Add authorization policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy("UserOrAdmin", policy =>
                policy.RequireRole("User", "Admin"));
        });

        return services;
    }

    /// <summary>
    /// Map Keycloak resource_access roles to ASP.NET Core standard roles
    /// </summary>
    private static Task MapKeycloakRolesToStandardRoles(
        TokenValidatedContext context,
        string clientId)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
        {
            return Task.CompletedTask;
        }

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        try
        {
            // Extract resource_access claim
            var resourceAccessClaim = identity.FindFirst("resource_access")?.Value;
            
            if (string.IsNullOrEmpty(resourceAccessClaim))
            {
                logger.LogDebug("No resource_access claim found in token");
                return Task.CompletedTask;
            }

            // Parse resource_access JSON
            var resourceAccess = JsonDocument.Parse(resourceAccessClaim);

            // Try to get roles from the configured clientId
            if (resourceAccess.RootElement.TryGetProperty(clientId, out var clientResource))
            {
                if (clientResource.TryGetProperty("roles", out var rolesElement))
                {
                    var roles = rolesElement.EnumerateArray()
                        .Select(r => r.GetString())
                        .Where(r => !string.IsNullOrEmpty(r))
                        .ToList();

                    if (roles.Any())
                    {
                        logger.LogInformation(
                            "Mapping {Count} roles from resource_access.{ClientId}: {Roles}",
                            roles.Count,
                            clientId,
                            string.Join(", ", roles));

                        // Add each role as a standard ClaimTypes.Role claim
                        foreach (var role in roles)
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, role!));
                        }
                    }
                    else
                    {
                        logger.LogDebug(
                            "No roles found in resource_access.{ClientId}", 
                            clientId);
                    }
                }
            }
            else
            {
                logger.LogWarning(
                    "ClientId '{ClientId}' not found in resource_access. Available clients: {Clients}",
                    clientId,
                    string.Join(", ", resourceAccess.RootElement.EnumerateObject().Select(p => p.Name)));
            }

            // Also map realm_access roles if needed (optional)
            var realmAccessClaim = identity.FindFirst("realm_access")?.Value;
            if (!string.IsNullOrEmpty(realmAccessClaim))
            {
                var realmAccess = JsonDocument.Parse(realmAccessClaim);
                if (realmAccess.RootElement.TryGetProperty("roles", out var realmRoles))
                {
                    foreach (var role in realmRoles.EnumerateArray())
                    {
                        var roleValue = role.GetString();
                        if (!string.IsNullOrEmpty(roleValue) && 
                            !identity.HasClaim(ClaimTypes.Role, roleValue))
                        {
                            // Only add specific realm roles (avoid default Keycloak roles)
                            if (IsRelevantRealmRole(roleValue))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                                logger.LogDebug(
                                    "Added realm role: {Role}", 
                                    roleValue);
                            }
                        }
                    }
                }
            }

            // Log all mapped roles for debugging
            var allRoles = identity.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
            
            if (allRoles.Any())
            {
                logger.LogInformation(
                    "User {UserId} authenticated with roles: {Roles}",
                    identity.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown",
                    string.Join(", ", allRoles));
            }
        }
        catch (JsonException ex)
        {
            logger.LogError(
                ex,
                "Failed to parse resource_access claim for role mapping");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error during role mapping");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Determine if a realm role should be mapped to ASP.NET Core roles
    /// </summary>
    private static bool IsRelevantRealmRole(string role)
    {
        // Filter out default Keycloak roles that are not relevant for authorization
        var excludedRoles = new[]
        {
            "offline_access",
            "uma_authorization",
            "default-roles-maker-community"
        };

        return !excludedRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
