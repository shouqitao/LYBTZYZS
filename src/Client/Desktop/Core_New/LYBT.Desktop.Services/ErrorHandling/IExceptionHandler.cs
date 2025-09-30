namespace LYBT.Desktop.Services.ErrorHandling
{
    /// <summary>
    /// 异常处理器接口 - 简化版本
    /// 遵循"适度设计、拒绝过度工程"原则，提供基本的异常处理功能
    /// </summary>
    public interface IExceptionHandler
    {
        /// <summary>
        /// 处理异常
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <param name="context">上下文信息</param>
        void HandleException(Exception exception, string? context = null);

        /// <summary>
        /// 异步处理异常
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <param name="context">上下文信息</param>
        Task HandleExceptionAsync(Exception exception, string? context = null);

        /// <summary>
        /// 记录异常
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <param name="severity">严重程度</param>
        void LogException(Exception exception, ExceptionSeverity severity = ExceptionSeverity.Error);

        /// <summary>
        /// 获取用户友好的错误消息
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <returns>友好的错误消息</returns>
        string GetUserFriendlyMessage(Exception exception);

        /// <summary>
        /// 判断是否可重试
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <returns>是否可重试</returns>
        bool CanRetry(Exception exception);
    }

    /// <summary>
    /// 异常严重程度
    /// </summary>
    public enum ExceptionSeverity
    {
        /// <summary>
        /// 信息级别
        /// </summary>
        Information = 0,

        /// <summary>
        /// 警告级别
        /// </summary>
        Warning = 1,

        /// <summary>
        /// 错误级别
        /// </summary>
        Error = 2,

        /// <summary>
        /// 严重错误级别
        /// </summary>
        Critical = 3
    }
}
