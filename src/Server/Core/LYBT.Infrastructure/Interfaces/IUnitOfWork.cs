/// <summary>
/// P3-Fix 工作单元接口 - 解决测试项目编译错误
/// 最小化接口定义，仅用于编译通过
/// </summary>

namespace LYBT.Infrastructure.Interfaces
{
    /// <summary>
    /// 工作单元接口
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// 保存更改
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

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
    }
}
