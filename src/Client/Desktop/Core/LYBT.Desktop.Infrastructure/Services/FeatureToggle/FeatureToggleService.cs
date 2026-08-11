using LYBT.Desktop.Controls.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services.FeatureToggle;

/// <summary>
/// 功能开关服务实现（T8: US-CFG-004）。
/// IConfiguration 动态读取（AddJsonFile reloadOnChange 已配置——文件变更即时生效）；
/// GetReloadToken 注册变更回调触发 TogglesChanged 事件（热更新通知，无需重启 Desktop）。
/// </summary>
public class FeatureToggleService : IFeatureToggleService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FeatureToggleService> _logger;
    private IDisposable? _reloadRegistration;

    public FeatureToggleService(
        IConfiguration configuration,
        ILogger<FeatureToggleService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _reloadRegistration = _configuration.GetReloadToken().RegisterChangeCallback(OnConfigurationReloaded, null);
    }

    /// <inheritdoc />
    public event EventHandler? TogglesChanged;

    /// <inheritdoc />
    public bool IsEnabled(string key)
    {
        var section = _configuration.GetSection($"{FeatureToggleOptions.SectionName}:{key}");
        return bool.TryParse(section.Value, out var value) ? value : true;
    }

    /// <inheritdoc />
    public string? GetValue(string key)
        => _configuration.GetSection($"{FeatureToggleOptions.SectionName}:{key}").Value;

    /// <summary>
    /// 当前重复药材合并策略（T8-2: DuplicateHerbMergeStrategy 消费——非法值回退 Max）
    /// </summary>
    public DuplicateDosageStrategy GetDuplicateMergeStrategy()
    {
        var value = GetValue(nameof(FeatureToggleOptions.DuplicateHerbMergeStrategy));
        return Enum.TryParse<DuplicateDosageStrategy>(value, ignoreCase: true, out var strategy)
            ? strategy
            : DuplicateDosageStrategy.Max;
    }

    /// <summary>当前是否自动覆盖冲突（T8-2: OverwriteConflicts 消费）</summary>
    public bool GetOverwriteConflicts()
        => IsEnabled(nameof(FeatureToggleOptions.OverwriteConflicts));

    private void OnConfigurationReloaded(object? state)
    {
        try
        {
            _logger.LogDebug("[TOGGLE] 配置变更，触发 TogglesChanged");
            TogglesChanged?.Invoke(this, EventArgs.Empty);
            // 重新注册变更回调（ReloadToken 一次性）
            _reloadRegistration?.Dispose();
            _reloadRegistration = _configuration.GetReloadToken().RegisterChangeCallback(OnConfigurationReloaded, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TOGGLE] 配置变更回调失败");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _reloadRegistration?.Dispose();
        _reloadRegistration = null;
    }
}
