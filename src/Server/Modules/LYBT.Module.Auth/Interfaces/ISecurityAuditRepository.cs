using LYBT.Entities.Auth;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// 安全审计日志仓储接口
    /// 负责 SecurityAuditLog 的持久化操作，替代服务中直接使用 AppDbContext
    /// </summary>
    public interface ISecurityAuditRepository
    {
        /// <summary>
        /// 添加审计日志记录
        /// </summary>
        Task<SecurityAuditLog> AddAsync(SecurityAuditLog log, CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存更改
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
