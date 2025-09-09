using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Transactions
{
    /// <summary>
    /// 数据库事务步骤基类
    /// </summary>
    /// <typeparam name="TContext">事务上下文类型</typeparam>
    public abstract class DatabaseTransactionStep<TContext> : ITransactionStep<TContext>
        where TContext : TransactionContext
    {
        /// <summary>
        /// 数据库上下文
        /// </summary>
        protected readonly AppDbContext DbContext;

        /// <summary>
        /// 日志记录器
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseTransactionStep{TContext}"/> class.
        /// 初始化数据库事务步骤
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="logger">日志记录器</param>
        protected DatabaseTransactionStep(AppDbContext dbContext, ILogger logger)
        {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public virtual string StepName => GetType().Name;

        /// <inheritdoc />
        public abstract int Order { get; }

        /// <inheritdoc />
        public abstract bool SupportsCompensation { get; }

        /// <inheritdoc />
        public abstract TimeSpan Timeout { get; }

        /// <summary>
        /// 获取步骤描述
        /// </summary>
        public virtual string StepDescription => StepName;

        /// <inheritdoc />
        public virtual async Task<bool> CanExecuteAsync(TContext context, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                Logger.LogError("Transaction context is null");
                return false;
            }

            try
            {
                // 检查数据库连接
                await DbContext.Database.CanConnectAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Database connection check failed for step: {StepName}", StepName);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<TransactionResult<TContext>> ExecuteCoreAsync(TContext context, CancellationToken cancellationToken)
        {
            Logger.LogDebug("开始执行数据库事务步骤: {StepName}", StepName);
            var startTime = DateTime.UtcNow;

            try
            {
                // 执行前验证
                var validationResult = await ValidateAsync(context, cancellationToken);
                if (!validationResult.IsSuccess)
                {
                    return TransactionResult<TContext>.FromError(validationResult.Message, validationResult.Exception);
                }

                // 执行数据库操作
                var stepResult = await ExecuteDatabaseOperationAsync(context, cancellationToken);
                var duration = DateTime.UtcNow - startTime;

                Logger.LogDebug(
                    "数据库事务步骤执行完成: {StepName}, 结果: {IsSuccess}, 耗时: {Duration}ms",
                    StepName, stepResult.IsSuccess, duration.TotalMilliseconds);

                return stepResult.IsSuccess
                    ? TransactionResult<TContext>.FromSuccess(context)
                    : TransactionResult<TContext>.FromError(stepResult.Message, stepResult.Exception);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                Logger.LogError(ex, "数据库事务步骤执行失败: {StepName}, 耗时: {Duration}ms",
                    StepName, duration.TotalMilliseconds);

                return TransactionResult<TContext>.FromError($"事务步骤 {StepName} 执行失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行数据库操作
        /// 子类需要实现具体的数据库操作逻辑
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行结果</returns>
        protected abstract Task<TransactionStepResult> ExecuteDatabaseOperationAsync(TContext context, CancellationToken cancellationToken = default);

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

        /// <summary>
        /// 补偿操作（回滚）
        /// </summary>
        /// <param name="context">事务上下文</param>
        /// <param name="originalResult">原始执行结果</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>补偿结果</returns>
        public virtual Task<TransactionStepResult> CompensateAsync(TContext context, TransactionStepResult originalResult, CancellationToken cancellationToken = default)
        {
            Logger.LogDebug("执行事务步骤补偿: {StepName}", StepName);
            return Task.FromResult(TransactionStepResult.Success($"步骤 {StepName} 补偿完成"));
        }

        #region 数据库实体操作辅助方法

        /// <summary>
        /// 创建数据库实体
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="entity">要创建的实体</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建的实体</returns>
        protected async Task<TEntity> CreateEntityAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class
        {
            DbContext.Set<TEntity>().Add(entity);
            await DbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }

        /// <summary>
        /// 删除数据库实体
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="id">实体ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否删除成功</returns>
        protected async Task<bool> DeleteEntityAsync<TEntity>(Guid id, CancellationToken cancellationToken) where TEntity : class
        {
            var entity = await DbContext.Set<TEntity>().FindAsync(new object[] { id }, cancellationToken);
            if (entity == null)
            {
                return false;
            }

            DbContext.Set<TEntity>().Remove(entity);
            await DbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        #endregion

        #region 结果创建辅助方法

        /// <summary>
        /// 创建实体操作成功结果
        /// </summary>
        /// <param name="entityType">实体类型名称</param>
        /// <param name="entityId">实体ID</param>
        /// <param name="action">操作类型</param>
        /// <returns>成功结果</returns>
        protected TransactionStepResult CreateEntitySuccessResult(string entityType, Guid entityId, string action)
        {
            return new TransactionStepResult
            {
                IsSuccess = true,
                Message = $"{entityType} {action} 操作成功",
                Data = new Dictionary<string, object>
                {
                    ["EntityType"] = entityType,
                    ["EntityId"] = entityId,
                    ["Action"] = action
                }
            };
        }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        /// <param name="data">结果数据</param>
        /// <param name="message">成功消息</param>
        /// <returns>成功结果</returns>
        protected TransactionStepResult CreateSuccessResult(Dictionary<string, object> data, string message = "操作成功")
        {
            return new TransactionStepResult
            {
                IsSuccess = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        /// <param name="exception">异常信息</param>
        /// <param name="data">结果数据</param>
        /// <param name="message">失败消息</param>
        /// <returns>失败结果</returns>
        protected TransactionStepResult CreateFailureResult(Exception exception, Dictionary<string, object> data, string message = "操作失败")
        {
            return new TransactionStepResult
            {
                IsSuccess = false,
                Message = message,
                Exception = exception,
                Data = data
            };
        }

        #endregion
    }
}
