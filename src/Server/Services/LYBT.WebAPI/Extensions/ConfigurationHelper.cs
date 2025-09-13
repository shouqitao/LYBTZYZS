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
    /// 获取管理员默认密码 - 统一从DefaultPasswordOptions获取
    /// 优先级: ADMIN_DEFAULT_PASSWORD环境变量 -> DefaultPasswords配置节 -> 安全默认值
    /// </summary>
    public static string GetAdminPassword(IConfiguration configuration)
    {
        // 优先使用环境变量
        var envPassword = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD");
        if (!string.IsNullOrEmpty(envPassword))
        {
            return envPassword;
        }

        // 统一从DefaultPasswords配置节获取
        var defaultPassword = configuration["DefaultPasswords:SystemAdmin"];
        if (!string.IsNullOrEmpty(defaultPassword))
        {
            return defaultPassword;
        }

        // 向后兼容：尝试读取旧配置路径
        var legacyPassword = configuration["SysAdminOptions:DefaultPassword"];
        if (!string.IsNullOrEmpty(legacyPassword))
        {
            return legacyPassword;
        }

        // 安全默认值
        return "LybtAdmin2025@SecurePass!";
    }

    /// <summary>
    /// 获取用户默认密码 - 统一从DefaultPasswordOptions获取
    /// 优先级: USER_DEFAULT_PASSWORD环境变量 -> DefaultPasswords配置节 -> 安全默认值
    /// </summary>
    public static string GetUserDefaultPassword(IConfiguration configuration)
    {
        // 优先使用环境变量
        var envPassword = Environment.GetEnvironmentVariable("USER_DEFAULT_PASSWORD");
        if (!string.IsNullOrEmpty(envPassword))
        {
            return envPassword;
        }

        // 统一从DefaultPasswords配置节获取
        var defaultPassword = configuration["DefaultPasswords:NewUser"];
        if (!string.IsNullOrEmpty(defaultPassword))
        {
            return defaultPassword;
        }

        // 向后兼容：尝试读取旧配置路径
        var legacyPassword = configuration["UserOptions:DefaultUserPassword"];
        if (!string.IsNullOrEmpty(legacyPassword))
        {
            return legacyPassword;
        }

        // 安全默认值
        return "LybtUser2025#InitPass!";
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
