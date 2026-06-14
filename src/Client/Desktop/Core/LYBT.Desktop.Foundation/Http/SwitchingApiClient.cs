// ---------------------------------------------------------------------------
// SwitchingApiClient — Runtime-switchable IApiClient proxy
// ---------------------------------------------------------------------------
// Routes each property access to the correct underlying implementation
// (HttpClientApiClient or RefitApiClient) based on the current connection URL.
//
//   localhost / 127.0.0.1 → HttpClientApiClient (LocalWebAPI)
//   any other address    → RefitApiClient       (Remote WebAPI)
//
// Repository layer is completely unaware — same IApiClient, same behavior.
// ---------------------------------------------------------------------------

using System.Net.Http;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Services;
using Refit;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// Runtime-switchable API client that delegates to either
/// <see cref="RefitApiClient"/> or <see cref="HttpClientApiClient"/>
/// based on the connection URL.
/// </summary>
public sealed class SwitchingApiClient : IApiClient, IDisposable
{
    private readonly IConnectionSettingsService _connectionSettings;
    private readonly Func<string, HttpClient> _remoteHttpClientFactory;
    private readonly Func<string, IHttpClientFactory> _localHttpClientFactory;
    private readonly RefitSettings _refitSettings;

    private IApiClient? _current;
    private string? _currentUrl;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="SwitchingApiClient"/>.
    /// </summary>
    /// <param name="connectionSettings">Connection URL provider.</param>
    /// <param name="remoteHttpClientFactory">Factory for Remote-mode HttpClient with handler chain.</param>
    /// <param name="localHttpClientFactory">Factory for Local-mode IHttpClientFactory.</param>
    /// <param name="refitSettings">Refit serialization settings.</param>
    public SwitchingApiClient(
        IConnectionSettingsService connectionSettings,
        Func<string, HttpClient> remoteHttpClientFactory,
        Func<string, IHttpClientFactory> localHttpClientFactory,
        RefitSettings refitSettings)
    {
        _connectionSettings = connectionSettings
            ?? throw new ArgumentNullException(nameof(connectionSettings));
        _remoteHttpClientFactory = remoteHttpClientFactory
            ?? throw new ArgumentNullException(nameof(remoteHttpClientFactory));
        _localHttpClientFactory = localHttpClientFactory
            ?? throw new ArgumentNullException(nameof(localHttpClientFactory));
        _refitSettings = refitSettings
            ?? throw new ArgumentNullException(nameof(refitSettings));
    }

    /// <summary>
    /// Current active API client, resolved from URL.
    /// Throws if no client could be created.
    /// </summary>
    private IApiClient Current
    {
        get
        {
            lock (_lock)
            {
                var url = _connectionSettings.CurrentUrl;
                if (_current is null || _currentUrl != url)
                {
                    var oldClient = _current as IDisposable;
                    _current = _connectionSettings.IsLocal
                        ? new HttpClientApiClient(_localHttpClientFactory(url))
                        : new RefitApiClient(_remoteHttpClientFactory(url), _refitSettings);
                    _currentUrl = url;
                    oldClient?.Dispose();
                }
                return _current;
            }
        }
    }

    // ========== IApiClient property delegation ==========

    /// <inheritdoc />
    public IApiClientAuth Auth => Current.Auth;

    /// <inheritdoc />
    public IApiClientUsers Users => Current.Users;

    /// <inheritdoc />
    public IApiClientPatients Patients => Current.Patients;

    /// <inheritdoc />
    public IApiClientHerbs Herbs => Current.Herbs;

    /// <inheritdoc />
    public IApiClientFormulas Formulas => Current.Formulas;

    /// <inheritdoc />
    public IApiClientMedicalCases MedicalCases => Current.MedicalCases;

    /// <inheritdoc />
    public IApiClientRegistrations Registrations => Current.Registrations;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        (_current as IDisposable)?.Dispose();
    }
}
