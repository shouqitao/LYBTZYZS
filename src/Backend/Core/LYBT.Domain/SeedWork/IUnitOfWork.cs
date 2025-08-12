using System;
using System.Threading.Tasks;

namespace LYBT.Domain.SeedWork
{
    /// <summary>
    /// 工作单元接口 - DDD聚合一致性保证
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// 保存所有更改
        /// </summary>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// 开始事务
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// 提交事务
        /// </summary>
        Task CommitTransactionAsync();

        /// <summary>
        /// 回滚事务
        /// </summary>
        Task RollbackTransactionAsync();

        /// <summary>
        /// 检查是否有活跃事务
        /// </summary>
        bool HasActiveTransaction { get; }
    }
}