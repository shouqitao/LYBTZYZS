using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// API 路由器实现 — 根据 IApiHealthMonitor 状态自动切换远程/本地 API
/// </summary>
public sealed class ApiRouter : IApiRouter, IDisposable
{
    private readonly IApiHealthMonitor _healthMonitor;
    private readonly ILogger<ApiRouter> _logger;

    private ApiMode _currentMode = ApiMode.Remote;
    private ApiMode? _manualOverride;
    private bool _disposed;

    public ApiRouter(
        IApiHealthMonitor healthMonitor,
        ILogger<ApiRouter> logger)
    {
        _healthMonitor = healthMonitor ?? throw new ArgumentNullException(nameof(healthMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _healthMonitor.StatusChanged += OnHealthMonitorStatusChanged;
    }

    /// <inheritdoc />
    public ApiMode CurrentMode => _currentMode;

    /// <inheritdoc />
    public bool IsOffline => _currentMode == ApiMode.Local;

    /// <inheritdoc />
    public ApiMode? ManualOverride
    {
        get => _manualOverride;
        set
        {
            if (_manualOverride == value) return;

            _manualOverride = value;
            _logger.LogInformation("[API-ROUTER] 手动覆盖已{Action}: {Value}",
                value.HasValue ? "设置" : "清除",
                value?.ToString() ?? "自动");

            if (value.HasValue)
            {
                SwitchMode(value.Value, isManual: true);
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<ApiModeChangedEventArgs>? ModeChanged;

    /// <inheritdoc />
    public void SwitchTo(ApiMode mode)
    {
        ManualOverride = mode;
    }

    /// <inheritdoc />
    public void ClearManualOverride()
    {
        ManualOverride = null;
        // 恢复自动判断
        EvaluateAutomaticMode();
    }

    private void OnHealthMonitorStatusChanged(object? sender, ApiHealthMonitorChangedEventArgs e)
    {
        // 手动覆盖模式下忽略自动判断
        if (_manualOverride.HasValue)
        {
            _logger.LogDebug("[API-ROUTER] 健康状态变更但手动覆盖生效，忽略自动切换");
            return;
        }

        EvaluateAutomaticMode();
    }

    private void EvaluateAutomaticMode()
    {
        var newMode = _healthMonitor.Status switch
        {
            ApiMonitorHealthStatus.Healthy => ApiMode.Remote,
            ApiMonitorHealthStatus.Unhealthy when _healthMonitor.ConsecutiveFailures >= _healthMonitor.CircuitBreakerThreshold => ApiMode.Local,
            _ => _currentMode // 保持当前模式（Checking 等中间状态不切换）
        };

        if (newMode != _currentMode)
        {
            SwitchMode(newMode, isManual: false);
        }
    }

    private void SwitchMode(ApiMode newMode, bool isManual)
    {
        var oldMode = _currentMode;
        _currentMode = newMode;

        _logger.LogInformation(
            "[API-ROUTER] API 模式切换: {OldMode} -> {NewMode} ({Source})",
            oldMode, newMode, isManual ? "手动" : "自动");

        ModeChanged?.Invoke(this, new ApiModeChangedEventArgs
        {
            OldMode = oldMode,
            NewMode = newMode,
            IsManual = isManual
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _healthMonitor.StatusChanged -= OnHealthMonitorStatusChanged;
    }
}
