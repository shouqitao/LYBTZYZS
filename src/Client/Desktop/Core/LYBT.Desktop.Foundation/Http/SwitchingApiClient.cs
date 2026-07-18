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

    private volatile IApiClient? _current;
    private volatile string? _currentUrl;
    private readonly object _lock = new();
    private bool _disposed;

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
    /// 无锁快路径：URL 未变时直接返回缓存的客户端（99.9% 命中）。
    /// URL 变更时才进入 lock 创建新客户端。
    /// </summary>
    private IApiClient Current
    {
        get
        {
            var current = _current;
            var url = _connectionSettings.CurrentUrl;

            // 快路径：URL 未变，直接返回缓存实例
            if (current is not null && _currentUrl == url)
                return current;

            // 慢路径：URL 变更或首次初始化，加锁创建
            lock (_lock)
            {
                // 双重检查（可能被其他线程抢先创建）
                if (_current is not null && _currentUrl == url)
                    return _current;

                var oldClient = _current as IDisposable;
                _current = _connectionSettings.IsLocal
                    ? new HttpClientApiClient(_localHttpClientFactory(url))
                    : new RefitApiClient(_remoteHttpClientFactory(url), _refitSettings);
                _currentUrl = url;
                oldClient?.Dispose();
                return _current;
            }
        }
    }

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

    /// <inheritdoc />
    public IApiClientReports Reports => Current.Reports;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        (_current as IDisposable)?.Dispose();
    }
}
