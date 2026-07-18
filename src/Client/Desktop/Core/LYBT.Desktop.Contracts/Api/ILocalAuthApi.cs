using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for Auth endpoints.
/// Method signatures aligned with IAuthApi (remote).
/// </summary>
public interface ILocalAuthApi
{
    [Refit.Post("/api/v1/auth/login")]
    Task<LoginResponse> LoginAsync([Refit.Body] LoginRequest request);

    [Refit.Post("/api/v1/auth/auto-login")]
    Task<LoginResponse> AutoLoginAsync([Refit.Body] AutoLoginRequest request);

    [Refit.Post("/api/v1/auth/logout")]
    Task LogoutAsync([Refit.Body] LogoutRequest request);

    [Refit.Post("/api/v1/auth/refresh")]
    Task<LoginResponse> RefreshAsync([Refit.Body] RefreshTokenRequest request);

    [Refit.Get("/api/v1/auth/validate")]
    Task<ValidateTokenResponse> ValidateTokenAsync([Refit.Body] ValidateTokenRequest request);

    [Refit.Get("/api/v1/auth/validate")]
    Task<object> ValidateTokenFromHeaderAsync();
}
