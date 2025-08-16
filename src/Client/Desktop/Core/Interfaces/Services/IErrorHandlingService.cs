using LYBT.Desktop.Core.Models.Common;
using System;
using System.Threading.Tasks;
using SharedCommon = LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// 统一错误处理服务接口
    /// </summary>
    public interface IErrorHandlingService
    {
        /// <summary>
        /// 处理异常并返回用户友好的错误信息
        /// </summary>
        /// <param name="exception">原始异常</param>
        /// <param name="context">错误上下文</param>
        /// <returns>处理后的错误信息</returns>
        SharedCommon.HandledError HandleException(Exception exception, ErrorContext? context = null);

        /// <summary>
        /// 异步处理异常
        /// </summary>
        /// <param name="exception">原始异常</param>
        /// <param name="context">错误上下文</param>
        /// <returns>处理后的错误信息</returns>
        Task<SharedCommon.HandledError> HandleExceptionAsync(Exception exception, ErrorContext? context = null);

        /// <summary>
        /// 显示错误通知给用户
        /// </summary>
        /// <param name="handledError">处理后的错误信息</param>
        /// <param name="showDialog">是否显示对话框</param>
        Task ShowErrorAsync(SharedCommon.HandledError handledError, bool showDialog = true);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="handledError">处理后的错误信息</param>
        Task LogErrorAsync(SharedCommon.HandledError handledError);

        /// <summary>
        /// 获取用户友好的错误消息
        /// </summary>
        /// <param name="exception">异常</param>
        /// <param name="defaultMessage">默认消息</param>
        /// <returns>用户友好的错误消息</returns>
        string GetUserFriendlyMessage(Exception exception, string? defaultMessage = null);

        /// <summary>
        /// 检查异常是否可重试
        /// </summary>
        /// <param name="exception">异常</param>
        /// <returns>是否可重试</returns>
        bool CanRetry(Exception exception);

        /// <summary>
        /// 获取异常的错误分类
        /// </summary>
        /// <param name="exception">异常</param>
        /// <returns>错误分类</returns>
        SharedCommon.ErrorCategory GetErrorCategory(Exception exception);

        /// <summary>
        /// 获取异常的严重程度
        /// </summary>
        /// <param name="exception">异常</param>
        /// <returns>错误严重程度</returns>
        SharedCommon.ErrorSeverity GetErrorSeverity(Exception exception);

        /// <summary>
        /// 获取建议的恢复操作
        /// </summary>
        /// <param name="exception">异常</param>
        /// <returns>建议操作列表</returns>
        string[] GetSuggestedActions(Exception exception);

        /// <summary>
        /// 安全执行操作，自动处理异常
        /// </summary>
        /// <param name="operation">要执行的操作</param>
        /// <param name="context">错误上下文</param>
        /// <param name="showErrorDialog">出错时是否显示错误对话框</param>
        /// <returns>操作是否成功</returns>
        Task<bool> ExecuteSafelyAsync(Func<Task> operation, ErrorContext? context = null, bool showErrorDialog = true);

        /// <summary>
        /// 安全执行操作并返回结果，自动处理异常
        /// </summary>
        /// <param name="operation">要执行的操作</param>
        /// <param name="context">错误上下文</param>
        /// <param name="showErrorDialog">出错时是否显示错误对话框</param>
        /// <returns>操作结果，失败时返回默认值</returns>
        Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> operation, ErrorContext? context = null, bool showErrorDialog = true);

        /// <summary>
        /// 注册全局未处理异常处理器
        /// </summary>
        void RegisterGlobalExceptionHandlers();

        /// <summary>
        /// 错误发生事件
        /// </summary>
        event EventHandler<SharedCommon.HandledError>? ErrorOccurred;

        /// <summary>
        /// 严重错误发生事件
        /// </summary>
        event EventHandler<SharedCommon.HandledError>? CriticalErrorOccurred;
    }
}