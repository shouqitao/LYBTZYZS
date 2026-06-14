// ---------------------------------------------------------------------------
// ApiRouter — API connection query implementation (simplified)
// ---------------------------------------------------------------------------
// Thin wrapper around IConnectionSettingsService. Exposes CurrentUrl and
// IsLocal for consumers that need to know the current connection status.
// ---------------------------------------------------------------------------

using LYBT.Desktop.Contracts.Services;

namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// Read-only API connection query. Delegates to <see cref="IConnectionSettingsService"/>.
/// </summary>
public sealed class ApiRouter : IApiRouter
{
    private readonly IConnectionSettingsService _connectionSettings;

    public ApiRouter(IConnectionSettingsService connectionSettings)
    {
        _connectionSettings = connectionSettings
            ?? throw new ArgumentNullException(nameof(connectionSettings));
    }

    /// <inheritdoc />
    public string CurrentUrl => _connectionSettings.CurrentUrl;

    /// <inheritdoc />
    public bool IsLocal => _connectionSettings.IsLocal;
}
