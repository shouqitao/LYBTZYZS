using LYBT.Desktop.Infrastructure.CardReader.Abstractions;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Desktop.Infrastructure.Services.FeatureToggle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Prism.Ioc;

namespace LYBT.Desktop.Shell.Extensions;

/// <summary>
/// Prism DryIoc 容器配置扩展方法
/// </summary>
public static class PrismConfigurationExtensions
{
    /// <summary>
    /// 注册客户端强类型配置到 Prism 容器
    /// </summary>
    /// <param name="containerRegistry">Prism 容器注册表</param>
    /// <param name="configuration">配置根</param>
    public static void AddLybtClientConfiguration(
        this IContainerRegistry containerRegistry,
        IConfiguration configuration)
    {
        // JWT 配置
        RegisterOptions<JwtOptions>(containerRegistry, configuration, JwtOptions.SectionName);

        // API 客户端配置
        RegisterOptions<ApiClientOptions>(containerRegistry, configuration, ApiClientOptions.SectionName);

        // 客户端会话配置
        RegisterOptions<ClientSessionOptions>(containerRegistry, configuration, ClientSessionOptions.SectionName);

        // 诊所设置配置
        RegisterOptions<ClinicSettingsOptions>(containerRegistry, configuration, ClinicSettingsOptions.SectionName);

        // 功能开关配置 (US-CFG-004)
        RegisterOptions<FeatureToggleOptions>(containerRegistry, configuration, FeatureToggleOptions.SectionName);

        // 读卡器配置 (PRD-13)
        RegisterOptions<CardReaderOptions>(containerRegistry, configuration, CardReaderOptions.SectionName);

        // 离线模式配置
        RegisterOptions<OfflineModeOptions>(containerRegistry, configuration, OfflineModeOptions.SectionName);

        // 默认密码配置
        RegisterOptions<DefaultPasswordOptions>(containerRegistry, configuration, DefaultPasswordOptions.SectionName);

        // LocalWebAPI JWT 配置
        RegisterOptions<LocalJwtOptions>(containerRegistry, configuration, LocalJwtOptions.SectionName);
    }

    /// <summary>
    /// 注册单个 Options 类型到 Prism 容器（启动时冻结快照）
    /// </summary>
    private static void RegisterOptions<TOptions>(
        IContainerRegistry containerRegistry,
        IConfiguration configuration,
        string sectionName) where TOptions : class, new()
    {
        var options = new TOptions();
        configuration.GetSection(sectionName).Bind(options);

        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(options);

        containerRegistry.RegisterInstance<IOptions<TOptions>>(optionsWrapper);
        containerRegistry.RegisterInstance(options);
    }
}
