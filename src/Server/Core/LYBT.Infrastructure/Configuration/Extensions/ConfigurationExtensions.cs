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

        // 注册传统 CacheOptions（仅保留真实使用的配置）
        // MemoryCacheAdapter依赖此配置
        services.Configure<CacheOptions>(opt =>
        {
            opt.Enabled = true; // 默认启用
            opt.GlobalKeyPrefix = "LYBT:";
            opt.Memory = MapToLegacyMemoryCacheConfig(lybtOptions.MemoryCache);
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
        // Issue #1761 Phase 3.1: Authentication.Jwt → Jwt（完全扁平化）
        if (string.IsNullOrEmpty(options.Jwt.SecretKey))
            validationResults.Add("JWT SecretKey is required");

        if (options.Jwt.SecretKey?.Length < 32)
            validationResults.Add("JWT SecretKey must be at least 32 characters");

        if (string.IsNullOrEmpty(options.Jwt.Issuer))
            validationResults.Add("JWT Issuer is required");

        if (string.IsNullOrEmpty(options.Jwt.Audience))
            validationResults.Add("JWT Audience is required");

        // 数据库连接字符串验证 - Issue #1726 Phase 4: 移除验证
        // 原因：代码使用fallback链（Lybt:Infrastructure:Database:ConnectionString → ConnectionStrings:DefaultConnection → 环境变量）
        // 验证应由数据库初始化服务执行，而非配置验证层
        // if (string.IsNullOrEmpty(options.Infrastructure.Database.ConnectionString))
        //     validationResults.Add("Database ConnectionString is required");

        // 系统管理员必填项验证
        // Issue #1761 Phase 3.1: Business.SystemAdmin → SystemAdmin（完全扁平化）
        if (string.IsNullOrEmpty(options.SystemAdmin.Username))
            validationResults.Add("SystemAdmin UserName is required");

        if (string.IsNullOrEmpty(options.SystemAdmin.Email))
            validationResults.Add("SystemAdmin Email is required");

        // Issue #1761 Phase 3.1: Authentication.DefaultPasswords → DefaultPasswords（完全扁平化）
        if (string.IsNullOrEmpty(options.DefaultPasswords.SysAdminPassword))
            validationResults.Add("SysAdmin Password is required");
    }

    /// <summary>
    /// 验证业务逻辑
    /// </summary>
    private static void ValidateBusinessLogic(LybtOptions options, List<string> validationResults)
    {
        // 验证令牌过期时间逻辑
        // Issue #1761 Phase 3.1: Authentication.Jwt → Jwt（完全扁平化）
        if (options.Jwt.AccessTokenExpirationMinutes >=
            options.Jwt.RefreshTokenExpirationDays * 1440)
        {
            validationResults.Add("Access token expiration should be less than refresh token expiration");
        }

        // 验证密码策略逻辑
        // Issue #1761 Phase 3.1: Authentication.PasswordPolicy → PasswordPolicy（完全扁平化）
        if (options.PasswordPolicy.MinLength > options.PasswordPolicy.MaxLength)
        {
            validationResults.Add("Password MinLength cannot be greater than MaxLength");
        }

        // Issue #1732 Phase 1: 移除分布式缓存验证（MVP阶段仅使用MemoryCache）
        // Issue #1761 Phase 2.1: 移除ConnectionPool和Monitoring验证（MVP阶段不需要）
    }

    #region Legacy Mapping Methods（仅保留CacheOptions相关）

    private static MemoryCacheConfig MapToLegacyMemoryCacheConfig(MemoryCacheConfiguration config)
    {
        return new MemoryCacheConfig
        {
            // 映射到实际字段，需要根据 MemoryCacheConfig 类确定具体字段
        };
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
