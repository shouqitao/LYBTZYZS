// ---------------------------------------------------------------------------
// ConnectionModeService — Remote/Local explicit mode management
// ---------------------------------------------------------------------------
// Wraps IConnectionSettingsService (URL + PreferredMode + RemoteUrl) and
// IApplicationStateService (health) to expose a mode-oriented view of the
// connection: probe remote health for UI state, switch modes only on explicit
// user request, and keep subscribers in sync via ModeChanged. No automatic
// fallback — a selected mode stays active until the user changes it.
// ---------------------------------------------------------------------------

using System.Net.Http;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Services;

/// <summary>
/// 管理连接模式（Remote 或 Local）。模式仅由用户显式切换，
/// 不自动探测或降级；远程健康探测仅用于 UI 状态显示。
/// </summary>
public sealed class ConnectionModeService : IConnectionModeService, IDisposable
{
    /// <summary>远程 WebAPI 匿名健康路径。</summary>
    private const string RemoteHealthPath = "/api/v1/health";

    /// <summary>LocalWebAPI 匿名健康路径。</summary>
    private const string LocalHealthPath = "/api/v1/health";

    private static readonly TimeSpan RemoteProbeTimeout = TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan LocalProbeTimeout = TimeSpan.FromMilliseconds(1000);

    private readonly IConnectionSettingsService _connectionSettings;
    private readonly IApplicationStateService _applicationState;
    private readonly IApiClient _apiClient;
    private readonly ILogger<ConnectionModeService> _logger;

    private ConnectionMode _currentMode;
    private bool _isRemoteAvailable;
    private bool _isSwitching; // 重入守卫：防止 SetModeAsync → SavePreferredModeAsync → UrlChanged → SetModeAsync 循环

