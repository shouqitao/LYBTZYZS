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
public interface IApiClientIdentity : IUserManagementApiClient, IAuthApiClient
{
    // ========== 认证扩展端点（IAuthApiClient 含 Login/LoginWithAutoToken/Logout/RefreshToken） ==========

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

    /// <summary>
    /// 分页查询安全审计日志（US-SHELL-014）。
    /// </summary>
    Task<ApiResponse<PagedResult<SecurityAuditLogDto>>> GetSecurityAuditLogsAsync(
        int page = 1,
        int pageSize = 20,
        string? eventType = null,
        string? userName = null,
        DateTime? from = null,
        DateTime? to = null);

    // ========== 用户管理扩展端点（IUserManagementApiClient 含 CRUD/Profile/ToggleStatus） ==========

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
