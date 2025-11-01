using System.ComponentModel.DataAnnotations;
using LYBT.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LYBT.Infrastructure.Configuration.Extensions;

/// <summary>
/// 配置注册扩展方法
/// 简化统一配置选项的注册和验证
/// </summary>
/// <summary>
/// 配置注册扩展方法
/// 简化统一配置选项的注册和验证
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// 注册凌隐宝堂系统统一配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLybtConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册统一配置
        services.Configure<LybtOptions>(configuration.GetSection(LybtOptions.SectionName));

        // 注册各子配置选项（向后兼容）
        RegisterLegacyCompatibilityOptions(services, configuration);

        // 添加配置验证
        services.AddConfigurationValidation<LybtOptions>();

        return services;
    }

    /// <summary>
    /// 获取凌隐宝堂配置选项
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <returns>配置选项</returns>
    public static LybtOptions GetLybtOptions(this IConfiguration configuration)
    {
        var options = new LybtOptions();
        configuration.GetSection(LybtOptions.SectionName).Bind(options);
        return options;
    }

    /// <summary>
    /// 验证配置选项
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <returns>验证结果</returns>
    public static ConfigurationValidationResult ValidateLybtConfiguration(this IConfiguration configuration)
    {
        var options = configuration.GetLybtOptions();
        var validationResults = new List<string>();

        // 验证必填项
        ValidateRequiredSettings(options, validationResults);

        // 验证业务逻辑
        ValidateBusinessLogic(options, validationResults);

        return new ConfigurationValidationResult
        {
            IsValid = validationResults.Count == 0,
            Errors = validationResults
        };
    }

    /// <summary>
    /// 注册传统兼容性配置选项
    /// 用于向后兼容，逐步迁移到统一配置
    /// </summary>
    private static void RegisterLegacyCompatibilityOptions(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var lybtOptions = configuration.GetLybtOptions();

        // 注册传统 AuthOptions
        services.Configure<AuthOptions>(opt =>
        {
            opt.MaxFailedLoginAttempts = lybtOptions.Security.IpSecurity.FailedAttemptsThreshold;
            opt.AccountLockoutDuration = TimeSpan.FromMinutes(lybtOptions.Security.IpSecurity.LockoutDurationMinutes);
            opt.EnableDetailedLoginLogging = true; // 默认启用
            opt.SupportedLoginTypes = new List<string> { "Password" };
            opt.PasswordPolicy = MapToLegacyPasswordPolicy(lybtOptions.Authentication.PasswordPolicy);
            opt.SessionOptions = MapToLegacySessionOptions(lybtOptions.Authentication.Session);
        });

        // 注册传统 JwtOptions
        services.Configure<JwtOptions>(opt =>
        {
#pragma warning disable CS0618 // 类型或成员已过时
            opt.Secret = lybtOptions.Authentication.Jwt.SecretKey;
#pragma warning restore CS0618 // 类型或成员已过时
            opt.Issuer = lybtOptions.Authentication.Jwt.Issuer;
            opt.Audience = lybtOptions.Authentication.Jwt.Audience;
            opt.ExpireMinutes = lybtOptions.Authentication.Jwt.AccessTokenExpirationMinutes;
            opt.RememberMeExpireMinutes = lybtOptions.Authentication.Jwt.RememberMeExpirationDays * 1440;
            opt.ClockSkewSeconds = 300; // Default 5 minutes
        });

        // 注册传统 DatabaseOptions
        services.Configure<DatabaseOptions>(opt =>
        {
            opt.EnableAutoMigration = lybtOptions.Infrastructure.Database.Migration.AutoMigrate;
            opt.EnableSensitiveDataLogging = lybtOptions.Infrastructure.Database.Monitoring.LogAllQueries;
            opt.EnableDetailedErrors = true; // 默认启用
            opt.CommandTimeout = lybtOptions.Infrastructure.Database.ConnectionPool.CommandTimeoutSeconds;
            opt.ConnectionPool = MapToLegacyConnectionPoolOptions(lybtOptions.Infrastructure.Database.ConnectionPool);
            opt.Monitoring = MapToLegacyDatabaseMonitoringOptions(lybtOptions.Infrastructure.Database.Monitoring);
            opt.Backup = new DatabaseBackupOptions(); // 使用默认值
        });

        // 注册传统 CacheOptions
        services.Configure<CacheOptions>(opt =>
        {
            opt.Enabled = true; // 默认启用
            opt.GlobalKeyPrefix = "LYBT:";
            opt.Memory = MapToLegacyMemoryCacheConfig(lybtOptions.Infrastructure.Cache.MemoryCache);
            opt.Monitoring = MapToLegacyMonitoringConfig(lybtOptions.Infrastructure.Cache.Monitoring);
        });

        // 注册传统 SecurityOptions
        services.Configure<SecurityOptions>(opt =>
        {
            MapToLegacySecurityOptions(lybtOptions.Security, opt);
        });

        // 注册传统 UserOptions
        services.Configure<UserOptions>(opt =>
        {
            opt.EnableUserCache = true;
            opt.UserCacheExpirationMinutes = 30;
            opt.SessionTimeoutMinutes = lybtOptions.Business.SystemAdmin.SessionTimeoutMinutes;
            opt.EnableDetailedAuditLogging = true;
            opt.EnableOnlineStatusTracking = true;
        });

        // 注册传统 DefaultPasswordOptions
        services.Configure<DefaultPasswordOptions>(opt =>
        {
            opt.SystemAdmin = lybtOptions.Authentication.DefaultPasswords.SysAdminPassword;
            opt.NewUser = lybtOptions.Authentication.DefaultPasswords.NewUserPassword;
            opt.ExpiryDays = 30; // Default 30 days
        });

        // 注册传统 SysAdminOptions
        services.Configure<SysAdminOptions>(opt =>
        {
            opt.Username = lybtOptions.Business.SystemAdmin.Username;
            opt.DefaultPassword = lybtOptions.Authentication.DefaultPasswords.SysAdminPassword;
            opt.RequirePasswordChangeOnFirstLogin = lybtOptions.Authentication.DefaultPasswords.ForceChangeOnFirstLogin;
            opt.EnableAccountLockout = false;
        });
    }

    /// <summary>
    /// 添加配置验证
    /// </summary>
    private static IServiceCollection AddConfigurationValidation<TOptions>(this IServiceCollection services)
        where TOptions : class
    {
        services.AddSingleton<IValidateOptions<TOptions>, ConfigurationValidator<TOptions>>();
        return services;
    }

    /// <summary>
    /// 验证必填设置
    /// </summary>
    private static void ValidateRequiredSettings(LybtOptions options, List<string> validationResults)
    {
        // JWT 必填项验证
        if (string.IsNullOrEmpty(options.Authentication.Jwt.SecretKey))
            validationResults.Add("JWT SecretKey is required");

        if (options.Authentication.Jwt.SecretKey?.Length < 32)
            validationResults.Add("JWT SecretKey must be at least 32 characters");

        if (string.IsNullOrEmpty(options.Authentication.Jwt.Issuer))
            validationResults.Add("JWT Issuer is required");

        if (string.IsNullOrEmpty(options.Authentication.Jwt.Audience))
            validationResults.Add("JWT Audience is required");

        // 数据库连接字符串验证 - Issue #1726 Phase 4: 移除验证
        // 原因：代码使用fallback链（Lybt:Infrastructure:Database:ConnectionString → ConnectionStrings:DefaultConnection → 环境变量）
        // 验证应由数据库初始化服务执行，而非配置验证层
        // if (string.IsNullOrEmpty(options.Infrastructure.Database.ConnectionString))
        //     validationResults.Add("Database ConnectionString is required");

        // 系统管理员必填项验证
        if (string.IsNullOrEmpty(options.Business.SystemAdmin.Username))
            validationResults.Add("SystemAdmin UserName is required");

        if (string.IsNullOrEmpty(options.Business.SystemAdmin.Email))
            validationResults.Add("SystemAdmin Email is required");

        if (string.IsNullOrEmpty(options.Authentication.DefaultPasswords.SysAdminPassword))
            validationResults.Add("SysAdmin Password is required");
    }

    /// <summary>
    /// 验证业务逻辑
    /// </summary>
    private static void ValidateBusinessLogic(LybtOptions options, List<string> validationResults)
    {
        // 验证令牌过期时间逻辑
        if (options.Authentication.Jwt.AccessTokenExpirationMinutes >=
            options.Authentication.Jwt.RefreshTokenExpirationDays * 1440)
        {
            validationResults.Add("Access token expiration should be less than refresh token expiration");
        }

        // 验证密码策略逻辑
        if (options.Authentication.PasswordPolicy.MinLength > options.Authentication.PasswordPolicy.MaxLength)
        {
            validationResults.Add("Password MinLength cannot be greater than MaxLength");
        }

        // 验证数据库连接池配置
        if (options.Infrastructure.Database.ConnectionPool.MinConnections >
            options.Infrastructure.Database.ConnectionPool.MaxConnections)
        {
            validationResults.Add("Database MinConnections cannot be greater than MaxConnections");
        }

        // Issue #1732 Phase 1: 移除分布式缓存验证（MVP阶段仅使用MemoryCache）
    }

    #region Legacy Mapping Methods

    private static PasswordPolicy MapToLegacyPasswordPolicy(PasswordPolicyConfiguration config)
    {
        // Issue #1732 Phase 1: 移除PasswordHistoryCount和PasswordExpirationDays（未实现功能）
        return new PasswordPolicy
        {
            MinLength = config.MinLength,
            RequireUppercase = config.RequireUppercase,
            RequireLowercase = config.RequireLowercase,
            RequireDigit = config.RequireDigit,
            RequireSpecialChar = config.RequireSpecialChar,
            PasswordHistoryCount = 0,  // MVP阶段暂不支持密码历史
            PasswordExpireDays = 0     // MVP阶段暂不支持密码过期
        };
    }

    private static SessionOptions MapToLegacySessionOptions(SessionConfiguration config)
    {
        return new SessionOptions
        {
            TimeoutMinutes = config.TimeoutMinutes,
            SlidingExpiration = config.SlidingExpiration,
            AllowConcurrentSessions = config.AllowConcurrentSessions,
            MaxConcurrentSessions = config.MaxConcurrentSessions
        };
    }

    private static ConnectionPoolOptions MapToLegacyConnectionPoolOptions(ConnectionPoolConfiguration config)
    {
        // 注意：需要根据实际的 ConnectionPoolOptions 类结构进行映射
        // 这里提供基础映射，可能需要调整字段名
        return new ConnectionPoolOptions
        {
            // 映射到实际字段，具体字段名需要根据 ConnectionPoolOptions 类确定
        };
    }

    private static DatabaseMonitoringOptions MapToLegacyDatabaseMonitoringOptions(DatabaseMonitoringConfiguration config)
    {
        // 注意：需要根据实际的 DatabaseMonitoringOptions 类结构进行映射
        return new DatabaseMonitoringOptions
        {
            // 映射到实际字段
        };
    }

    private static MemoryCacheConfig MapToLegacyMemoryCacheConfig(MemoryCacheConfiguration config)
    {
        return new MemoryCacheConfig
        {
            // 映射到实际字段，需要根据 MemoryCacheConfig 类确定具体字段
        };
    }

    private static MonitoringConfig MapToLegacyMonitoringConfig(CacheMonitoringConfiguration config)
    {
        return new MonitoringConfig
        {
            // 映射到实际字段
        };
    }

    private static void MapToLegacySecurityOptions(SecurityOptions config, Options.SecurityOptions legacyOptions)
    {
        // 映射安全相关配置
        // 需要根据实际的 SecurityOptions 类结构进行映射
    }

    #endregion
}

/// <summary>
/// 配置验证结果
/// </summary>
public class ConfigurationValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误列表
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// 配置验证器
/// </summary>
/// <typeparam name="TOptions">配置选项类型</typeparam>
public class ConfigurationValidator<TOptions> : IValidateOptions<TOptions>
    where TOptions : class
{
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        var context = new ValidationContext(options);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(options, context, validationResults, true);

        if (isValid)
        {
            return ValidateOptionsResult.Success;
        }

        var errors = validationResults.Select(r => r.ErrorMessage ?? "Unknown validation error").ToList();
        return ValidateOptionsResult.Fail(errors);
    }
}
