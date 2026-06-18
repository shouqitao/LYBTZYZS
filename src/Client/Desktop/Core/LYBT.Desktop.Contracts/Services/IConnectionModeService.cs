// ---------------------------------------------------------------------------
// IConnectionModeService — Connection mode detection and switching
// ---------------------------------------------------------------------------
// Abstracts the dual-mode (Remote / Local) connection model on top of the
// URL-driven IConnectionSettingsService. Provides health probing, automatic
// best-mode detection, and a UI-friendly mode descriptor for the login screen.
// ---------------------------------------------------------------------------

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// Connection mode qualifier. <see cref="Auto"/> is only used as a request
/// to detect the best mode; the actual <see cref="IConnectionModeService.CurrentMode"/>
/// resolves to either <see cref="Remote"/> or <see cref="Local"/>.
/// </summary>
public enum ConnectionMode
{
    /// <summary>Remote WebAPI (HTTP → SQL Server).</summary>
    Remote,

    /// <summary>Embedded LocalWebAPI (→ SQL Server LocalDB).</summary>
    Local,

    /// <summary>Probe remote first, fall back to local (detection request only).</summary>
    Auto
}

/// <summary>
/// Detects and manages the active connection mode (Remote vs Local), providing
/// transparent fallback when the remote server is unreachable.
/// </summary>
public interface IConnectionModeService
{
    /// <summary>
    /// The currently effective mode. Always resolves to <see cref="ConnectionMode.Remote"/>
    /// or <see cref="ConnectionMode.Local"/> after initialization.
    /// </summary>
    ConnectionMode CurrentMode { get; }

    /// <summary>
    /// Localized display label for the current mode
    /// ("远程模式" for Remote, "本地模式" for Local).
    /// </summary>
    string CurrentModeDisplay { get; }

    /// <summary>True when the effective mode is <see cref="ConnectionMode.Remote"/>.</summary>
    bool IsRemote { get; }

    /// <summary>True when the effective mode is <see cref="ConnectionMode.Local"/>.</summary>
    bool IsLocal { get; }

    /// <summary>API status display text with mode info (e.g., "远程 WebAPI 已连接").</summary>
    string ApiStatusDisplay { get; }

    /// <summary>
    /// Probe the configured remote URL and select the best mode automatically.
    /// Falls back to Local when the remote server is unreachable.
    /// </summary>
    /// <returns>The resolved effective mode (Remote or Local).</returns>
    Task<ConnectionMode> DetectBestModeAsync();

    /// <summary>
    /// Test whether the remote WebAPI at <paramref name="url"/> is reachable.
    /// Performs an anonymous GET against <c>{url}/api/v1/health</c>.
    /// </summary>
    /// <param name="url">Remote server base URL (e.g., "http://192.168.1.10:5000").</param>
    /// <returns>True when the health endpoint responded successfully.</returns>
    Task<bool> TestRemoteConnectionAsync(string url);

    /// <summary>
    /// Test whether the embedded LocalWebAPI is reachable on its default port.
    /// </summary>
    /// <returns>True when <c>http://localhost:5000/api/health</c> responded successfully.</returns>
    Task<bool> TestLocalConnectionAsync();

    /// <summary>
    /// Explicitly switch the effective mode. Updates the underlying
    /// <see cref="IConnectionSettingsService"/> URL accordingly:
    /// <list type="bullet">
    ///   <item><see cref="ConnectionMode.Local"/> → points the URL at localhost.</item>
    ///   <item><see cref="ConnectionMode.Remote"/> → keeps the current remote URL.</item>
    ///   <item><see cref="ConnectionMode.Auto"/> → triggers background detection.</item>
    /// </list>
    /// </summary>
    /// <param name="mode">The mode to activate.</param>
    void SetMode(ConnectionMode mode);

    /// <summary>
    /// Raised whenever <see cref="CurrentMode"/> changes. Payload is the new mode.
    /// </summary>
    event EventHandler<ConnectionMode>? ModeChanged;
}
