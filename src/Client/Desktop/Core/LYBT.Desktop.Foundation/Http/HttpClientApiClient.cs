// ---------------------------------------------------------------------------
// HttpClientApiClient — LocalWebAPI IApiClient facade
// ---------------------------------------------------------------------------
// Facade that lazily creates per-domain adapter classes ({Domain}HttpApiClient)
// implementing each IApiClient sub-interface.
//
// This is the counterpart to RefitApiClient (Remote mode).
// Routes to LocalWebAPI controllers via /api/ prefix (no version).
// LocalWebAPI returns raw DTOs; adapters wrap them in ApiResponse<T>.
// ---------------------------------------------------------------------------

using System.Net.Http;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Foundation.Http.Clients;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// LocalWebAPI 模式 API 客户端——使用 IHttpClientFactory 调用本地 ASP.NET Core 端点。
/// 通过首次访问时惰性创建各领域适配器
/// （<see cref="IdentityHttpApiClient"/> 等）实现 <see cref="IApiClient"/>。
/// </summary>
public sealed class HttpClientApiClient : IApiClient, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    private IApiClientIdentity? _identity;
    private IApiClientPatients? _patients;
    private IApiClientHerbs? _herbs;
    private IApiClientFormulas? _formulas;
    private IApiClientMedicalCases? _medicalCases;
    private IApiClientRegistrations? _registrations;
    private IApiClientReports? _reports;
    private IApiClientDeploy? _deploy;
    private IApiClientDiagnostics? _diagnostics;
    private IApiClientConfiguration? _configuration;
    private IApiClientBackup? _backup;

    /// <summary>
    /// 初始化 <see cref="HttpClientApiClient"/> 的新实例。
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating named HttpClient instances.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClientFactory"/> is null.</exception>
    public HttpClientApiClient(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IApiClientIdentity Identity => _identity ??= new IdentityHttpApiClient(_httpClientFactory, _logger);

    /// <inheritdoc />
    public IApiClientPatients Patients => _patients ??= new PatientsHttpApiClient(_httpClientFactory, _logger);

    /// <inheritdoc />
    public IApiClientHerbs Herbs => _herbs ??= new HerbsHttpApiClient(_httpClientFactory, _logger);

    /// <inheritdoc />
    public IApiClientFormulas Formulas => _formulas ??= new FormulasHttpApiClient(_httpClientFactory, _logger);

    /// <inheritdoc />
    public IApiClientMedicalCases MedicalCases => _medicalCases ??= new MedicalCasesHttpApiClient(_httpClientFactory, _logger);

    /// <inheritdoc />
    public IApiClientRegistrations Registrations => _registrations ??= new RegistrationsHttpApiClient(_httpClientFactory, _logger);

    /// <inheritdoc />
    public IApiClientReports Reports => _reports ??= new ReportsHttpApiClient(_httpClientFactory, _logger);

    /// <inheritdoc />
    public IApiClientDeploy Deploy => _deploy ??= new DeployHttpApiClient(_httpClientFactory, _logger);

    /// <inheritdoc />
    public IApiClientDiagnostics Diagnostics => _diagnostics ??= new DiagnosticsHttpApiClient(_httpClientFactory, _logger);

    /// <inheritdoc />
    public IApiClientConfiguration Configuration => _configuration ??= new ConfigurationHttpApiClient(_httpClientFactory, _logger);

    /// <inheritdoc />
    public IApiClientBackup Backup => _backup ??= new BackupHttpApiClient(_httpClientFactory, _logger);
    /// <summary>
    /// 释放惰性子接口实例（不释放 IHttpClientFactory——由 DI 容器管理，ADR-0021）。
    /// </summary>
    public void Dispose()
    {
        _identity = null;
        _patients = null;
        _herbs = null;
        _formulas = null;
        _medicalCases = null;
        _registrations = null;
        _reports = null;
        _deploy = null;
        _diagnostics = null;
        _configuration = null;
        _backup = null;
    }
}
