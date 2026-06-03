using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Users.Interfaces
{
    /// <summary>
    /// 用户批量操作服务接口
    /// 从 IUserService 中分离批量操作职责，遵循单一职责原则
    /// </summary>
    public interface IUserBatchOperationService
    {
        /// <summary>
        /// 批量删除用户
        /// </summary>
        /// <param name="ids">要删除的用户ID列表</param>
        /// <param name="currentUserId">当前操作用户ID（不能删除自己）</param>
        /// <param name="currentRole">当前操作用户角色</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, Guid? currentUserId, UserRole currentRole, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量更新用户状态
        /// </summary>
        /// <param name="ids">用户ID列表</param>
        /// <param name="status">目标状态</param>
        /// <param name="currentUserId">当前操作用户ID（不能修改自己的状态）</param>
        /// <param name="currentRole">当前操作用户角色</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task<Result<BatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> ids, CommonStatus status, Guid? currentUserId, UserRole currentRole, CancellationToken cancellationToken = default);
    }
}