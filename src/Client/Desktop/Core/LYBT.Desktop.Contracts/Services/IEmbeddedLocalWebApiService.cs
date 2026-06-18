namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// Manages the embedded LocalWebAPI Kestrel server lifecycle within the WPF process.
/// </summary>
public interface IEmbeddedLocalWebApiService
{
    /// <summary>True if the server is currently running.</summary>
    bool IsRunning { get; }

    /// <summary>The base URL the server is listening on.</summary>
    string BaseUrl { get; }

    /// <summary>Start the embedded server. Idempotent — no-op if already running.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stop the embedded server gracefully.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
