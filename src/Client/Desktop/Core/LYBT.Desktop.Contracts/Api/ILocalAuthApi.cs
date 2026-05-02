using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for Auth endpoints.
/// Method signatures aligned with IAuthApi (remote).
/// </summary>
public interface ILocalAuthApi
{
    [Refit.Post("/api/auth/login")]
    Task<LoginResponse> LoginAsync([Refit.Body] LoginRequest request);

    [Refit.Post("/api/auth/auto-login")]
    Task<LoginResponse> AutoLoginAsync([Refit.Body] AutoLoginRequest request);

    [Refit.Post("/api/auth/logout")]
    Task LogoutAsync([Refit.Body] LogoutRequest request);

    [Refit.Post("/api/auth/refresh")]
    Task<LoginResponse> RefreshAsync([Refit.Body] RefreshTokenRequest request);

    [Refit.Get("/api/auth/validate")]
    Task<ValidateTokenResponse> ValidateTokenAsync([Refit.Body] ValidateTokenRequest request);

    [Refit.Get("/api/auth/validate")]
    Task<object> ValidateTokenFromHeaderAsync();
}
