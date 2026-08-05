using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Options.Server;

namespace LYBT.Infrastructure.Configuration.Security;

/// <summary>
/// 运行时配置写入白名单策略
/// 只允许修改已声明 SectionName 的 Server 端 Options 对应的配置节，
/// 敏感配置项（连接字符串、JWT 密钥、默认密码）一律禁止修改
/// </summary>
public static class ConfigurationWritePolicy
{
    // 允许修改的配置节：Server 端已注册 Options 的 SectionName
    private static readonly HashSet<string> AllowedSections = new(StringComparer.OrdinalIgnoreCase)
    {
        AppInfoOptions.SectionName,       // App
        CorsOptions.SectionName,          // Cors
        DatabaseOptions.SectionName,      // Database
        DesktopUpdateOptions.SectionName, // DesktopUpdate
        JwtOptions.SectionName,           // Jwt
        LoggingOptions.SectionName,       // Logging
        MemoryCacheOptions.SectionName,   // MemoryCache
        SecurityOptions.SectionName,      // Security
        SessionOptions.SectionName,       // Session
        SwaggerOptions.SectionName,       // Swagger
        SystemAdminOptions.SectionName,   // SystemAdmin
    };

    // 精确禁止的配置键（即使所属节在白名单内也禁止，如 Jwt:SecretKey）
    private static readonly HashSet<string> ForbiddenKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "ConnectionStrings:DefaultConnection",
        "Jwt:SecretKey",
    };

    // 整节禁止（默认密码涉及明文凭据）
    private static readonly HashSet<string> ForbiddenSections = new(StringComparer.OrdinalIgnoreCase)
    {
        DefaultPasswordOptions.SectionName, // DefaultPasswords
    };

    /// <summary>
    /// 判断配置键是否允许运行时修改
    /// </summary>
    public static bool IsAllowed(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var section = key.Contains(':') ? key[..key.IndexOf(':')] : key;
        if (!AllowedSections.Contains(section))
            return false;
        if (ForbiddenSections.Contains(section))
            return false;
        return !ForbiddenKeys.Contains(key);
    }
}
