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
/// 认证 API 客户端——包装 IAuthApi（Refit）以实现 IApiClientAuth。
/// </summary>
internal sealed class AuthApiClient : IApiClientAuth
{
    private readonly IAuthApi _api;

    /// <summary>
    /// 初始化 <see cref="AuthApiClient"/> 类的新实例。
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
    public Task<ApiResponse<ValidateTokenResponse>> ValidateTokenAsync()
        => _api.ValidateTokenAsync();

    /// <inheritdoc />
    public Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync()
        => _api.HealthCheckAsync();
}
