using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Users;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 用户API服务接口
    /// </summary>
    public interface IUserApiService
    {
        /// <summary>
        /// 获取所有用户
        /// </summary>
        [Get("/api/v1/users")]
        Task<ApiResponse<List<UserDto>>> GetUsersAsync([Query] string? search = null);

        /// <summary>
        /// 获取用户详情
        /// </summary>
        [Get("/api/v1/users/{id}")]
        Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id);

        /// <summary>
        /// 创建用户
        /// </summary>
        [Post("/api/v1/users")]
        Task<ApiResponse<UserDto>> CreateUserAsync([Body] CreateUserDto dto);

        /// <summary>
        /// 更新用户
        /// </summary>
        [Put("/api/v1/users/{id}")]
        Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, [Body] UpdateUserDto dto);

        /// <summary>
        /// 删除用户
        /// </summary>
        [Delete("/api/v1/users/{id}")]
        Task<ApiResponse<bool>> DeleteUserAsync(Guid id);

        /// <summary>
        /// 切换用户状态
        /// </summary>
        [Patch("/api/v1/users/{id}/toggle-status")]
        Task<ApiResponse<bool>> ToggleUserStatusAsync(Guid id);

        /// <summary>
        /// 重置用户密码
        /// </summary>
        [Post("/api/v1/users/{id}/reset-password")]
        Task<ApiResponse<bool>> ResetPasswordAsync(Guid id);
    }
}