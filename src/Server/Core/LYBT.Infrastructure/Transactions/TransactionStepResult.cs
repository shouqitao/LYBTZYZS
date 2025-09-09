using System;

namespace LYBT.Infrastructure.Transactions
{
    /// <summary>
    /// 事务步骤执行结果
    /// </summary>
    public class TransactionStepResult
    {
        /// <summary>
        /// 获取或设置执行是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 获取或设置执行结果消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置执行过程中的异常信息
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// 获取或设置执行耗时
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 获取或设置步骤执行的额外数据
        /// </summary>
        public Dictionary<string, object>? Data { get; set; }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        /// <param name="message">成功消息</param>
        /// <returns>成功的事务步骤结果</returns>
        public static TransactionStepResult Success(string message = "操作成功")
        {
            return new TransactionStepResult
            {
                IsSuccess = true,
                Message = message
            };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        /// <param name="message">失败消息</param>
        /// <param name="exception">异常信息</param>
        /// <returns>失败的事务步骤结果</returns>
        public static TransactionStepResult Failure(string message, Exception? exception = null)
        {
            return new TransactionStepResult
            {
                IsSuccess = false,
                Message = message,
                Exception = exception
            };
        }
    }
}