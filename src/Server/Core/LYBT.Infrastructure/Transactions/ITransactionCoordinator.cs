using System;
using System.Threading;
using System.Threading.Tasks;

namespace LYBT.Infrastructure.Transactions
{
    /// <summary>
    /// 事务协调器接口 (Record-Only模式：复杂事务协调功能已移除)
    /// </summary>
    [Obsolete("Complex transaction coordination removed in Record-Only mode. Use simple EF Core transactions instead.")]
    public interface ITransactionCoordinator
    {
        /// <summary>
        /// 执行事务
        /// </summary>
        /// <typeparam name="TContext">事务上下文类型</typeparam>
        /// <param name="context">事务上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行结果</returns>
        Task<bool> ExecuteTransactionAsync<TContext>(TContext context, CancellationToken cancellationToken = default)
            where TContext : TransactionContext;

        /// <summary>
        /// 获取事务状态
        /// </summary>
        /// <param name="transactionId">事务ID</param>
        /// <returns>事务状态</returns>
        Task<TransactionStatus?> GetTransactionStatusAsync(Guid transactionId);

        /// <summary>
        /// 取消事务
        /// </summary>
        /// <param name="transactionId">事务ID</param>
        /// <returns>取消结果</returns>
        Task<bool> CancelTransactionAsync(Guid transactionId);
    }
}
