// ---------------------------------------------------------------------------
// IConnectionSettingsService — Connection URL management interface
// ---------------------------------------------------------------------------
// Manages the active connection URL for API access. Replaces the ApiMode
// concept with URL-driven connection selection:
//   - 127.0.0.1 / localhost → HttpClientApiClient (LocalWebAPI)
//   - any other address → RefitApiClient (Remote WebAPI)
// ---------------------------------------------------------------------------

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// Manages the active API connection URL and persists it across sessions.
/// </summary>
public interface IConnectionSettingsService
{
    /// <summary>Current connection URL (e.g., "http://192.168.190.248:5000").</summary>
    string CurrentUrl { get; }

    /// <summary>
    /// Whether the current URL points to a local service
    /// (contains "127.0.0.1" or "localhost").
    /// </summary>
    bool IsLocal { get; }

    /// <summary>
    /// Set a new connection URL, persist it, and notify subscribers.
    /// </summary>
    /// <param name="url">The new URL (e.g., "http://192.168.190.248:5000").</param>
    Task SetUrlAsync(string url);

    /// <summary>Fires when the connection URL changes. Payload is the new URL.</summary>
    event EventHandler<string>? UrlChanged;

    /// <summary>
    /// Validates whether a URL string is a well-formed HTTP URL.
    /// </summary>
    /// <param name="url">URL string to validate.</param>
    /// <returns>True if the URL starts with "http://" or "https://" and is a valid URI.</returns>
    bool IsValidUrl(string url);
}
