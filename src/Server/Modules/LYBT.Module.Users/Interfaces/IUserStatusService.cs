using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Users.Interfaces
{
    /// <summary>
    /// 用户状态管理服务接口
    /// 职责: 状态切换（启用/禁用）、软删除恢复
    /// </summary>
    public interface IUserStatusService
    {
        /// <summary>
        /// 切换用户状态（启用/禁用）
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="currentRole">当前操作用户角色</param>
        Task<Result<UserDetailDto>> ToggleStatusAsync(Guid id, UserRole currentRole, CancellationToken cancellationToken = default);

        /// <summary>
        /// 恢复软删除的用户
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="currentRole">当前操作用户角色</param>
        Task<Result<UserDetailDto>> RestoreAsync(Guid id, UserRole currentRole, CancellationToken cancellationToken = default);
    }
}
