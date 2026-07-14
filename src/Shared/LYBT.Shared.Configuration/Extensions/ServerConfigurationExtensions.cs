using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();
        services.AddSingleton<IValidateOptions<SecurityOptions>, SecurityOptionsValidator>();

        // JWT 配置
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 数据库配置
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 安全配置
        services.AddOptions<SecurityOptions>()
            .Bind(configuration.GetSection(SecurityOptions.SectionName))
            .ValidateDataAnnotations()
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

        // 系统管理员配置
        services.AddOptions<SystemAdminOptions>()
            .Bind(configuration.GetSection(SystemAdminOptions.SectionName))
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

        // LocalJWT 配置
        services.AddOptions<LocalJwtOptions>()
            .Bind(configuration.GetSection(LocalJwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // CORS 配置 (支持热更新，不使用 ValidateOnStart)
        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .ValidateDataAnnotations();

        // Desktop 更新配置
        services.AddOptions<DesktopUpdateOptions>()
            .Bind(configuration.GetSection(DesktopUpdateOptions.SectionName))
            .ValidateDataAnnotations();

        // 应用信息配置
        services.AddOptions<AppInfoOptions>()
            .Bind(configuration.GetSection(AppInfoOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 注册 LocalJwt 验证器
        services.AddSingleton<IValidateOptions<LocalJwtOptions>, LocalJwtOptionsValidator>();

        return services;
    }
}
