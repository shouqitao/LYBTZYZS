// ---------------------------------------------------------------------------
// IApiRouter — API connection query interface (simplified)
// ---------------------------------------------------------------------------
// Provides read-only access to the current connection URL and local-mode
// status. No manual override or mode switching — URL is managed by
// IConnectionSettingsService.
// ---------------------------------------------------------------------------

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// Read-only query interface for the current API connection.
/// Delegates to <see cref="IConnectionSettingsService"/>.
/// </summary>
public interface IApiRouter
{
    /// <summary>Current connection URL.</summary>
    string CurrentUrl { get; }

    /// <summary>Whether the current URL points to a local service.</summary>
    bool IsLocal { get; }
}
