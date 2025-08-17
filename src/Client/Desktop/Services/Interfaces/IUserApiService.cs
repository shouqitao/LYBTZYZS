using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Common;

namespace LYBT.Desktop.Services.Interfaces
{
    /// <summary>
    /// 用户API服务接口 - 统一标准
    /// </summary>
    public interface IUserApiService
    {
        /// <summary>
        /// 获取用户列表（支持分页和查询）
        /// </summary>
        [Get("/api/v1/users")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<PagedData<UserDto>>>> GetUsersAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null,
            [Query] string? username = null,
            [Query] string? realName = null,
            [Query] string? email = null,
            [Query] string? phoneNumber = null,
            [Query] string? role = null,
            [Query] bool? isActive = null);

        /// <summary>
        /// 获取用户详情
        /// </summary>
        [Get("/api/v1/users/{id}")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<UserDto>>> GetUserByIdAsync(Guid id);

        // 移除重复的GetById接口，统一使用GetUserByIdAsync

        /// <summary>
        /// 创建用户
        /// </summary>
        [Post("/api/v1/users")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<UserDto>>> CreateUserAsync([Body] UserCreateDto dto);

        /// <summary>
        /// 更新用户
        /// </summary>
        [Put("/api/v1/users/{id}")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<UserDto>>> UpdateUserAsync(Guid id, [Body] UserUpdateDto dto);

        // 移除单独的Enable/Disable接口，统一使用ToggleStatus

        /// <summary>
        /// 切换用户状态
        /// </summary>
        [Patch("/api/v1/users/{id}/toggle-status")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        [Patch("/api/v1/users/batch-disable")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> BatchDisableAsync([Body] BatchIdsDto dto);

        /// <summary>
        /// 批量启用用户
        /// </summary>
        [Patch("/api/v1/users/batch-enable")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> BatchEnableAsync([Body] BatchIdsDto dto);

        /// <summary>
        /// 重置用户密码
        /// </summary>
        [Post("/api/v1/users/reset-password/{id}")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> ResetPasswordAsync(Guid id);

        /// <summary>
        /// 修改密码
        /// </summary>
        [Patch("/api/v1/users/password")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> ChangePasswordAsync([Body] ChangePasswordDto dto);

        /// <summary>
        /// 修改个人信息
        /// </summary>
        [Put("/api/v1/users/profile")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>>> ChangeProfileAsync([Body] ChangeProfileDto dto);

        /// <summary>
        /// 获取所有角色
        /// </summary>
        [Get("/api/v1/users/roles")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<IEnumerable<object>>>> GetRolesAsync();

        /// <summary>
        /// 获取活跃用户列表
        /// </summary>
        [Get("/api/v1/users/active")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<IEnumerable<UserDto>>>> GetActiveUsersAsync();
    }
}