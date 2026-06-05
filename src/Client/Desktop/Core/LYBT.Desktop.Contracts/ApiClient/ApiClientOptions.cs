// ---------------------------------------------------------------------------
// ApiClientOptions — HttpClient Configuration for the Unified API Client
// ---------------------------------------------------------------------------
// Configures timeout, retry, and base URL overrides for the IApiClient
// infrastructure. Consumed by the HttpClient/Refit registration pipeline.
// ---------------------------------------------------------------------------

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// Configuration options for the unified API client infrastructure.
/// Controls HTTP timeout, retry behavior, and optional base URL overrides
/// for remote and local API endpoints.
/// </summary>
/// <remarks>
/// <para>
/// These options are distinct from <c>LYBT.Shared.Configuration.Options.Client.ApiClientOptions</c>,
/// which configures the legacy per-module Refit clients. This class targets the new
/// unified <see cref="IApiClient"/> pipeline.
/// </para>
/// <para>
/// Typical configuration source: <c>appsettings.json</c> section <c>ApiClient</c>.
/// </para>
/// </remarks>
public sealed class ApiClientOptions
{
    /// <summary>
    /// Configuration section name in <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "ApiClient";

    /// <summary>
    /// HTTP request timeout in seconds.
    /// </summary>
    /// <value>Default is <c>30</c> seconds. Valid range: 5–300.</value>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of automatic retries for transient HTTP failures.
    /// </summary>
    /// <value>Default is <c>3</c>. Set to <c>0</c> to disable retries.</value>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Override base URL for the remote (server-hosted) API endpoint.
    /// </summary>
    /// <value>
    /// When <c>null</c>, the default remote base URL from the connection mode
    /// service is used. Set this to override at configuration level.
    /// </value>
    public string? RemoteBaseUrl { get; set; }

    /// <summary>
    /// Override base URL for the local (embedded LocalWebAPI) endpoint.
    /// </summary>
    /// <value>
    /// When <c>null</c>, the default local base URL from the connection mode
    /// service is used. Set this to override at configuration level.
    /// </value>
    public string? LocalBaseUrl { get; set; }
}
