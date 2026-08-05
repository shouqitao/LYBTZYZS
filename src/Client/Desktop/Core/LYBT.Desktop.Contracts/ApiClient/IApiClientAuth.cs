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
/// 认证 API 子接口——JWT 认证、会话管理、令牌操作。
/// </summary>
/// <remarks>
/// <para>Combines methods from IAuthApi (remote, ApiResponse-wrapped) and ILocalAuthApi (local, raw DTOs).</para>
/// <para>Remote methods return ApiResponse&lt;T&gt;; local-only methods return raw DTOs.</para>
/// </remarks>
public interface IApiClientAuth
{
    /// <summary>
    /// 用户登录认证。
    /// </summary>
    /// <param name="loginRequest">Login request containing username, password, and remember-me option.</param>
    /// <returns>Login response with JWT token, user info, and expiration.</returns>
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest loginRequest);

    /// <summary>
    /// 使用存储的 AutoLoginToken 自动登录。
    /// </summary>
    /// <param name="request">Auto-login request containing username and AutoLoginToken.</param>
    /// <returns>Login response with JWT token, user info, and new AutoLoginToken.</returns>
    Task<ApiResponse<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request);

    /// <summary>
    /// 用户登出——使当前 JWT 令牌失效。
    /// </summary>
    /// <param name="logoutRequest">Logout request information.</param>
    Task<ApiResponse> LogoutAsync(LogoutRequest logoutRequest);

    /// <summary>
    /// 使用刷新令牌刷新访问令牌。
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <returns>New token pair (AccessToken + RefreshToken).</returns>
    Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request);

    /// <summary>
    /// 从 Authorization 头校验令牌（GET 方法）。
    /// Issue #1824
    /// </summary>
    /// <returns>Detailed validation result.</returns>
    Task<ApiResponse<ValidateTokenResponse>> ValidateTokenAsync();

    /// <summary>
    /// API 服务健康检查。
    /// </summary>
    /// <returns>Health check response.</returns>
    Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync();
}
