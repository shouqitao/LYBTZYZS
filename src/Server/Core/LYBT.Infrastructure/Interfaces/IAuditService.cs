using LYBT.Entities.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Infrastructure.Interfaces
{
    /// <summary>
    /// 通用审计服务接口
    /// OpenSpec: add-global-audit-system
    /// 提供对任意业务实体的变更审计能力
    /// </summary>
    /// <typeparam name="TEntity">业务实体类型，必须继承自BaseEntity</typeparam>
    public interface IAuditService<TEntity> where TEntity : BaseEntity
    {
        /// <summary>
        /// 记录创建操作的审计日志
        /// </summary>
        /// <param name="entity">创建的实体</param>
        /// <param name="operatorId">操作者ID</param>
        /// <param name="operatorName">操作者姓名</param>
        /// <param name="role">操作者角色</param>
        Task LogCreateAsync(
            TEntity entity,
            Guid operatorId,
            string operatorName,
            UserRole role);

        /// <summary>
        /// 记录更新操作的审计日志
        /// </summary>
        /// <param name="before">更新前的实体状态（可为null，表示无法获取原始状态）</param>
        /// <param name="after">更新后的实体状态</param>
        /// <param name="operatorId">操作者ID</param>
        /// <param name="operatorName">操作者姓名</param>
        /// <param name="role">操作者角色</param>
        /// <param name="reason">修改原因（可选）</param>
        Task LogUpdateAsync(
            TEntity? before,
            TEntity after,
            Guid operatorId,
            string operatorName,
            UserRole role,
            string? reason = null);

        /// <summary>
        /// 记录删除操作的审计日志
        /// </summary>
        /// <param name="entity">被删除的实体</param>
        /// <param name="operatorId">操作者ID</param>
        /// <param name="operatorName">操作者姓名</param>
        /// <param name="role">操作者角色</param>
        /// <param name="reason">删除原因（可选）</param>
        Task LogDeleteAsync(
            TEntity entity,
            Guid operatorId,
            string operatorName,
            UserRole role,
            string? reason = null);

        /// <summary>
        /// 获取实体的审计日志列表（分页）
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="page">页码（从1开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>审计日志列表和总记录数</returns>
        Task<(List<EntityAuditLog> Logs, int TotalCount)> GetLogsAsync(
            Guid entityId,
            int page = 1,
            int pageSize = 20);
    }
}
