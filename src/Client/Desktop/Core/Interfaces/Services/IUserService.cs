using LYBT.Shared.Models.Contracts.Common;
using System;
using LYBT.Desktop.Core.Models.Users;
using System.Collections.Generic;
using System.Threading.Tasks;

using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Core;

// UltraThink重构: 统一UserInfo和UserDto，使用UserDto作为统一模型
using UserInfo = LYBT.Shared.Models.Contracts.Users.UserDto;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// 用户服务接口
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 搜索用户
        /// </summary>
        Task<LYBT.Shared.Models.Contracts.Common.PagedResult<UserInfo>> SearchUsersAsync(UserPagedQueryDto request);

        /// <summary>
        /// 新增用户
        /// </summary>
        Task<ServiceResult> CreateUserAsync(UserCreateDto request);

        /// <summary>
        /// 更新用户
        /// </summary>
        Task<ServiceResult> UpdateUserAsync(UserUpdateDto request);

        /// <summary>
        /// 禁用用户
        /// </summary>
        Task<ServiceResult> DisableUserAsync(Guid userId);

        /// <summary>
        /// 启用用户
        /// </summary>
        Task<ServiceResult> EnableUserAsync(Guid userId);

        /// <summary>
        /// 重置用户密码
        /// </summary>
        Task<ServiceResult> ResetPasswordAsync(Guid userId);

        /// <summary>
        /// 获取所有角色
        /// </summary>
        Task<List<string>> GetRolesAsync();

        /// <summary>
        /// 获取所有用户
        /// </summary>
        Task<List<UserInfo>> GetUsersAsync();
    }
}