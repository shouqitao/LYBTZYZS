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
/// Detects the best connection mode (Remote vs Local) by probing the
/// configured WebAPI health endpoint, and transparently falls back to the
/// embedded LocalWebAPI when the remote server is unreachable.
/// </summary>
public sealed class ConnectionModeService : IConnectionModeService, IDisposable
{
    /// <summary>Remote WebAPI anonymous health path.</summary>
    private const string RemoteHealthPath = "/api/v1/health";

    /// <summary>LocalWebAPI anonymous health path.</summary>
    private const string LocalHealthPath = "/api/health";

    private static readonly TimeSpan RemoteProbeTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan LocalProbeTimeout = TimeSpan.FromSeconds(2);

    private readonly IConnectionSettingsService _connectionSettings;
    private readonly IApplicationStateService _applicationState;
    private readonly ILogger<ConnectionModeService> _logger;
    private readonly SemaphoreSlim _detectGate = new(1, 1);

    private ConnectionMode _currentMode;
    private bool _isRemoteAvailable;

    /// <summary>
    /// Build the service. The initial <see cref="CurrentMode"/> is derived
    /// from <see cref="IConnectionSettingsService.PreferredMode"/> /
    /// <see cref="IConnectionSettingsService.IsLocal"/>.
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

    /// <summary>API status message including mode info.</summary>
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
    /// Update the effective mode and raise <see cref="ModeChanged"/> when it changes.
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
    /// When the URL is changed externally, re-derive the effective mode.
    /// </summary>
    private void OnUrlChanged(object? sender, string newUrl)
    {
        var derived = _connectionSettings.IsLocal
            ? ConnectionMode.Local
            : ConnectionMode.Remote;
        ApplyMode(derived);
    }

    /// <summary>
    /// Release the detection gate and detach the URL subscription.
    /// The DI container disposes singletons on shutdown.
    /// </summary>
    public void Dispose()
    {
        _connectionSettings.UrlChanged -= OnUrlChanged;
        _detectGate.Dispose();
    }
}
