// ---------------------------------------------------------------------------
// RefitApiClient — Remote IApiClient implementation using Refit
// ---------------------------------------------------------------------------
// Creates Refit-generated HTTP clients for each domain API interface
// and wraps them in adapter classes that implement IApiClient sub-interfaces.
//
// This class is registered as IApiClient in Remote mode. The adapter pattern
// bridges the gap between Refit-attributed interfaces (IAuthApi, IUserApi, etc.)
// and the plain sub-interfaces (IApiClientIdentity, IApiClientPatients, etc.) that
// define the unified contract without Refit dependencies.
// ---------------------------------------------------------------------------

using System.Net.Http;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Foundation.Http.Clients;
using Refit;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// 使用 Refit 生成的 HTTP 客户端的远程 API 客户端实现。
/// 为每个领域 API 创建 Refit 实例并将其包装在适配器类中实现 <see cref="IApiClient"/>。
/// </summary>
/// <remarks>
/// <para>Each property lazily creates a Refit instance via <see cref="RestService.For{T}(HttpClient, RefitSettings)"/>
/// and wraps it in a corresponding adapter (e.g., <see cref="IdentityApiClient"/>).</para>
/// <para>The shared <see cref="HttpClient"/> has the full handler chain:
/// HttpClientHandler → TokenRefreshHandler → AuthorizationMessageHandler → LoggingHttpHandler.</para>
/// <para>Local-only methods on sub-interfaces (e.g., GetCurrentUserAsync)
/// throw <see cref="NotSupportedException"/> in this remote-mode implementation.</para>
/// </remarks>
public sealed class RefitApiClient : IApiClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly RefitSettings _refitSettings;

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
    /// 初始化 <see cref="RefitApiClient"/> 的新实例。
    /// </summary>
    /// <param name="httpClient">
    /// Shared HttpClient with the configured handler chain
    /// (HttpClientHandler → TokenRefreshHandler → AuthorizationMessageHandler → LoggingHttpHandler).
    /// </param>
    /// <param name="refitSettings">
    /// Refit serialization settings (camelCase, StringEnumConverter, etc.).
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/> or <paramref name="refitSettings"/> is null.</exception>
    public RefitApiClient(HttpClient httpClient, RefitSettings refitSettings)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _refitSettings = refitSettings ?? throw new ArgumentNullException(nameof(refitSettings));
    }

    /// <inheritdoc />
    public IApiClientIdentity Identity => _identity ??= new IdentityApiClient(
        RestService.For<IAuthApi>(_httpClient, _refitSettings),
        RestService.For<IUserApi>(_httpClient, _refitSettings));

    /// <inheritdoc />
    public IApiClientPatients Patients => _patients ??= new PatientApiClient(
        RestService.For<IPatientApi>(_httpClient, _refitSettings));

    /// <inheritdoc />
    public IApiClientHerbs Herbs => _herbs ??= new HerbApiClient(
        RestService.For<IHerbApi>(_httpClient, _refitSettings));

    /// <inheritdoc />
    public IApiClientFormulas Formulas => _formulas ??= new FormulaApiClient(
        RestService.For<IFormulaApi>(_httpClient, _refitSettings));

    /// <inheritdoc />
    public IApiClientMedicalCases MedicalCases => _medicalCases ??= new MedicalCaseApiClient(
        RestService.For<IMedicalCaseApi>(_httpClient, _refitSettings));

    /// <inheritdoc />
    public IApiClientRegistrations Registrations => _registrations ??= new RegistrationApiClient(
        RestService.For<IRegistrationApi>(_httpClient, _refitSettings));

    /// <inheritdoc />
    public IApiClientReports Reports => _reports ??= new ReportsApiClient(
        RestService.For<IReportsApi>(_httpClient, _refitSettings));

    /// <inheritdoc />
    public IApiClientDeploy Deploy => _deploy ??= new DeployApiClient(
        RestService.For<IDeployApi>(_httpClient, _refitSettings));

    /// <inheritdoc />
    public IApiClientDiagnostics Diagnostics => _diagnostics ??= new DiagnosticsApiClient(
        RestService.For<IDiagnosticsApi>(_httpClient, _refitSettings));

    /// <inheritdoc />
    public IApiClientConfiguration Configuration => _configuration ??= new ConfigurationApiClient(
        RestService.For<IConfigurationApi>(_httpClient, _refitSettings));
    /// <summary>
    /// 释放惰性创建的 Refit 代理（不释放共享 HttpClient——由外部 handler 链管理，ADR-0021）。
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
    }
}
