using System;
using LYBT.Shared.Models.Contracts.Common;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Services.Core
{
    /// <summary>
    /// 用户基础CRUD操作服务接口
    /// UltraThink重构：单一职责原则，只负责用户的基础增删改查操作
    /// </summary>
    public interface IUserCrudService
    {
        /// <summary>
        /// 创建用户
        /// </summary>
        /// <param name="dto">创建用户DTO</param>
        /// <returns>创建的用户DTO</returns>
        Task<ServiceResult<UserDto>> CreateUserAsync(UserMutationDto dto);

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="dto">更新用户DTO</param>
        /// <returns>更新的用户DTO</returns>
        Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserMutationDto dto);

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>删除结果</returns>
        Task<ServiceResult<bool>> DeleteUserAsync(Guid id);
    }
}