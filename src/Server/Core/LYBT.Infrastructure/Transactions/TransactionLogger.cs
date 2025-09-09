using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Transactions
{
    /// <summary>
    /// 事务日志记录器
    /// </summary>
    public class TransactionLogger
    {
        private readonly ILogger<TransactionLogger> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionLogger"/> class.
        /// 初始化事务日志记录器
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public TransactionLogger(ILogger<TransactionLogger> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 记录事务开始
        /// </summary>
        /// <param name="transactionId">事务ID</param>
        /// <param name="transactionName">事务名称</param>
        /// <param name="userId">用户ID</param>
        public void LogTransactionStart(Guid transactionId, string transactionName = "", Guid? userId = null)
        {
            _logger.LogInformation(
                "事务开始 - ID: {TransactionId}, 名称: {TransactionName}, 用户: {UserId}",
                transactionId, transactionName, userId);
        }

        /// <summary>
        /// 记录事务完成
        /// </summary>
        /// <param name="transactionId">事务ID</param>
        /// <param name="success">是否成功</param>
        /// <param name="duration">执行时长</param>
        /// <param name="message">附加消息</param>
        public void LogTransactionComplete(Guid transactionId, bool success, TimeSpan duration, string message = "")
        {
            if (success)
            {
                _logger.LogInformation(
                    "事务完成 - ID: {TransactionId}, 成功: {Success}, 耗时: {Duration}ms, 消息: {Message}",
                    transactionId, success, duration.TotalMilliseconds, message);
            }
            else
            {
                _logger.LogWarning(
                    "事务失败 - ID: {TransactionId}, 成功: {Success}, 耗时: {Duration}ms, 消息: {Message}",
                    transactionId, success, duration.TotalMilliseconds, message);
            }
        }

        /// <summary>
        /// 记录事务步骤执行
        /// </summary>
        /// <param name="transactionId">事务ID</param>
        /// <param name="stepName">步骤名称</param>
        /// <param name="success">是否成功</param>
        /// <param name="duration">执行时长</param>
        /// <param name="message">附加消息</param>
        public void LogStepExecution(Guid transactionId, string stepName, bool success, TimeSpan duration, string message = "")
        {
            _logger.LogDebug(
                "事务步骤 - 事务ID: {TransactionId}, 步骤: {StepName}, 成功: {Success}, 耗时: {Duration}ms, 消息: {Message}",
                transactionId, stepName, success, duration.TotalMilliseconds, message);
        }

        /// <summary>
        /// 记录事务异常
        /// </summary>
        /// <param name="transactionId">事务ID</param>
        /// <param name="exception">异常信息</param>
        /// <param name="context">异常上下文</param>
        public void LogTransactionException(Guid transactionId, Exception exception, string context = "")
        {
            _logger.LogError(exception, "事务异常 - ID: {TransactionId}, 上下文: {Context}",
                transactionId, context);
        }

        /// <summary>
        /// 记录事务回滚
        /// </summary>
        /// <param name="transactionId">事务ID</param>
        /// <param name="reason">回滚原因</param>
        public void LogTransactionRollback(Guid transactionId, string reason)
        {
            _logger.LogWarning(
                "事务回滚 - ID: {TransactionId}, 原因: {Reason}",
                transactionId, reason);
        }

        /// <summary>
        /// 异步记录事务统计信息
        /// </summary>
        /// <param name="transactionId">事务ID</param>
        /// <param name="statistics">统计信息</param>
        /// <returns>记录任务</returns>
        public async Task LogTransactionStatisticsAsync(Guid transactionId, Dictionary<string, object> statistics)
        {
            await Task.Run(() =>
            {
                _logger.LogInformation(
                    "事务统计 - ID: {TransactionId}, 统计信息: {@Statistics}",
                    transactionId, statistics);
            });
        }
    }
}
