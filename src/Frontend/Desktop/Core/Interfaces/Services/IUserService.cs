using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.WPF.Client.Core.Models.Users;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 用户服务接口
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 搜索用户
        /// </summary>
        Task<PaginatedResult<UserInfo>> SearchUsersAsync(UserQueryRequest request);

        /// <summary>
        /// 新增用户
        /// </summary>
        Task<ApiResponse<object>> CreateUserAsync(UserCreateRequest request);

        /// <summary>
        /// 更新用户
        /// </summary>
        Task<ApiResponse<object>> UpdateUserAsync(UserUpdateRequest request);

        /// <summary>
        /// 禁用用户
        /// </summary>
        Task<ApiResponse<object>> DisableUserAsync(Guid userId);

        /// <summary>
        /// 启用用户
        /// </summary>
        Task<ApiResponse<object>> EnableUserAsync(Guid userId);

        /// <summary>
        /// 重置用户密码
        /// </summary>
        Task<ApiResponse<object>> ResetPasswordAsync(Guid userId);

        /// <summary>
        /// 获取所有角色
        /// </summary>
        Task<List<RoleInfo>> GetRolesAsync();
    }
}