using System;
using System.Threading;
using System.Threading.Tasks;

namespace LYBT.Infrastructure.Transactions
{
    /// <summary>
    /// 事务步骤接口
    /// </summary>
    /// <typeparam name="TContext">事务上下文类型</typeparam>
    public interface ITransactionStep<TContext>
    {
        /// <summary>
        /// 步骤名称
        /// </summary>
        string StepName { get; }
        
        /// <summary>
        /// 执行顺序
        /// </summary>
        int Order { get; }
        
        /// <summary>
        /// 是否支持补偿
        /// </summary>
        bool SupportsCompensation { get; }
        
        /// <summary>
        /// 超时时间
        /// </summary>
        TimeSpan Timeout { get; }
        
        /// <summary>
        /// 检查是否可以执行
        /// </summary>
        Task<bool> CanExecuteAsync(TContext context, CancellationToken cancellationToken);
        
        /// <summary>
        /// 执行核心逻辑
        /// </summary>
        Task<TransactionResult<TContext>> ExecuteCoreAsync(TContext context, CancellationToken cancellationToken);
    }
}