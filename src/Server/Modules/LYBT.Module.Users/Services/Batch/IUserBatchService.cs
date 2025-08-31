using System;
using LYBT.Shared.Models.Contracts.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;

namespace LYBT.Module.Users.Services.Batch
{
    /// <summary>
    /// 用户批量操作服务接口
    /// UltraThink重构：专注于用户的批量操作功能
    /// </summary>
    public interface IUserBatchService
    {
        /// <summary>
        /// 批量启用用户
        /// </summary>
        /// <param name="ids">用户ID列表</param>
        /// <returns>影响的记录数</returns>
        Task<ServiceResult<int>> BatchEnableUsersAsync(List<Guid> ids);

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        /// <param name="ids">用户ID列表</param>
        /// <returns>影响的记录数</returns>
        Task<ServiceResult<int>> BatchDisableUsersAsync(List<Guid> ids);

        /// <summary>
        /// 批量删除用户
        /// </summary>
        /// <param name="ids">用户ID列表</param>
        /// <returns>影响的记录数</returns>
        Task<ServiceResult<int>> BatchDeleteUsersAsync(List<Guid> ids);
    }
}