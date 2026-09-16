// ---------------------------------------------------------------------------
// IAuthApiClient — 认证 API 窄接口（T5.2 ISP：从 IApiClientIdentity 拆出）
// ---------------------------------------------------------------------------
// LoginViewModel/AuthenticationService 等仅依赖 4 个认证端点，无需实现
// BatchDeleteAsync 等 9 个用户管理方法（Mock 成本降 60%）。
// IApiClientIdentity 保留为组合 Facade 兼容旧调用方。
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// 认证 API 子接口（T5.2 从 IApiClientIdentity 拆出，仅认证端点）。
/// </summary>
public interface IAuthApiClient
{
    /// <summary>用户登录认证。</summary>
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest loginRequest, CancellationToken ct = default);

    /// <summary>使用存储的 AutoLoginToken 自动登录。</summary>
    Task<ApiResponse<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request, CancellationToken ct = default);

    /// <summary>用户登出——使当前 JWT 令牌失效。</summary>
    Task<ApiResponse> LogoutAsync(LogoutRequest logoutRequest, CancellationToken ct = default);

    /// <summary>使用刷新令牌刷新访问令牌。</summary>
    Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
}
