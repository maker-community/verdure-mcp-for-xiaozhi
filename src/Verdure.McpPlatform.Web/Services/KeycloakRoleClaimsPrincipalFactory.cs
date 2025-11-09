using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Custom AccountClaimsPrincipalFactory for mapping Keycloak roles to standard claims
/// Maps resource_access.<clientId>.roles from ACCESS TOKEN to ClaimTypes.Role
/// </summary>
public class KeycloakRoleClaimsPrincipalFactory : AccountClaimsPrincipalFactory<RemoteUserAccount>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakRoleClaimsPrincipalFactory> _logger;
    private readonly IAccessTokenProviderAccessor _accessor;

    public KeycloakRoleClaimsPrincipalFactory(
        IAccessTokenProviderAccessor accessor,
        IConfiguration configuration,
        ILogger<KeycloakRoleClaimsPrincipalFactory> logger)
        : base(accessor)
    {
        _configuration = configuration;
        _logger = logger;
        _accessor = accessor;
    }

    public override async ValueTask<ClaimsPrincipal> CreateUserAsync(
        RemoteUserAccount account,
        RemoteAuthenticationUserOptions options)
    {
        // Create the base user principal from ID token
        var user = await base.CreateUserAsync(account, options);

        if (user.Identity is ClaimsIdentity identity && identity.IsAuthenticated)
        {
            // Get ClientId from configuration
            var clientId = _configuration["Oidc:ClientId"] ?? "verdure-mcp";

            _logger.LogInformation(
                "🔐 Mapping Keycloak roles for user {UserId} with ClientId {ClientId}",
                identity.Name,
                clientId);

            // 🆕 从 ACCESS TOKEN 中读取角色信息
            await MapRolesFromAccessTokenAsync(identity, clientId);
            
            // Log all mapped roles for debugging
            var roles = identity.FindAll(identity.RoleClaimType).Select(c => c.Value).ToList();
            if (roles.Any())
            {
                _logger.LogInformation(
                    "✅ User {UserId} authenticated with roles: {Roles}",
                    identity.Name,
                    string.Join(", ", roles));
            }
            else
            {
                _logger.LogWarning(
                    "⚠️ User {UserId} has no roles mapped - check ClientId configuration and Token structure",
                    identity.Name);
            }
        }

        return user;
    }

    /// <summary>
    /// 从 Access Token 中读取并映射角色信息
    /// </summary>
    private async Task MapRolesFromAccessTokenAsync(ClaimsIdentity identity, string clientId)
    {
        try
        {
            _logger.LogInformation("� Requesting access token to extract roles...");
            
            // 获取 Access Token
            var tokenProvider = _accessor.TokenProvider;
            var tokenResult = await tokenProvider.RequestAccessToken();
            
            if (!tokenResult.TryGetToken(out var accessToken))
            {
                _logger.LogWarning("❌ Failed to get access token");
                return;
            }

            _logger.LogInformation("✅ Access token obtained, parsing...");
            
            // 解析 JWT Access Token
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken.Value))
            {
                _logger.LogError("❌ Access token is not a valid JWT");
                return;
            }

            var jwtToken = handler.ReadJwtToken(accessToken.Value);
            
            _logger.LogInformation("� Access Token Claims:");
            foreach (var claim in jwtToken.Claims.Take(20)) // 限制前20个避免日志过长
            {
                var value = claim.Value.Length > 200 
                    ? claim.Value.Substring(0, 200) + "..." 
                    : claim.Value;
                _logger.LogInformation("  - {Type}: {Value}", claim.Type, value);
            }
            
            // 从 Access Token 的 Claims 中提取角色
            MapKeycloakRolesToStandardRoles(identity, jwtToken.Claims, clientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error extracting roles from access token");
        }
    }

    /// <summary>
    /// Map Keycloak resource_access roles to ASP.NET Core standard roles
    /// </summary>
    private void MapKeycloakRolesToStandardRoles(
        ClaimsIdentity identity, 
        IEnumerable<Claim> accessTokenClaims, 
        string clientId)
    {
        try
        {
            _logger.LogInformation("🔍 Looking for resource_access claim in access token...");
            
            // Find the resource_access claim from access token
            var resourceAccessClaim = accessTokenClaims.FirstOrDefault(c => c.Type == "resource_access");
            
            if (resourceAccessClaim == null)
            {
                _logger.LogWarning("❌ No 'resource_access' claim found in access token");
                
                // 打印所有可用的 claim 类型
                var allClaimTypes = accessTokenClaims.Select(c => c.Type).Distinct().ToList();
                _logger.LogInformation("📝 Available claim types in access token: {ClaimTypes}", 
                    string.Join(", ", allClaimTypes));
                
                _logger.LogError("❌ resource_access not found in access token");
                return;
            }

            _logger.LogInformation("✅ Found resource_access claim in access token");

            var resourceAccessValue = resourceAccessClaim.Value;
            if (string.IsNullOrWhiteSpace(resourceAccessValue))
            {
                _logger.LogWarning("❌ resource_access claim is empty");
                return;
            }

            _logger.LogInformation("📄 resource_access JSON: {Json}", resourceAccessValue);

            // Parse the resource_access JSON
            var resourceAccess = JsonDocument.Parse(resourceAccessValue);
            
            _logger.LogInformation("🔑 Looking for ClientId '{ClientId}' in resource_access...", clientId);

            // Try to get roles from the configured clientId
            if (resourceAccess.RootElement.TryGetProperty(clientId, out var clientResource))
            {
                _logger.LogInformation("✅ Found ClientId '{ClientId}' in resource_access", clientId);
                
                if (clientResource.TryGetProperty("roles", out var rolesElement))
                {
                    _logger.LogInformation("✅ Found 'roles' property in resource_access.{ClientId}", clientId);
                    
                    var roles = rolesElement.EnumerateArray()
                        .Select(r => r.GetString())
                        .Where(r => !string.IsNullOrEmpty(r))
                        .ToList();

                    _logger.LogInformation("📋 Extracted {Count} roles: {Roles}", 
                        roles.Count, 
                        string.Join(", ", roles));

                    if (roles.Any())
                    {
                        _logger.LogInformation(
                            "🎯 Mapping {Count} roles from resource_access.{ClientId}: {Roles}",
                            roles.Count,
                            clientId,
                            string.Join(", ", roles));

                        // Add each role as a standard role claim
                        foreach (var role in roles)
                        {
                            // Use the role claim type from the identity
                            identity.AddClaim(new Claim(identity.RoleClaimType, role!));
                            _logger.LogInformation("➕ Added role claim: {Role} (ClaimType: {ClaimType})", 
                                role, 
                                identity.RoleClaimType);
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "⚠️ No roles found in resource_access.{ClientId} - roles array is empty",
                            clientId);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "❌ No 'roles' property found in resource_access.{ClientId}",
                        clientId);
                    
                    // 打印可用的属性
                    var availableProps = string.Join(", ", 
                        clientResource.EnumerateObject().Select(p => p.Name));
                    _logger.LogInformation("📝 Available properties in resource_access.{ClientId}: {Props}",
                        clientId,
                        availableProps);
                }
            }
            else
            {
                var availableClients = string.Join(", ", 
                    resourceAccess.RootElement.EnumerateObject().Select(p => p.Name));
                    
                _logger.LogWarning(
                    "❌ ClientId '{ClientId}' not found in resource_access. Available clients: {Clients}",
                    clientId,
                    availableClients);
                
                _logger.LogWarning(
                    "💡 Tip: Check if Oidc:ClientId in appsettings.json matches one of the available clients");
            }

            // Also map realm_access roles if needed (optional)
            MapRealmRoles(identity, accessTokenClaims);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "❌ Failed to parse resource_access claim for role mapping - Invalid JSON");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Unexpected error during role mapping");
        }
    }

    /// <summary>
    /// Map realm_access roles (optional)
    /// Filters out default Keycloak roles
    /// </summary>
    private void MapRealmRoles(ClaimsIdentity identity, IEnumerable<Claim> accessTokenClaims)
    {
        try
        {
            _logger.LogInformation("🔍 Looking for realm_access roles...");
            
            var realmAccessClaim = accessTokenClaims.FirstOrDefault(c => c.Type == "realm_access");
            
            if (realmAccessClaim == null || string.IsNullOrWhiteSpace(realmAccessClaim.Value))
            {
                _logger.LogInformation("ℹ️ No realm_access claim found (optional)");
                return;
            }

            _logger.LogInformation("📄 realm_access JSON: {Json}", realmAccessClaim.Value);

            var realmAccess = JsonDocument.Parse(realmAccessClaim.Value);
            
            if (realmAccess.RootElement.TryGetProperty("roles", out var rolesElement))
            {
                var allRealmRoles = rolesElement.EnumerateArray()
                    .Select(r => r.GetString())
                    .Where(r => !string.IsNullOrEmpty(r))
                    .ToList();
                
                _logger.LogInformation("📋 Found {Count} realm roles (before filtering): {Roles}",
                    allRealmRoles.Count,
                    string.Join(", ", allRealmRoles));
                
                var realmRoles = allRealmRoles
                    .Where(r => IsRelevantRealmRole(r!))
                    .ToList();

                _logger.LogInformation("✅ {Count} relevant realm roles (after filtering): {Roles}",
                    realmRoles.Count,
                    string.Join(", ", realmRoles));

                foreach (var role in realmRoles)
                {
                    // Only add if not already present
                    if (!identity.HasClaim(identity.RoleClaimType, role!))
                    {
                        identity.AddClaim(new Claim(identity.RoleClaimType, role!));
                        
                        _logger.LogInformation(
                            "➕ Added realm role: {Role}",
                            role);
                    }
                    else
                    {
                        _logger.LogDebug(
                            "⏭️ Skipped realm role (already exists): {Role}",
                            role);
                    }
                }
            }
            else
            {
                _logger.LogWarning("❌ No 'roles' property found in realm_access");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Error mapping realm roles");
        }
    }

    /// <summary>
    /// Determine if a realm role should be mapped
    /// Filters out default Keycloak roles
    /// </summary>
    private static bool IsRelevantRealmRole(string role)
    {
        var excludedRoles = new[]
        {
            "offline_access",
            "uma_authorization",
            "default-roles-maker-community"
        };

        return !excludedRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
