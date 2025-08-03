using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts.Users;

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
        Task<LYBT.Shared.Models.Common.ApiResponse<LYBT.Shared.Models.Common.PaginatedResult<UserDto>>> GetPagedUsersAsync([Body] UserPagedQueryDto query);

        /// <summary>
        /// 获取用户详情
        /// </summary>
        [Get("/api/v1/users/{id}")]
        Task<LYBT.Shared.Models.Common.ApiResponse<UserDto>> GetUserByIdAsync(Guid id);

        /// <summary>
        /// 创建用户
        /// </summary>
        [Post("/api/v1/users/add")]
        Task<LYBT.Shared.Models.Common.ApiResponse<object>> CreateUserAsync([Body] UserCreateDto dto);

        /// <summary>
        /// 更新用户
        /// </summary>
        [Put("/api/v1/users/update")]
        Task<LYBT.Shared.Models.Common.ApiResponse<object>> UpdateUserAsync([Body] UserUpdateDto dto);

        /// <summary>
        /// 禁用用户
        /// </summary>
        [Patch("/api/v1/users/{id}/disable")]
        Task<LYBT.Shared.Models.Common.ApiResponse<object>> DisableUserAsync(Guid id);

        /// <summary>
        /// 启用用户
        /// </summary>
        [Patch("/api/v1/users/{id}/enable")]
        Task<LYBT.Shared.Models.Common.ApiResponse<object>> EnableUserAsync(Guid id);

        /// <summary>
        /// 重置用户密码
        /// </summary>
        [Post("/api/v1/users/{id}/reset-password")]
        Task<LYBT.Shared.Models.Common.ApiResponse<bool>> ResetPasswordAsync(Guid id);
    }
}