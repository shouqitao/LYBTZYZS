// ---------------------------------------------------------------------------
// RefitApiClient — Remote IApiClient implementation using Refit
// ---------------------------------------------------------------------------
// Creates Refit-generated HTTP clients for each domain API interface
// and wraps them in adapter classes that implement IApiClient sub-interfaces.
//
// This class is registered as IApiClient in Remote mode. The adapter pattern
// bridges the gap between Refit-attributed interfaces (IAuthApi, IUserApi, etc.)
// and the plain sub-interfaces (IApiClientAuth, IApiClientUsers, etc.) that
// define the unified contract without Refit dependencies.
// ---------------------------------------------------------------------------

using System.Net.Http;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Foundation.Http.Clients;
using Refit;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// Remote API client implementation using Refit-generated HTTP clients.
/// Implements <see cref="IApiClient"/> by creating Refit instances for each
/// domain API and wrapping them in adapter classes.
/// </summary>
/// <remarks>
/// <para>Each property lazily creates a Refit instance via <see cref="RestService.For{T}(HttpClient, RefitSettings)"/>
/// and wraps it in a corresponding adapter (e.g., <see cref="AuthApiClient"/>).</para>
/// <para>The shared <see cref="HttpClient"/> has the full handler chain:
/// HttpClientHandler → TokenRefreshHandler → AuthorizationMessageHandler → LoggingHttpHandler.</para>
/// <para>Local-only methods on sub-interfaces (e.g., GetCurrentUserAsync, GetCategoriesAsync)
/// throw <see cref="NotSupportedException"/> in this remote-mode implementation.</para>
/// </remarks>
public sealed class RefitApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly RefitSettings _refitSettings;

    private IApiClientAuth? _auth;
    private IApiClientUsers? _users;
    private IApiClientPatients? _patients;
    private IApiClientHerbs? _herbs;
    private IApiClientFormulas? _formulas;
    private IApiClientMedicalCases? _medicalCases;
    private IApiClientRegistrations? _registrations;

    /// <summary>
    /// Initializes a new instance of <see cref="RefitApiClient"/>.
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
    public IApiClientAuth Auth => _auth ??= new AuthApiClient(
        RestService.For<IAuthApi>(_httpClient, _refitSettings));

    /// <inheritdoc />
    public IApiClientUsers Users => _users ??= new UserApiClient(
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
}
