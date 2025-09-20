using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Shared.Interfaces.Api
{
    /// <summary>
    /// 用户API客户端接口 - UltraThink统一标准
    /// 移动到shared层以确保前后端契约一致性
    /// </summary>
    public interface IUserApi
    {
        /// <summary>
        /// 获取用户列表（支持分页和查询）
        /// </summary>
        [Refit.Get("/api/v1/users")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<PagedResult<UserDto>>>> GetUsersAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null,
            [Refit.Query] string? username = null,
            [Refit.Query] string? realName = null,
            [Refit.Query] string? email = null,
            [Refit.Query] string? phoneNumber = null,
            [Refit.Query] string? role = null,
            [Refit.Query] bool? isActive = null);

        /// <summary>
        /// 获取用户详情
        /// </summary>
        [Refit.Get("/api/v1/users/{id}")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<UserDto>>> GetUserByIdAsync(Guid id);

        /// <summary>
        /// 创建用户
        /// </summary>
        [Refit.Post("/api/v1/users")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<UserDto>>> CreateUserAsync([Refit.Body] UserCreateDto dto);

        /// <summary>
        /// 更新用户
        /// </summary>
        [Refit.Put("/api/v1/users/{id}")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<UserDto>>> UpdateUserAsync(Guid id, [Refit.Body] UserUpdateDto dto);

        /// <summary>
        /// 切换用户状态
        /// </summary>
        [Refit.Patch("/api/v1/users/{id}/toggle-status")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        [Refit.Patch("/api/v1/users/batch-disable")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> BatchDisableAsync([Refit.Body] BatchIdsDto dto);

        /// <summary>
        /// 批量启用用户
        /// </summary>
        [Refit.Patch("/api/v1/users/batch-enable")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> BatchEnableAsync([Refit.Body] BatchIdsDto dto);

        /// <summary>
        /// 重置用户密码
        /// </summary>
        [Refit.Post("/api/v1/users/reset-password/{id}")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> ResetPasswordAsync(Guid id);

        /// <summary>
        /// 修改密码
        /// </summary>
        [Refit.Patch("/api/v1/users/password")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> ChangePasswordAsync([Refit.Body] ChangePasswordDto dto);

        /// <summary>
        /// 修改个人信息
        /// </summary>
        [Refit.Put("/api/v1/users/profile")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> ChangeProfileAsync([Refit.Body] ChangeProfileDto dto);

        /// <summary>
        /// 获取所有角色
        /// </summary>
        [Refit.Get("/api/v1/users/roles")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<IEnumerable<object>>>> GetRolesAsync();

        /// <summary>
        /// 获取活跃用户列表
        /// </summary>
        [Refit.Get("/api/v1/users/active")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<IEnumerable<UserDto>>>> GetActiveUsersAsync();
    }
}
