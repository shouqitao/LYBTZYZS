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

    private static readonly TimeSpan RemoteProbeTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan LocalProbeTimeout = TimeSpan.FromSeconds(2);

    private readonly IConnectionSettingsService _connectionSettings;
    private readonly IApplicationStateService _applicationState;
    private readonly IApiClient _apiClient;
    private readonly ILogger<ConnectionModeService> _logger;

    private ConnectionMode _currentMode;
    private bool _isRemoteAvailable;

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
        switch (mode)
        {
            case ConnectionMode.Local:
                _ = _connectionSettings.SavePreferredModeAsync("Local");
                ApplyMode(ConnectionMode.Local);
                return ModeSwitchResult.Success();

            case ConnectionMode.Remote:
                // B2 (US-SHELL-007): 守卫 1/2/3 —— URL 配置、远程可达、未完成医案
                if (string.IsNullOrEmpty(_connectionSettings.RemoteUrl))
                {
                    _logger.LogWarning("[CONNECTION-MODE] Cannot switch to Remote: no remote URL configured");
                    return ModeSwitchResult.Blocked("NO_REMOTE_URL", "未配置远程服务器地址，无法切换到远程模式");
                }

                if (!_isRemoteAvailable)
                {
                    _logger.LogWarning("[CONNECTION-MODE] Cannot switch to Remote: server unreachable");
                    return ModeSwitchResult.Blocked("REMOTE_UNREACHABLE", "远程服务器不可达，无法切换到远程模式");
                }

                var pendingCount = await GetPendingCaseCountAsync().ConfigureAwait(false);
                if (pendingCount > 0)
                {
                    _logger.LogWarning(
                        "[CONNECTION-MODE] Blocked switch to Remote: {Count} pending medical cases (ERR-70506)", pendingCount);
                    return ModeSwitchResult.PendingCasesBlocked(pendingCount);
                }

                _ = _connectionSettings.SavePreferredModeAsync("Remote");
                ApplyMode(ConnectionMode.Remote);
                return ModeSwitchResult.Success();

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported connection mode");
        }
    }

    /// <summary>
    /// 查询当前数据源的未完成医案数（Active/Suspended）。查询失败视为无未完成医案
    /// （守卫仅阻断，不做强制）——保持切换可用性优先。
    /// </summary>
    private async Task<int> GetPendingCaseCountAsync()
    {
        try
        {
            var response = await _apiClient.MedicalCases.GetPendingCasesAsync(null).ConfigureAwait(false);
            if (!response.Success || response.Data == null)
                return 0;
            return response.Data.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CONNECTION-MODE] Pending-case guard query failed - guard skipped");
            return 0;
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
    /// 当 URL 被外部修改时，重新推导生效模式。
    /// </summary>
    private void OnUrlChanged(object? sender, string newUrl)
    {
        var derived = _connectionSettings.IsLocal
            ? ConnectionMode.Local
            : ConnectionMode.Remote;
        ApplyMode(derived);
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
