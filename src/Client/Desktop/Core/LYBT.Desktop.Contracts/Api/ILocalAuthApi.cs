using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for Auth endpoints.
/// </summary>
public interface ILocalAuthApi
{
    [Refit.Post("/api/auth/login")]
    Task<object> LoginAsync([Refit.Body] LoginRequest request);

    [Refit.Post("/api/auth/logout")]
    Task LogoutAsync();

    [Refit.Post("/api/auth/refresh")]
    Task<object> RefreshAsync();

    [Refit.Get("/api/auth/validate")]
    Task<object> ValidateTokenAsync();

    [Refit.Post("/api/auth/auto-login")]
    Task<object> AutoLoginAsync([Refit.Body] object request);
}
