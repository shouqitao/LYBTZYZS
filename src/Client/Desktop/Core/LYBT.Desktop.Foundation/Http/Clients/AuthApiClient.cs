// ---------------------------------------------------------------------------
// AuthApiClient — Refit adapter for IApiClientAuth
// ---------------------------------------------------------------------------
// Delegates all calls to IAuthApi (Refit-generated HTTP client).
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>
/// Authentication API client — wraps IAuthApi (Refit) to implement IApiClientAuth.
/// </summary>
internal sealed class AuthApiClient : IApiClientAuth
{
    private readonly IAuthApi _api;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthApiClient"/>.
    /// </summary>
    /// <param name="api">Refit-generated authentication API client.</param>
    public AuthApiClient(IAuthApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest loginRequest)
        => _api.LoginAsync(loginRequest);

    /// <inheritdoc />
    public Task<ApiResponse<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request)
        => _api.LoginWithAutoTokenAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse> LogoutAsync(LogoutRequest logoutRequest)
        => _api.LogoutAsync(logoutRequest);

    /// <inheritdoc />
    public Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        => _api.RefreshTokenAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<object>> ValidateTokenFromHeaderAsync()
        => _api.ValidateTokenFromHeaderAsync();

    /// <inheritdoc />
    public Task<ApiResponse<ValidateTokenResponse>> ValidateTokenAsync(ValidateTokenRequest request)
        => _api.ValidateTokenAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync()
        => _api.HealthCheckAsync();
}
