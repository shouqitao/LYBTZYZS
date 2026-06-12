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

        // 功能开关配置 (支持热更新)
        RegisterReloadableOptions<FeatureToggleOptions>(containerRegistry, configuration, FeatureToggleOptions.SectionName);

        // 诊所设置配置
        RegisterOptions<ClinicSettingsOptions>(containerRegistry, configuration, ClinicSettingsOptions.SectionName);

        // 读卡器配置 (PRD-13)
        RegisterOptions<CardReaderOptions>(containerRegistry, configuration, CardReaderOptions.SectionName);
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

    /// <summary>
    /// 注册支持热更新的 Options 类型到 Prism 容器
    /// 使用 IOptionsMonitor 包装，配置文件变更时自动刷新
    /// </summary>
    private static void RegisterReloadableOptions<TOptions>(
        IContainerRegistry containerRegistry,
        IConfiguration configuration,
        string sectionName) where TOptions : class, new()
    {
        var monitor = new ConfigurationOptionsMonitor<TOptions>(
            configuration.GetSection(sectionName));

        containerRegistry.RegisterInstance<IOptionsMonitor<TOptions>>(monitor);
        containerRegistry.RegisterInstance<IOptions<TOptions>>(
            new OptionsMonitorWrapper<TOptions>(monitor));
    }

    /// <summary>
    /// 基于 IConfiguration 变更通知的 IOptionsMonitor 实现
    /// </summary>
    private sealed class ConfigurationOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>, IDisposable
        where TOptions : class, new()
    {
        private readonly IConfigurationSection _section;
        private readonly IDisposable _changeListener;
        private TOptions _current;
        private readonly List<Action<TOptions, string?>> _listeners = [];

        public ConfigurationOptionsMonitor(IConfigurationSection section)
        {
            _section = section;
            _current = BindOptions();
            _changeListener = section.GetReloadToken().RegisterChangeCallback(OnConfigurationChanged, null);
        }

        public TOptions CurrentValue => _current;

        public TOptions Get(string? name) => _current;

        public IDisposable OnChange(Action<TOptions, string?> listener)
        {
            _listeners.Add(listener);
            return new ChangeListenerDisposable(() => _listeners.Remove(listener));
        }

        private void OnConfigurationChanged(object? state)
        {
            var newValue = BindOptions();
            Interlocked.Exchange(ref _current, newValue);
            foreach (var listener in _listeners)
            {
                listener(newValue, null);
            }
            _changeListener?.Dispose();
            var token = _section.GetReloadToken().RegisterChangeCallback(OnConfigurationChanged, null);
        }

        private TOptions BindOptions()
        {
            var options = new TOptions();
            _section.Bind(options);
            return options;
        }

        public void Dispose()
        {
            _changeListener?.Dispose();
        }
    }

    /// <summary>
    /// 将 IOptionsMonitor 适配为 IOptions 接口（向后兼容）
    /// </summary>
    private sealed class OptionsMonitorWrapper<TOptions> : IOptions<TOptions>
        where TOptions : class, new()
    {
        private readonly IOptionsMonitor<TOptions> _monitor;
        public OptionsMonitorWrapper(IOptionsMonitor<TOptions> monitor) => _monitor = monitor;
        public TOptions Value => _monitor.CurrentValue;
    }

    private sealed class ChangeListenerDisposable : IDisposable
    {
        private readonly Action _unsubscribe;
        public ChangeListenerDisposable(Action unsubscribe) => _unsubscribe = unsubscribe;
        public void Dispose() => _unsubscribe();
    }
}
