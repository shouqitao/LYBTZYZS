// ---------------------------------------------------------------------------
// AuthHttpApiClient — HttpClient adapter for IApiClientAuth
// ---------------------------------------------------------------------------
// LocalWebAPI mode implementation of IApiClientAuth (split from HttpClientApiClient).
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式认证 API 客户端（拆分自 HttpClientApiClient）</summary>
internal sealed class AuthHttpApiClient : HttpApiClientBase, IApiClientAuth
{
    public AuthHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest loginRequest)
        => PostAndWrapAsync<LoginResponse>("/api/v1/auth/login", loginRequest);

    public Task<ApiResponse<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request)
        => PostAndWrapAsync<LoginResponse>("/api/v1/auth/auto-login", request);

    public async Task<ApiResponse> LogoutAsync(LogoutRequest logoutRequest)
    {
        await PostVoidAsync("/api/v1/auth/logout", logoutRequest);
        return WrapSuccess();
    }

    public Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        => PostAndWrapAsync<LoginResponse>("/api/v1/auth/refresh", request);

    public Task<ApiResponse<ValidateTokenResponse>> ValidateTokenAsync()
        => GetAndWrapAsync<ValidateTokenResponse>("/api/v1/auth/validate");

    public Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync()
        => GetAndWrapAsync<HealthCheckResponse>("/api/v1/health");
}
