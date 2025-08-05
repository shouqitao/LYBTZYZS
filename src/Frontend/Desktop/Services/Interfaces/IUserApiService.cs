using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Common;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 用户API服务接口
    /// </summary>
    public interface IUserApiService
    {
        /// <summary>
        /// 分页查询用户
        /// </summary>
        [Post("/api/v1/users/paged")]
        Task<Refit.ApiResponse<PaginatedResult<UserDto>>> GetPagedUsersAsync([Body] UserPagedQueryDto query);

        /// <summary>
        /// 获取用户详情
        /// </summary>
        [Get("/api/v1/users/{id}")]
        Task<Refit.ApiResponse<UserDto>> GetUserByIdAsync(Guid id);

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        [Get("/api/v1/users/getById/{id}")]
        Task<Refit.ApiResponse<UserDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建用户
        /// </summary>
        [Post("/api/v1/users/add")]
        Task<Refit.ApiResponse<object>> CreateUserAsync([Body] UserCreateDto dto);

        /// <summary>
        /// 更新用户
        /// </summary>
        [Put("/api/v1/users/update")]
        Task<Refit.ApiResponse<object>> UpdateUserAsync([Body] UserUpdateDto dto);

        /// <summary>
        /// 禁用用户
        /// </summary>
        [Patch("/api/v1/users/{id}/disable")]
        Task<Refit.ApiResponse<object>> DisableUserAsync(Guid id);

        /// <summary>
        /// 启用用户
        /// </summary>
        [Patch("/api/v1/users/{id}/enable")]
        Task<Refit.ApiResponse<object>> EnableUserAsync(Guid id);

        /// <summary>
        /// 切换用户状态
        /// </summary>
        [Patch("/api/v1/users/{id}/toggle-status")]
        Task<Refit.ApiResponse<object>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        [Patch("/api/v1/users/batch-disable")]
        Task<Refit.ApiResponse<object>> BatchDisableAsync([Body] BatchIdsDto dto);

        /// <summary>
        /// 批量启用用户
        /// </summary>
        [Patch("/api/v1/users/batch-enable")]
        Task<Refit.ApiResponse<object>> BatchEnableAsync([Body] BatchIdsDto dto);

        /// <summary>
        /// 重置用户密码
        /// </summary>
        [Post("/api/v1/users/resetPassword/{id}")]
        Task<Refit.ApiResponse<object>> ResetPasswordAsync(Guid id);

        /// <summary>
        /// 修改密码
        /// </summary>
        [Patch("/api/v1/users/password")]
        Task<Refit.ApiResponse<object>> ChangePasswordAsync([Body] ChangePasswordDto dto);

        /// <summary>
        /// 修改个人信息
        /// </summary>
        [Put("/api/v1/users/profile")]
        Task<Refit.ApiResponse<object>> ChangeProfileAsync([Body] ChangeProfileDto dto);

        /// <summary>
        /// 获取所有角色
        /// </summary>
        [Get("/api/v1/users/getRoles")]
        Task<Refit.ApiResponse<IEnumerable<object>>> GetRolesAsync();

        /// <summary>
        /// 获取启用的用户列表
        /// </summary>
        [Get("/api/v1/users/active")]
        Task<Refit.ApiResponse<IEnumerable<UserDto>>> GetActiveUsersAsync();
    }
}