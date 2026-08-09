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
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Foundation.Http.Clients;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// LocalWebAPI 模式 API 客户端——使用 IHttpClientFactory 调用本地 ASP.NET Core 端点。
/// 通过首次访问时惰性创建各领域适配器
/// （<see cref="IdentityHttpApiClient"/> 等）实现 <see cref="IApiClient"/>。
/// </summary>
public sealed class HttpClientApiClient : IApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

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

    /// <summary>
    /// 初始化 <see cref="HttpClientApiClient"/> 的新实例。
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating named HttpClient instances.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClientFactory"/> is null.</exception>
    public HttpClientApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <inheritdoc />
    public IApiClientIdentity Identity => _identity ??= new IdentityHttpApiClient(_httpClientFactory);

    /// <inheritdoc />
    public IApiClientPatients Patients => _patients ??= new PatientsHttpApiClient(_httpClientFactory);

    /// <inheritdoc />
    public IApiClientHerbs Herbs => _herbs ??= new HerbsHttpApiClient(_httpClientFactory);

    /// <inheritdoc />
    public IApiClientFormulas Formulas => _formulas ??= new FormulasHttpApiClient(_httpClientFactory);

    /// <inheritdoc />
    public IApiClientMedicalCases MedicalCases => _medicalCases ??= new MedicalCasesHttpApiClient(_httpClientFactory);

    /// <inheritdoc />
    public IApiClientRegistrations Registrations => _registrations ??= new RegistrationsHttpApiClient(_httpClientFactory);

    /// <inheritdoc />
    public IApiClientReports Reports => _reports ??= new ReportsHttpApiClient(_httpClientFactory);

    /// <inheritdoc />
    public IApiClientDeploy Deploy => _deploy ??= new DeployHttpApiClient(_httpClientFactory);

    /// <inheritdoc />
    public IApiClientDiagnostics Diagnostics => _diagnostics ??= new DiagnosticsHttpApiClient(_httpClientFactory);

    /// <inheritdoc />
    public IApiClientConfiguration Configuration => _configuration ??= new ConfigurationHttpApiClient(_httpClientFactory);
}
