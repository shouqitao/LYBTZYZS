using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Shared.Configuration.Extensions;

/// <summary>
/// 服务端配置扩展方法
/// </summary>
public static class ServerConfigurationExtensions
{
    /// <summary>
    /// 添加服务端配置
    /// </summary>
    public static IServiceCollection AddLybtServerConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册验证器
        services.AddSingleton<JwtOptionsValidator>();
        services.AddSingleton<DatabaseOptionsValidator>();
        services.AddSingleton<SecurityOptionsValidator>();

        // JWT 配置
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate<JwtOptionsValidator>((options, validator) => 
                validator.Validate(null, options).Succeeded, 
                "JWT 配置验证失败")
            .ValidateOnStart();

        // 数据库配置
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate<DatabaseOptionsValidator>((options, validator) => 
                validator.Validate(null, options).Succeeded, 
                "数据库配置验证失败")
            .ValidateOnStart();

        // 安全配置
        services.AddOptions<SecurityOptions>()
            .Bind(configuration.GetSection(SecurityOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate<SecurityOptionsValidator>((options, validator) => 
                validator.Validate(null, options).Succeeded, 
                "安全配置验证失败")
            .ValidateOnStart();

        // 会话配置
        services.AddOptions<SessionOptions>()
            .Bind(configuration.GetSection(SessionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 日志配置 (支持热更新，不使用 ValidateOnStart)
        services.AddOptions<LoggingOptions>()
            .Bind(configuration.GetSection(LoggingOptions.SectionName))
            .ValidateDataAnnotations();

        // 用户管理配置
        services.AddOptions<UserManagementOptions>()
            .Bind(configuration.GetSection(UserManagementOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 系统管理员配置
        services.AddOptions<SystemAdminOptions>()
            .Bind(configuration.GetSection(SystemAdminOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 密码策略配置
        services.AddOptions<PasswordPolicyOptions>()
            .Bind(configuration.GetSection(PasswordPolicyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 默认密码配置
        services.AddOptions<DefaultPasswordOptions>()
            .Bind(configuration.GetSection(DefaultPasswordOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 内存缓存配置
        services.AddOptions<MemoryCacheOptions>()
            .Bind(configuration.GetSection(MemoryCacheOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