    /// <summary>
    /// 构建服务。初始 <see cref="CurrentMode"/> 由
    /// <see cref="IConnectionSettingsService.PreferredMode"/> /
    /// <see cref="IConnectionSettingsService.IsLocal"/> 推导。
    /// </summary>
    public ConnectionModeService(
        IConnectionSettingsService connectionSettings,
        IApplicationStateService applicationState,
        IApiClient apiClient,
        ILogger<ConnectionModeService> logger)
    {
        _connectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));
        _applicationState = applicationState ?? throw new ArgumentNullException(nameof(applicationState));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _currentMode = connectionSettings.IsLocal
            ? ConnectionMode.Local
            : ConnectionMode.Remote;

        _connectionSettings.UrlChanged += OnUrlChanged;
    }

    /// <inheritdoc />
    public ConnectionMode CurrentMode => _currentMode;

    /// <inheritdoc />
    public string CurrentModeDisplay => _currentMode == ConnectionMode.Remote
        ? "远程模式"
        : "本地模式";

    public bool IsRemote => _currentMode == ConnectionMode.Remote;
    public bool IsLocal => _currentMode == ConnectionMode.Local;

    /// <inheritdoc />
    public bool IsRemoteAvailable => _isRemoteAvailable;

    /// <summary>含模式信息的 API 状态消息。</summary>
    public string ApiStatusDisplay => _currentMode == ConnectionMode.Remote
        ? "远程 WebAPI 已连接"
        : "本地 WebAPI 已连接";

    /// <inheritdoc />
    public event EventHandler<ConnectionMode>? ModeChanged;

    /// <inheritdoc />
    public async Task<bool> CheckRemoteAvailableAsync()
    {
        if (string.IsNullOrEmpty(_connectionSettings.RemoteUrl))
        {
            _isRemoteAvailable = false;
            return false;
        }

        _isRemoteAvailable = await TestRemoteConnectionAsync(_connectionSettings.RemoteUrl).ConfigureAwait(false);
        return _isRemoteAvailable;
    }

    /// <inheritdoc />
    public async Task<bool> TestRemoteConnectionAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!_connectionSettings.IsValidUrl(url))
        {
            _logger.LogDebug("[CONNECTION-MODE] Invalid remote URL: {Url}", url);
            return false;
        }

        var healthUrl = $"{url.TrimEnd('/')}{RemoteHealthPath}";
        try
        {
            using var client = new HttpClient { Timeout = RemoteProbeTimeout };
            var response = await client.GetAsync(healthUrl).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException)
        {
            _logger.LogDebug(ex, "[CONNECTION-MODE] Remote probe failed for {Url}", healthUrl);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ModeSwitchResult> SetModeAsync(ConnectionMode mode)
    {
        if (_isSwitching)
        {
            _logger.LogDebug("[CONNECTION-MODE] SetModeAsync re-entrancy blocked");
            return ModeSwitchResult.Success();
        }

        _isSwitching = true;
        try
        {
            switch (mode)
        {
            case ConnectionMode.Local:
                    // 先切 URL，立即生效，不等网络探测
                    await _connectionSettings.SavePreferredModeAsync("Local").ConfigureAwait(false);
                    ApplyMode(ConnectionMode.Local);
                return ModeSwitchResult.Success();

            case ConnectionMode.Remote:
                // 守卫 1：URL 配置检查（纯本地，无网络）
                if (string.IsNullOrEmpty(_connectionSettings.RemoteUrl))
                {
                    _logger.LogWarning("[CONNECTION-MODE] Cannot switch to Remote: no remote URL configured");
                    return ModeSwitchResult.Blocked("NO_REMOTE_URL", "未配置远程服务器地址，无法切换到远程模式");
                }

                // 先切 URL，立即生效
                await _connectionSettings.SavePreferredModeAsync("Remote").ConfigureAwait(false);
                ApplyMode(ConnectionMode.Remote);

                // 后台探测远程可达性（不阻塞切换）
                _ = FireAndForgetRemoteProbeAsync();

                return ModeSwitchResult.Success();

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported connection mode");
        }
        }
        finally
        {
            _isSwitching = false;
        }
    }

    /// <summary>
    /// <summary>
    /// 后台探测远程可达性 + 未完成医案守卫，不阻塞 UI。
    /// </summary>
    private async Task FireAndForgetRemoteProbeAsync()
    {
        try
        {
            var isRemoteAvailable = await CheckRemoteAvailableAsync().ConfigureAwait(false);
            _isRemoteAvailable = isRemoteAvailable;

            if (!isRemoteAvailable)
            {
                _logger.LogWarning("[CONNECTION-MODE] Remote probe failed after mode switch — server unreachable");
                // 可在此处通过 IEventAggregator 通知 UI 显示警告
                return;
            }

            // 守卫：未完成医案（3s 超时）
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var pendingCount = await _apiClient.MedicalCases
                .GetPendingCasesAsync(null)
                .WaitAsync(cts.Token)
                .ConfigureAwait(false);
            if (pendingCount is { Success: true, Data: not null } && pendingCount.Data.Count > 0)
            {
                _logger.LogWarning("[CONNECTION-MODE] {Count} pending medical cases detected after switch (ERR-70506)", pendingCount.Data.Count);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "[CONNECTION-MODE] Background remote probe failed");
        }
    }

    /// <summary>
    /// 更新生效模式，变化时触发 <see cref="ModeChanged"/>。
    /// </summary>
    private void ApplyMode(ConnectionMode mode)
    {
        if (_currentMode == mode)
            return;

        _currentMode = mode;
        _logger.LogInformation("[CONNECTION-MODE] Mode changed → {Mode}", CurrentModeDisplay);

        // Mirror the result into ApplicationStateService for legacy consumers.
        _applicationState.ConnectionStatus = CurrentModeDisplay;
        _applicationState.IsApiHealthy = true;

        ModeChanged?.Invoke(this, mode);
    }

    /// <summary>
    /// 当 URL 被外部修改时，重新推导生效模式并统一走 SetModeAsync
    /// （守卫 + 状态同步 + 事件），不再直接 ApplyMode 绕过守卫。
    /// fire-and-forget：UrlChanged 为同步事件；异步执行体 HandleUrlChangedAsync 就地捕获异常。
    /// </summary>
    private void OnUrlChanged(object? sender, string newUrl)
    {
        _ = HandleUrlChangedAsync(newUrl);
    }

    /// <summary>
    /// URL 驱动模式切换的异步执行体。必须在此 await SetModeAsync 并捕获异常——
    /// 若在 OnUrlChanged 中写 `_ = SetModeAsync()` fire-and-forget，异步阶段的异常
    /// 会变成未观察任务异常（try/catch 只能捕获 await 前的同步抛出，捕获不到异步 Task 故障）。
    /// </summary>
    private async Task HandleUrlChangedAsync(string newUrl)
    {
        try
        {
            var derived = _connectionSettings.IsLocal
                ? ConnectionMode.Local
                : ConnectionMode.Remote;
            await SetModeAsync(derived).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CONNECTION-MODE] URL-driven mode switch failed for {Url}", newUrl);
        }
    }

    /// <summary>
    /// 释放 URL 订阅。
    /// DI 容器在关闭时释放单例。
    /// </summary>
    public void Dispose()
    {
        _connectionSettings.UrlChanged -= OnUrlChanged;
    }
}
