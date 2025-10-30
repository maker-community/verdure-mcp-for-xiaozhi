namespace Verdure.McpPlatform.Api.Settings;

/// <summary>
/// OIDC认证配置
/// </summary>
public class OidcSettings
{
    /// <summary>
    /// OIDC Authority URL (例如: https://auth.verdure-hiro.cn)
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// OIDC Realm (例如: maker-community)
    /// </summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// OIDC Client ID (例如: aiforge)
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// 是否自动创建用户
    /// </summary>
    public bool AutoCreateUser { get; set; } = true;

    /// <summary>
    /// 用户角色映射配置
    /// </summary>
    public Dictionary<string, string> RoleMapping { get; set; } = new()
    {
        { "admin", "Admin" },
        { "administrator", "Admin" },
        { "super-admin", "Root" },
        { "root", "Root" },
        { "user", "User" },
        { "client", "User" }
    };

    /// <summary>
    /// 时钟偏移容忍度（分钟）
    /// </summary>
    public int ClockSkewMinutes { get; set; } = 5;

    /// <summary>
    /// 是否在开发环境中要求HTTPS
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// 获取完整的Issuer URL
    /// </summary>
    public string GetIssuerUrl()
    {
        if (string.IsNullOrEmpty(Authority) || string.IsNullOrEmpty(Realm))
            return string.Empty;
            
        return $"{Authority.TrimEnd('/')}/realms/{Realm}";
    }

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Authority) && 
               !string.IsNullOrEmpty(Realm) && 
               !string.IsNullOrEmpty(ClientId);
    }
}
