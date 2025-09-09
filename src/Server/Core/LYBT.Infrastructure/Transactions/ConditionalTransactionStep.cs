using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Transactions
{
    /// <summary>
    /// 条件事务步骤抽象基类
    /// 提供基于条件判断的事务步骤执行逻辑
    /// </summary>
    /// <typeparam name="TContext">事务上下文类型</typeparam>
    public abstract class ConditionalTransactionStep<TContext> : ITransactionStep<TContext>
        where TContext : TransactionContext
    {
        /// <summary>
        /// 日志记录器
        /// </summary>
        protected readonly ILogger? Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConditionalTransactionStep{TContext}"/> class.
        /// 初始化条件事务步骤
        /// </summary>
        /// <param name="logger">日志记录器</param>
        protected ConditionalTransactionStep(ILogger? logger)
        {
            Logger = logger;
        }

        /// <inheritdoc />
        public abstract string StepName { get; }

        /// <inheritdoc />
        public abstract int Order { get; }

        /// <inheritdoc />
        public virtual bool SupportsCompensation => true;

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

            try
            {
                // 评估执行条件
                return await EvaluateConditionAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Failed to evaluate condition for step: {StepName}", StepName);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<TransactionResult<TContext>> ExecuteCoreAsync(TContext context, CancellationToken cancellationToken)
        {
            Logger?.LogDebug("开始执行条件事务步骤: {StepName}", StepName);
            var startTime = DateTime.UtcNow;

            try
            {
                // 先评估条件
                var shouldExecute = await EvaluateConditionAsync(context, cancellationToken);
                if (!shouldExecute)
                {
                    Logger?.LogDebug("跳过条件事务步骤: {StepName} - 条件不满足", StepName);
                    return TransactionResult<TContext>.FromSuccess(context);
                }

                // 执行条件操作
                var stepResult = await ExecuteConditionalOperationAsync(context, cancellationToken);
                var duration = DateTime.UtcNow - startTime;

                Logger?.LogDebug(
                    "条件事务步骤执行完成: {StepName}, 结果: {IsSuccess}, 耗时: {Duration}ms",
                    StepName, stepResult.IsSuccess, duration.TotalMilliseconds);

                return stepResult.IsSuccess
                    ? TransactionResult<TContext>.FromSuccess(context)
                    : TransactionResult<TContext>.FromError(stepResult.Message, stepResult.Exception);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                Logger?.LogError(ex, "条件事务步骤执行失败: {StepName}, 耗时: {Duration}ms",
                    StepName, duration.TotalMilliseconds);

                return TransactionResult<TContext>.FromError($"条件事务步骤 {StepName} 执行失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 评估执行条件
        /// 子类需要实现具体的条件判断逻辑
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否满足执行条件</returns>
        protected abstract Task<bool> EvaluateConditionAsync(TContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行条件操作
        /// 子类需要实现具体的条件操作逻辑
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行结果</returns>
        protected abstract Task<TransactionStepResult> ExecuteConditionalOperationAsync(TContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// 补偿操作（回滚）
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>补偿结果</returns>
        public virtual async Task<TransactionStepResult> CompensateAsync(TContext context, CancellationToken cancellationToken = default)
        {
            Logger?.LogDebug("执行条件事务步骤补偿: {StepName}", StepName);
            return await Task.FromResult(TransactionStepResult.Success($"条件步骤 {StepName} 补偿完成"));
        }
    }
}
