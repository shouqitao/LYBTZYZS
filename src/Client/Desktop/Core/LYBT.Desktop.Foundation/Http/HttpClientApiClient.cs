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
/// LocalWebAPI mode API client — uses IHttpClientFactory to call local ASP.NET Core endpoints.
/// Implements <see cref="IApiClient"/> by lazily creating a per-domain adapter
/// (<see cref="AuthHttpApiClient"/>, <see cref="UsersHttpApiClient"/>, etc.) on first access.
/// </summary>
public sealed class HttpClientApiClient : IApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    private IApiClientAuth? _auth;
    private IApiClientUsers? _users;
    private IApiClientPatients? _patients;
    private IApiClientHerbs? _herbs;
    private IApiClientFormulas? _formulas;
    private IApiClientMedicalCases? _medicalCases;
    private IApiClientRegistrations? _registrations;
    private IApiClientReports? _reports;
    private IApiClientDeploy? _deploy;
    private IApiClientDiagnostics? _diagnostics;

    /// <summary>
    /// Initializes a new instance of <see cref="HttpClientApiClient"/>.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating named HttpClient instances.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClientFactory"/> is null.</exception>
    public HttpClientApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <inheritdoc />
    public IApiClientAuth Auth => _auth ??= new AuthHttpApiClient(_httpClientFactory);

    /// <inheritdoc />
    public IApiClientUsers Users => _users ??= new UsersHttpApiClient(_httpClientFactory);

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
}
