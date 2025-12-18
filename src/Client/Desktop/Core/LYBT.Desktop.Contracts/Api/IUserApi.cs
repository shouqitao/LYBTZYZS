using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// 用户API客户端接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IUserApi
    {
        /// <summary>
        /// 获取用户列表（支持分页和查询）
        /// </summary>
        [Refit.Get("/api/v1/users")]
        Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取用户列表（返回UserListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        [Refit.Get("/api/v1/users/list")]
        Task<ApiResponse<PagedResult<UserListDto>>> GetUsersListAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取用户详情
        /// </summary>
        [Refit.Get("/api/v1/users/{id}")]
        Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id);

        /// <summary>
        /// 创建用户
        /// </summary>
        [Refit.Post("/api/v1/users")]
        Task<ApiResponse<UserDto>> CreateUserAsync([Refit.Body] UserInputDto request);

        /// <summary>
        /// 更新用户
        /// </summary>
        [Refit.Put("/api/v1/users/{id}")]
        Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, [Refit.Body] UserInputDto request);

        /// <summary>
        /// 删除用户
        /// </summary>
        [Refit.Delete("/api/v1/users/{id}")]
        Task<ApiResponse<ApiResponse>> DeleteUserAsync(Guid id);

        /// <summary>
        /// 修改个人资料 (Issue #1891)
        /// </summary>
        [Refit.Put("/api/v1/users/{id}/profile")]
        Task<ApiResponse<UserDto>> ChangeProfileAsync(Guid id, [Refit.Body] ChangeProfileDto request);


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
        /// Desktop主导模式：Desktop解析Excel并组装DTO，API接收并批量创建
        /// Note: Server端需要实现对应的 POST /api/v1/users/batch-import endpoint
        /// </summary>
        [Refit.Post("/api/v1/users/batch-import")]
        Task<ApiResponse<UserBatchImportResultDto>> BatchImportAsync([Refit.Body] UserBatchImportRequestDto request);

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复 ==========

        /// <summary>
        /// 切换用户状态（启用/禁用）
        /// </summary>
        [Refit.Post("/api/v1/users/{id}/toggle-status")]
        Task<ApiResponse<UserDto>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 恢复已删除的用户
        /// </summary>
        [Refit.Post("/api/v1/users/{id}/restore")]
        Task<ApiResponse<UserDto>> RestoreAsync(Guid id);
    }
}
