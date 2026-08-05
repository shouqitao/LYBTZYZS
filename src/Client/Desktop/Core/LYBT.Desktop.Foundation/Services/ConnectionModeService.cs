// ---------------------------------------------------------------------------
// ConnectionModeService — Remote/Local mode detection and transparent fallback
// ---------------------------------------------------------------------------
// Wraps IConnectionSettingsService (URL + PreferredMode + RemoteUrl) and
// IApplicationStateService (health) to expose a mode-oriented view of the
// connection: probe remote health, fall back to embedded LocalWebAPI when
// the remote server is unreachable, and keep subscribers in sync via
// ModeChanged.
// ---------------------------------------------------------------------------

using System.Net.Http;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Services;

/// <summary>
/// 通过探测配置的 WebAPI 健康端点检测最佳连接模式（Remote 或 Local），
/// 当远程服务器不可达时透明地回退到
/// 嵌入式 LocalWebAPI。
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
    private readonly ILogger<ConnectionModeService> _logger;
    private readonly SemaphoreSlim _detectGate = new(1, 1);

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
        ILogger<ConnectionModeService> logger)
    {
        _connectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));
        _applicationState = applicationState ?? throw new ArgumentNullException(nameof(applicationState));
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
    public async Task<ConnectionMode> DetectBestModeAsync()
    {
        await _detectGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var preferred = _connectionSettings.PreferredMode;

            if (preferred == "Remote" && !string.IsNullOrEmpty(_connectionSettings.RemoteUrl))
            {
                _isRemoteAvailable = await TestRemoteConnectionAsync(_connectionSettings.RemoteUrl).ConfigureAwait(false);
                if (_isRemoteAvailable)
                {
                    _logger.LogInformation("[CONNECTION-MODE] Remote server reachable at {Url} → Remote mode", _connectionSettings.RemoteUrl);
                    ApplyMode(ConnectionMode.Remote);
                    return ConnectionMode.Remote;
                }

                _logger.LogWarning("[CONNECTION-MODE] Remote server unreachable at {Url}, falling back to Local mode", _connectionSettings.RemoteUrl);
            }

            // Check if remote is available (for UI button state) even when we end up in Local mode.
            if (!string.IsNullOrEmpty(_connectionSettings.RemoteUrl))
            {
                _isRemoteAvailable = await TestRemoteConnectionAsync(_connectionSettings.RemoteUrl).ConfigureAwait(false);
            }
            else
            {
                _isRemoteAvailable = false;
            }

            ApplyMode(ConnectionMode.Local);
            return ConnectionMode.Local;
        }
        finally
        {
            _detectGate.Release();
        }
    }

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
    public async Task<bool> TestLocalConnectionAsync()
    {
        var healthUrl = $"{_connectionSettings.LocalUrl}{LocalHealthPath}";
        try
        {
            using var client = new HttpClient { Timeout = LocalProbeTimeout };
            var response = await client.GetAsync(healthUrl).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException)
        {
            _logger.LogDebug(ex, "[CONNECTION-MODE] Local probe failed at {Url}", healthUrl);
            return false;
        }
    }

    /// <inheritdoc />
    public void SetMode(ConnectionMode mode)
    {
        switch (mode)
        {
            case ConnectionMode.Local:
                _ = _connectionSettings.SavePreferredModeAsync("Local");
                ApplyMode(ConnectionMode.Local);
                break;

            case ConnectionMode.Remote:
                if (!string.IsNullOrEmpty(_connectionSettings.RemoteUrl) && _isRemoteAvailable)
                {
                    _ = _connectionSettings.SavePreferredModeAsync("Remote");
                    ApplyMode(ConnectionMode.Remote);
                }
                else
                {
                    _logger.LogWarning(
                        "[CONNECTION-MODE] Cannot switch to Remote: no remote URL configured or server unreachable");
                }
                break;

            case ConnectionMode.Auto:
                // Background detection; fire-and-forget is safe because
                // DetectBestModeAsync serializes via _detectGate.
                _ = DetectBestModeAsync();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported connection mode");
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
    /// 释放检测门并解除 URL 订阅。
    /// DI 容器在关闭时释放单例。
    /// </summary>
    public void Dispose()
    {
        _connectionSettings.UrlChanged -= OnUrlChanged;
        _detectGate.Dispose();
    }
}
