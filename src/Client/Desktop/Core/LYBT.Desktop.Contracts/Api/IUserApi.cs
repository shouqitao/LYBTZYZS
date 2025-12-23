using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// 用户API客户端接口 - RESTful设计
    /// List返回轻量UserListDto，Detail返回完整UserDetailDto
    /// </summary>
    public interface IUserApi
    {
        /// <summary>
        /// 获取用户列表（返回UserListDto）
        /// </summary>
        [Refit.Get("/api/v1/users")]
        Task<ApiResponse<PagedResult<UserListDto>>> GetUsersAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取用户详情
        /// </summary>
        [Refit.Get("/api/v1/users/{id}")]
        Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid id);

        /// <summary>
        /// 创建用户
        /// </summary>
        [Refit.Post("/api/v1/users")]
        Task<ApiResponse<UserDetailDto>> CreateUserAsync([Refit.Body] UserInputDto request);

        /// <summary>
        /// 更新用户
        /// </summary>
        [Refit.Put("/api/v1/users/{id}")]
        Task<ApiResponse<UserDetailDto>> UpdateUserAsync(Guid id, [Refit.Body] UserInputDto request);

        /// <summary>
        /// 删除用户
        /// </summary>
        [Refit.Delete("/api/v1/users/{id}")]
        Task<ApiResponse<ApiResponse>> DeleteUserAsync(Guid id);

        /// <summary>
        /// 修改个人资料 (Issue #1891)
        /// </summary>
        [Refit.Put("/api/v1/users/{id}/profile")]
        Task<ApiResponse<UserDetailDto>> ChangeProfileAsync(Guid id, [Refit.Body] ChangeProfileDto request);

        /// <summary>
        /// 修改密码 (Issue #1887-1892)
        /// </summary>
        [Refit.Put("/api/v1/users/{id}/change-password")]
        Task<ApiResponse<ApiResponse>> ChangePasswordAsync(Guid id, [Refit.Body] LYBT.Shared.Models.Contracts.Auth.ChangePasswordRequest request);

        /// <summary>
        /// 管理员重置用户密码 (Issue #1910)
        /// </summary>
        [Refit.Post("/api/v1/users/{id}/reset-password")]
        Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, [Refit.Body] ResetPasswordRequestDto request);

        /// <summary>
        /// 批量导入用户 (Issue #2003 Task 2.10)
        /// </summary>
        [Refit.Post("/api/v1/users/batch-import")]
        Task<ApiResponse<UserBatchImportResultDto>> BatchImportAsync([Refit.Body] UserBatchImportInputDto request);

        #region 状态切换、恢复和批量操作

        /// <summary>
        /// 切换用户状态（启用/禁用）
        /// </summary>
        [Refit.Post("/api/v1/users/{id}/toggle-status")]
        Task<ApiResponse<UserDetailDto>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 恢复已删除的用户
        /// </summary>
        [Refit.Post("/api/v1/users/{id}/restore")]
        Task<ApiResponse<UserDetailDto>> RestoreAsync(Guid id);

        /// <summary>
        /// 批量删除用户
        /// </summary>
        [Refit.Post("/api/v1/users/batch-delete")]
        Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);

        /// <summary>
        /// 批量启用用户
        /// </summary>
        [Refit.Post("/api/v1/users/batch-enable")]
        Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync([Refit.Body] BatchDeleteInputDto request);

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        [Refit.Post("/api/v1/users/batch-disable")]
        Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync([Refit.Body] BatchDeleteInputDto request);

        #endregion
    }
}
