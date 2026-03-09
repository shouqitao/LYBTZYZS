using LYBT.Desktop.CardReader.Abstractions;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Configuration.Options.Common;
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

        // 功能开关配置
        RegisterOptions<FeatureToggleOptions>(containerRegistry, configuration, FeatureToggleOptions.SectionName);

        // 诊所设置配置
        RegisterOptions<ClinicSettingsOptions>(containerRegistry, configuration, ClinicSettingsOptions.SectionName);

        // 处方配置
        RegisterOptions<PrescriptionOptions>(containerRegistry, configuration, PrescriptionOptions.SectionName);

        // 读卡器配置 (PRD-13)
        RegisterOptions<CardReaderOptions>(containerRegistry, configuration, CardReaderOptions.SectionName);
    }

    /// <summary>
    /// 注册单个 Options 类型到 Prism 容器
    /// </summary>
    private static void RegisterOptions<TOptions>(
        IContainerRegistry containerRegistry,
        IConfiguration configuration,
        string sectionName) where TOptions : class, new()
    {
        // 绑定配置到 Options 实例
        var options = new TOptions();
        configuration.GetSection(sectionName).Bind(options);

        // 创建 IOptions<T> 包装器
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(options);

        // 注册为单例
        containerRegistry.RegisterInstance<IOptions<TOptions>>(optionsWrapper);

        // 同时注册 TOptions 直接访问（便于简单注入场景）
        containerRegistry.RegisterInstance(options);
    }
}
