// ---------------------------------------------------------------------------
// IApiClientUsers — User Management API Sub-Interface
// ---------------------------------------------------------------------------
// Unified interface combining IUserApi (remote) and ILocalUserApi (local).
// No Refit attributes — implementations route to the correct backend.
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// User management API sub-interface — CRUD, password management, batch operations.
/// </summary>
/// <remarks>
/// <para>Combines methods from IUserApi (remote) and ILocalUserApi (local).</para>
/// <para>Remote methods return ApiResponse&lt;T&gt;; local-only methods return raw DTOs.</para>
/// </remarks>
public interface IApiClientUsers
{
    /// <summary>
    /// Get user list with pagination.
    /// </summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    /// <param name="keyword">Search keyword (optional).</param>
    Task<ApiResponse<PagedResult<UserListDto>>> GetUsersAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null);

    /// <summary>
    /// Get user detail by ID.
    /// </summary>
    /// <param name="id">User ID.</param>
    Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid id);

    /// <summary>
    /// Create a new user.
    /// </summary>
    /// <param name="request">User input data.</param>
    Task<ApiResponse<UserDetailDto>> CreateUserAsync(UserInputDto request);

    /// <summary>
    /// Update an existing user.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">User input data.</param>
    Task<ApiResponse<UserDetailDto>> UpdateUserAsync(Guid id, UserInputDto request);

    /// <summary>
    /// Delete a user (soft delete).
    /// </summary>
    /// <param name="id">User ID.</param>
    Task<ApiResponse> DeleteUserAsync(Guid id);

    /// <summary>
    /// Change user profile.
    /// Issue #1891
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">Profile change data.</param>
    Task<ApiResponse<UserDetailDto>> ChangeProfileAsync(Guid id, ChangeProfileDto request);

    /// <summary>
    /// Change user password.
    /// Issue #1887-1892
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">Password change request.</param>
    Task<ApiResponse> ChangePasswordAsync(Guid id, ChangePasswordRequest request);

    /// <summary>
    /// Admin reset user password.
    /// Issue #1910
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">Reset password request.</param>
    Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequestDto request);

    /// <summary>
    /// Batch import users.
    /// Issue #2003 Task 2.10
    /// </summary>
    /// <param name="request">Batch import input data.</param>
    Task<ApiResponse<UserBatchImportResultDto>> BatchImportAsync(UserBatchImportInputDto request);

    /// <summary>
    /// Toggle user status (enable/disable).
    /// </summary>
    /// <param name="id">User ID.</param>
    Task<ApiResponse<UserDetailDto>> ToggleStatusAsync(Guid id);

    /// <summary>
    /// Restore a soft-deleted user.
    /// </summary>
    /// <param name="id">User ID.</param>
    Task<ApiResponse<UserDetailDto>> RestoreAsync(Guid id);

    /// <summary>
    /// Batch delete users.
    /// </summary>
    /// <param name="request">Batch delete input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request);

    /// <summary>
    /// Batch enable users.
    /// </summary>
    /// <param name="request">Batch operation input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request);

    /// <summary>
    /// Batch disable users.
    /// </summary>
    /// <param name="request">Batch operation input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request);

    // ========== Local-only methods ==========

    /// <summary>
    /// Get current authenticated user (local mode only).
    /// </summary>
    Task<UserDetailDto> GetCurrentUserAsync();
}
