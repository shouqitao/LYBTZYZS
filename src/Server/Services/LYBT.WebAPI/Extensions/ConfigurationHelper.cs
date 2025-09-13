namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 配置帮助类 - 统一配置获取方法
/// </summary>
/// <remarks>
/// 消除重复的配置获取方法，统一配置读取逻辑
/// 支持环境变量优先级策略
/// </remarks>
public static class ConfigurationHelper
{
    /// <summary>
    /// 获取数据库连接字符串
    /// 优先级: CONNECTION_STRING环境变量 -> 配置文件
    /// </summary>
    public static string GetConnectionString(IConfiguration configuration, string name = "DefaultConnection")
    {
        // 优先使用环境变量
        var envConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            return envConnectionString;
        }

        // 使用配置文件
        return configuration.GetConnectionString(name) ?? string.Empty;
    }

    /// <summary>
    /// 获取JWT密钥
    /// 优先级: JWT_SECRET环境变量 -> 配置文件 -> 开发环境默认值
    /// </summary>
    public static string GetJwtSecret(IConfiguration configuration)
    {
        // 优先使用环境变量
        var envSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
        if (!string.IsNullOrEmpty(envSecret))
        {
            return envSecret;
        }

        // 使用配置文件
        var configSecret = configuration["JwtOptions:Secret"];
        if (!string.IsNullOrEmpty(configSecret))
        {
            return configSecret;
        }

        // 开发环境默认值
        return "DefaultDevelopmentSecretKeyForJWTAuthentication_ShouldBeReplacedInProduction";
    }

    /// <summary>
    /// 获取管理员默认密码
    /// 优先级: ADMIN_DEFAULT_PASSWORD环境变量 -> 配置文件
    /// </summary>
    public static string GetAdminPassword(IConfiguration configuration)
    {
        // 优先使用环境变量
        var envPassword = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD");
        if (!string.IsNullOrEmpty(envPassword))
        {
            return envPassword;
        }

        // 使用配置文件
        return configuration["SysAdminOptions:DefaultPassword"] ?? "LybtAdmin2025@SecurePass!";
    }

    /// <summary>
    /// 获取用户默认密码
    /// 优先级: USER_DEFAULT_PASSWORD环境变量 -> 配置文件
    /// </summary>
    public static string GetUserDefaultPassword(IConfiguration configuration)
    {
        // 优先使用环境变量
        var envPassword = Environment.GetEnvironmentVariable("USER_DEFAULT_PASSWORD");
        if (!string.IsNullOrEmpty(envPassword))
        {
            return envPassword;
        }

        // 使用配置文件
        return configuration["UserOptions:DefaultUserPassword"] ?? "LybtUser2025#InitPass!";
    }

    /// <summary>
    /// 获取配置节并绑定到强类型对象
    /// </summary>
    public static T GetConfigurationSection<T>(IConfiguration configuration, string sectionName) where T : class, new()
    {
        var section = configuration.GetSection(sectionName);
        var config = new T();
        section.Bind(config);
        return config;
    }
}