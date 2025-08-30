using System;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Users.Services.Core
{
    /// <summary>
    /// 用户服务核心层接口 - UltraThink三层架构
    /// 职责：基础CRUD操作，数据持久化专责
    /// </summary>
    public interface IUserServiceCore
    {
        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建用户
        /// </summary>
        Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto dto);

        /// <summary>
        /// 更新用户信息
        /// </summary>
        Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserMutationDto dto);

        /// <summary>
        /// 删除用户
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);

        /// <summary>
        /// 更新用户状态
        /// </summary>
        Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, CommonStatus status);
    }
}