// ---------------------------------------------------------------------------
// IApiClientIdentity — Unified Auth + Users API Sub-Interface
// ---------------------------------------------------------------------------
// 合并 IApiClientAuth（认证）与 IApiClientUsers（用户管理）为统一契约，
// 与 Server 端 IdentityController（A-31-C3a，双路由 /api/v1/auth/* + /api/v1/users/*）对齐。
// 无 Refit 属性 — 实现路由到正确的后端（Refit 远程 / HttpClient 本地）。
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// 认证与用户管理 API 子接口（A-31-C3d 合并 IApiClientAuth + IApiClientUsers）。
/// </summary>
/// <remarks>
/// <para>Combines methods from IAuthApi + IUserApi (remote, ApiResponse-wrapped) and
/// ILocalAuthApi + ILocalUserApi (local, raw DTOs).</para>
/// <para>Remote methods return ApiResponse&lt;T&gt;; local-only methods return raw DTOs.</para>
/// </remarks>
public interface IApiClientIdentity : IEntityApiSegment<UserListDto, UserDetailDto, UserInputDto>
{
    // ========== 认证端点（原 IApiClientAuth，路由 /api/v1/auth/*） ==========

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

    // ========== 用户管理端点（原 IApiClientUsers，路由 /api/v1/users/*） ==========

    /// <summary>
    /// 分页获取用户列表。
    /// </summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    /// <param name="keyword">Search keyword (optional).</param>
    Task<ApiResponse<PagedResult<UserListDto>>> GetUsersAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null);

    /// <summary>
    /// 按 ID 获取用户详情。
    /// </summary>
    /// <param name="id">User ID.</param>
    Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid id);

    /// <summary>
    /// 创建新用户。
    /// </summary>
    /// <param name="request">User input data.</param>
    Task<ApiResponse<UserDetailDto>> CreateUserAsync(UserInputDto request);

    /// <summary>
    /// 更新现有用户。
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">User input data.</param>
    Task<ApiResponse<UserDetailDto>> UpdateUserAsync(Guid id, UserInputDto request);

    /// <summary>
    /// 删除用户（软删除）。
    /// </summary>
    /// <param name="id">User ID.</param>
    Task<ApiResponse> DeleteUserAsync(Guid id);

    /// <summary>
    /// 修改用户个人资料。
    /// Issue #1891
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">Profile change data.</param>
    Task<ApiResponse<UserDetailDto>> ChangeProfileAsync(Guid id, ChangeProfileDto request);

    /// <summary>
    /// 修改用户密码。
    /// Issue #1887-1892
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">Password change request.</param>
    Task<ApiResponse> ChangePasswordAsync(Guid id, ChangePasswordRequest request);

    /// <summary>
    /// 管理员重置用户密码。
    /// Issue #1910
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">Reset password request.</param>
    Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequest request);

    /// <summary>
    /// 切换用户状态（启用/禁用）。
    /// </summary>
    /// <param name="id">User ID.</param>
    Task<ApiResponse<UserDetailDto>> ToggleStatusAsync(Guid id);

    /// <summary>
    /// 批量删除用户。
    /// </summary>
    /// <param name="request">Batch delete input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request);

    // ========== Local-only methods ==========

    /// <summary>
    /// 恢复软删除的用户。
    /// </summary>
    /// <param name="id">User ID.</param>
    Task<ApiResponse<UserDetailDto>> RestoreAsync(Guid id);

    /// <summary>
    /// 批量启用用户。
    /// </summary>
    /// <param name="request">Batch input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request);

    /// <summary>
    /// 批量禁用用户。
    /// </summary>
    /// <param name="request">Batch input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request);

    // ========== Local-only methods ==========

    /// <summary>
    /// 获取当前已认证用户（仅本地模式）。
    /// </summary>
    Task<UserDetailDto> GetCurrentUserAsync();

    // ========== 泛型段接口默认实现（转发到上方实体命名方法，实现类无需改动） ==========

    Task<ApiResponse<PagedResult<UserListDto>>> IEntityApiSegment<UserListDto, UserDetailDto, UserInputDto>.GetPagedAsync(
        int page, int pageSize, string? keyword, string? category)
        => GetUsersAsync(page, pageSize, keyword);

    Task<ApiResponse<UserDetailDto>> IEntityApiSegment<UserListDto, UserDetailDto, UserInputDto>.GetByIdAsync(Guid id)
        => GetUserByIdAsync(id);

    Task<ApiResponse<UserDetailDto>> IEntityApiSegment<UserListDto, UserDetailDto, UserInputDto>.CreateAsync(UserInputDto request)
        => CreateUserAsync(request);

    Task<ApiResponse<UserDetailDto>> IEntityApiSegment<UserListDto, UserDetailDto, UserInputDto>.UpdateAsync(Guid id, UserInputDto request)
        => UpdateUserAsync(id, request);

    Task<ApiResponse> IEntityApiSegment<UserListDto, UserDetailDto, UserInputDto>.DeleteAsync(Guid id)
        => DeleteUserAsync(id);
}
