using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Transactions
{
    /// <summary>
    /// 数据库事务步骤基类
    /// </summary>
    /// <typeparam name="TContext">事务上下文类型</typeparam>
    public abstract class DatabaseTransactionStep<TContext>
        where TContext : TransactionContext
    {
        /// <summary>
        /// 日志记录器
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// 初始化数据库事务步骤
        /// </summary>
        /// <param name="logger">日志记录器</param>
        protected DatabaseTransactionStep(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取步骤名称
        /// </summary>
        public virtual string StepName => GetType().Name;

        /// <summary>
        /// 获取步骤描述
        /// </summary>
        public virtual string StepDescription => StepName;

        /// <summary>
        /// 执行事务步骤
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行结果</returns>
        public async Task<TransactionStepResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
        {
            Logger.LogDebug("开始执行事务步骤: {StepName}", StepName);
            var startTime = DateTime.UtcNow;

            try
            {
                // 执行前验证
                var validationResult = await ValidateAsync(context, cancellationToken);
                if (!validationResult.IsSuccess)
                {
                    return validationResult;
                }

                // 执行核心逻辑
                var result = await ExecuteCoreAsync(context, cancellationToken);
                result.Duration = DateTime.UtcNow - startTime;

                Logger.LogDebug("事务步骤执行完成: {StepName}, 结果: {IsSuccess}, 耗时: {Duration}ms",
                    StepName, result.IsSuccess, result.Duration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                Logger.LogError(ex, "事务步骤执行失败: {StepName}, 耗时: {Duration}ms", StepName, duration.TotalMilliseconds);

                var failureResult = TransactionStepResult.Failure($"事务步骤 {StepName} 执行失败: {ex.Message}", ex);
                failureResult.Duration = duration;
                return failureResult;
            }
        }

        /// <summary>
        /// 执行核心业务逻辑
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行结果</returns>
        protected abstract Task<TransactionStepResult> ExecuteCoreAsync(TContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行前验证
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证结果</returns>
        protected virtual Task<TransactionStepResult> ValidateAsync(TContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(TransactionStepResult.Success());
        }

        /// <summary>
        /// 回滚操作
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>回滚结果</returns>
        public virtual Task<TransactionStepResult> RollbackAsync(TContext context, CancellationToken cancellationToken = default)
        {
            Logger.LogDebug("执行事务步骤回滚: {StepName}", StepName);
            return Task.FromResult(TransactionStepResult.Success($"步骤 {StepName} 回滚完成"));
        }
    }
}