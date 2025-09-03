using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Users.Services.Interfaces
{
    /// <summary>
    /// 用户业务服务接口 - UltraThink三层架构
    /// 职责：业务流程编排，完整事务管理和业务逻辑处理
    /// </summary>
    public interface IUserBusinessService
    {
        /// <summary>
        /// 禁用用户
        /// </summary>
        Task<ServiceResult<bool>> DisableAsync(Guid id);

        /// <summary>
        /// 启用用户
        /// </summary>
        Task<ServiceResult<bool>> EnableAsync(Guid id);

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids);

        /// <summary>
        /// 批量启用用户
        /// </summary>
        Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids);

        /// <summary>
        /// 重置密码
        /// </summary>
        Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword);

        /// <summary>
        /// 更改密码
        /// </summary>
        Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);

        /// <summary>
        /// 修改个人信息
        /// </summary>
        Task<ServiceResult<bool>> ChangeProfileAsync(Guid userId, string realName, string phoneNumber);

        /// <summary>
        /// 创建用户 - 完整业务流程
        /// </summary>
        Task<ServiceResult<UserDto>> CreateUserAsync(UserMutationDto dto);

        /// <summary>
        /// 更新用户 - 完整业务流程
        /// </summary>
        Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserMutationDto dto);

        /// <summary>
        /// 删除用户 - 完整业务流程
        /// </summary>
        Task<ServiceResult<bool>> DeleteUserAsync(Guid id);
    }
}