// ---------------------------------------------------------------------------
// IApiClientAuth — Authentication API Sub-Interface
// ---------------------------------------------------------------------------
// Unified interface combining IAuthApi (remote) and ILocalAuthApi (local).
// No Refit attributes — implementations route to the correct backend.
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// Authentication API sub-interface — JWT authentication, session management, token operations.
/// </summary>
/// <remarks>
/// <para>Combines methods from IAuthApi (remote, ApiResponse-wrapped) and ILocalAuthApi (local, raw DTOs).</para>
/// <para>Remote methods return ApiResponse&lt;T&gt;; local-only methods return raw DTOs.</para>
/// </remarks>
public interface IApiClientAuth
{
    /// <summary>
    /// User login authentication.
    /// </summary>
    /// <param name="loginRequest">Login request containing username, password, and remember-me option.</param>
    /// <returns>Login response with JWT token, user info, and expiration.</returns>
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest loginRequest);

    /// <summary>
    /// Auto-login using stored AutoLoginToken.
    /// OpenSpec: refactor-login-authentication (CVT-001)
    /// </summary>
    /// <param name="request">Auto-login request containing username and AutoLoginToken.</param>
    /// <returns>Login response with JWT token, user info, and new AutoLoginToken.</returns>
    Task<ApiResponse<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request);

    /// <summary>
    /// User logout — invalidates current JWT token.
    /// </summary>
    /// <param name="logoutRequest">Logout request information.</param>
    Task<ApiResponse> LogoutAsync(LogoutRequest logoutRequest);

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <returns>New token pair (AccessToken + RefreshToken).</returns>
    Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request);

    /// <summary>
    /// Validate token from Authorization header (GET method).
    /// </summary>
    /// <returns>Validation result with token validity and user info.</returns>
    Task<ApiResponse<object>> ValidateTokenFromHeaderAsync();

    /// <summary>
    /// Validate a specific token (POST method).
    /// Issue #1824
    /// </summary>
    /// <param name="request">Token validation request.</param>
    /// <returns>Detailed validation result.</returns>
    Task<ApiResponse<ValidateTokenResponse>> ValidateTokenAsync(ValidateTokenRequest request);

    /// <summary>
    /// API service health check.
    /// </summary>
    /// <returns>Health check response.</returns>
    Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync();
}
