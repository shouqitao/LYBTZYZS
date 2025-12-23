using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Shared.Configuration.Extensions;

/// <summary>
/// 客户端配置扩展方法
/// </summary>
public static class ClientConfigurationExtensions
{
    /// <summary>
    /// 添加客户端配置
    /// </summary>
    public static IServiceCollection AddLybtClientConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册验证器
        services.AddSingleton<JwtOptionsValidator>();

        // JWT 配置 (客户端用于令牌验证)
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate<JwtOptionsValidator>((options, validator) => 
                validator.Validate(null, options).Succeeded, 
                "JWT 配置验证失败")
            .ValidateOnStart();

        // API 客户端配置
        services.AddOptions<ApiClientOptions>()
            .Bind(configuration.GetSection(ApiClientOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 客户端会话配置
        services.AddOptions<ClientSessionOptions>()
            .Bind(configuration.GetSection(ClientSessionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 功能开关配置 (支持热更新，不使用 ValidateOnStart)
        services.AddOptions<FeatureToggleOptions>()
            .Bind(configuration.GetSection(FeatureToggleOptions.SectionName))
            .ValidateDataAnnotations();

        // 诊所设置配置
        services.AddOptions<ClinicSettingsOptions>()
            .Bind(configuration.GetSection(ClinicSettingsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 处方配置
        services.AddOptions<PrescriptionOptions>()
            .Bind(configuration.GetSection(PrescriptionOptions.SectionName))
            .ValidateDataAnnotations();

        return services;
    }
}
