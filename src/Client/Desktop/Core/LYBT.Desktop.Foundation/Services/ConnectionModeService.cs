// ---------------------------------------------------------------------------
// ConnectionModeService — Remote/Local mode detection and transparent fallback
// ---------------------------------------------------------------------------
// Wraps IConnectionSettingsService (URL) and IApplicationStateService (health)
// to expose a mode-oriented view of the connection: probe remote health, fall
// back to embedded LocalWebAPI when the remote server is unreachable, and keep
// subscribers in sync via ModeChanged.
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
    /// <summary>LocalWebAPI default base URL (anonymous health endpoint).</summary>
    private const string LocalBaseUrl = "http://localhost:5000";

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

    /// <summary>
    /// Build the service. The initial <see cref="CurrentMode"/> is derived from
    /// the configured URL (local URL → Local, otherwise Remote).
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

        // Keep this service's view in sync if the URL is changed elsewhere.
        _connectionSettings.UrlChanged += OnUrlChanged;
    }

    /// <inheritdoc />
    public ConnectionMode CurrentMode => _currentMode;

    /// <inheritdoc />
    public string CurrentModeDisplay => _currentMode == ConnectionMode.Remote
        ? "远程模式"
        : "本地模式";

    /// <inheritdoc />
    public bool IsRemote => _currentMode == ConnectionMode.Remote;

    /// <inheritdoc />
    public bool IsLocal => _currentMode == ConnectionMode.Local;

    /// <inheritdoc />
    public event EventHandler<ConnectionMode>? ModeChanged;

    /// <inheritdoc />
    public async Task<ConnectionMode> DetectBestModeAsync()
    {
        // Serialize concurrent detections so two callers cannot race the mode flag.
        await _detectGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var currentUrl = _connectionSettings.CurrentUrl;

            // Already pointed at local → nothing to detect.
            if (_connectionSettings.IsLocal)
            {
                ApplyMode(ConnectionMode.Local);
                return ConnectionMode.Local;
            }

            // Probe the configured remote server.
            if (await TestRemoteConnectionAsync(currentUrl).ConfigureAwait(false))
            {
                _logger.LogInformation(
                    "[CONNECTION-MODE] Remote server reachable at {Url} → Remote mode", currentUrl);
                ApplyMode(ConnectionMode.Remote);
                return ConnectionMode.Remote;
            }

            // Remote unreachable → transparent fallback to embedded LocalWebAPI.
            _logger.LogWarning(
                "[CONNECTION-MODE] Remote server unreachable at {Url}, falling back to Local mode", currentUrl);
            await SwitchUrlAsync(LocalBaseUrl).ConfigureAwait(false);
            ApplyMode(ConnectionMode.Local);
            return ConnectionMode.Local;
        }
        finally
        {
            _detectGate.Release();
        }
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
        var healthUrl = $"{LocalBaseUrl}{LocalHealthPath}";
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
                // Drive the URL to the embedded LocalWebAPI; the URL change
                // propagates back through OnUrlChanged and sets the mode.
                SwitchUrlFireAndForget(LocalBaseUrl);
                break;

            case ConnectionMode.Remote:
                // Keep the current URL (assumed already remote). If the URL is
                // currently local we cannot invent a remote address — the caller
                // is expected to have set one via the login UI first.
                if (_connectionSettings.IsLocal)
                {
                    _logger.LogWarning(
                        "[CONNECTION-MODE] SetMode(Remote) called while URL is local ({Url}); "
                        + "update the connection URL before switching to Remote",
                        _connectionSettings.CurrentUrl);
                }
                ApplyMode(ConnectionMode.Remote);
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
    /// Update the connection URL asynchronously. <see cref="IConnectionSettingsService.SetUrlAsync"/>
    /// assigns its internal URL field synchronously before the first await, so
    /// <see cref="IConnectionSettingsService.CurrentUrl"/> is consistent immediately
    /// after this returns; only persistence is deferred.
    /// </summary>
    private async Task SwitchUrlAsync(string url)
    {
        try
        {
            await _connectionSettings.SetUrlAsync(url).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CONNECTION-MODE] Failed to switch URL to {Url}", url);
        }
    }

    /// <summary>
    /// Fire-and-forget variant for the synchronous <see cref="SetMode"/> entry point.
    /// The URL field updates synchronously inside SetUrlAsync before the first await.
    /// </summary>
    private async void SwitchUrlFireAndForget(string url)
    {
        try
        {
            await _connectionSettings.SetUrlAsync(url).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CONNECTION-MODE] Failed to switch URL to {Url}", url);
        }
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
