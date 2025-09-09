using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Transactions
{
    /// <summary>
    /// 事务步骤通用基类
    /// 提供基础事务步骤实现，适用于简单的非数据库操作步骤
    /// </summary>
    /// <typeparam name="TContext">事务上下文类型</typeparam>
    public abstract class TransactionStepBase<TContext> : ITransactionStep<TContext>
        where TContext : TransactionContext
    {
        /// <summary>
        /// 日志记录器
        /// </summary>
        protected readonly ILogger? Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionStepBase{TContext}"/> class.
        /// 初始化事务步骤基类
        /// </summary>
        /// <param name="logger">日志记录器</param>
        protected TransactionStepBase(ILogger? logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionStepBase{TContext}"/> class.
        /// 无参数构造函数，用于子类不需要日志记录器的场景
        /// </summary>
        protected TransactionStepBase()
        {
            Logger = null;
        }

        /// <inheritdoc />
        public abstract string StepName { get; }

        /// <inheritdoc />
        public abstract int Order { get; }

        /// <inheritdoc />
        public abstract bool SupportsCompensation { get; }

        /// <inheritdoc />
        public virtual TimeSpan Timeout => TimeSpan.FromSeconds(30);

        /// <inheritdoc />
        public virtual async Task<bool> CanExecuteAsync(TContext context, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                Logger?.LogError("Transaction context is null");
                return false;
            }

            // 默认实现：总是可以执行，子类可以重写提供特定逻辑
            return await Task.FromResult(true);
        }

        /// <inheritdoc />
        public async Task<TransactionResult<TContext>> ExecuteCoreAsync(TContext context, CancellationToken cancellationToken)
        {
            Logger?.LogDebug("开始执行通用事务步骤: {StepName}", StepName);
            var startTime = DateTime.UtcNow;

            try
            {
                // 执行具体步骤逻辑
                var stepResult = await ExecuteAsync(context, cancellationToken);
                var duration = DateTime.UtcNow - startTime;

                Logger?.LogDebug(
                    "通用事务步骤执行完成: {StepName}, 结果: {IsSuccess}, 耗时: {Duration}ms",
                    StepName, stepResult.IsSuccess, duration.TotalMilliseconds);

                return stepResult.IsSuccess
                    ? TransactionResult<TContext>.FromSuccess(context)
                    : TransactionResult<TContext>.FromError(stepResult.Message, stepResult.Exception);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                Logger?.LogError(ex, "通用事务步骤执行失败: {StepName}, 耗时: {Duration}ms",
                    StepName, duration.TotalMilliseconds);

                return TransactionResult<TContext>.FromError($"通用事务步骤 {StepName} 执行失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行步骤操作
        /// 子类需要实现具体的步骤逻辑
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行结果</returns>
        public abstract Task<TransactionStepResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// 补偿操作（回滚）
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>补偿结果</returns>
        public virtual async Task<TransactionStepResult> CompensateAsync(TContext context, CancellationToken cancellationToken = default)
        {
            Logger?.LogDebug("执行通用事务步骤补偿: {StepName}", StepName);
            return await Task.FromResult(TransactionStepResult.Success($"通用步骤 {StepName} 补偿完成"));
        }
    }
}
