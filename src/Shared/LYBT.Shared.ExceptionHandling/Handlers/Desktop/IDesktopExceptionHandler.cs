using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.ExceptionHandling.Handlers;

/// <summary>
/// Desktop端异常处理器接口
/// consolidate-exception-handling: 从LYBT.Desktop.Foundation迁移，合并IExceptionHandler功能
/// </summary>
public interface IDesktopExceptionHandler
{
    /// <summary>
    /// 处理异常
    /// </summary>
    void HandleException(Exception exception, string? context = null);

    /// <summary>
    /// 异步处理异常
    /// </summary>
    Task HandleExceptionAsync(Exception exception, string? context = null);

    /// <summary>
    /// 记录异常
    /// </summary>
    void LogException(Exception exception, ExceptionSeverity severity = ExceptionSeverity.Error);

    /// <summary>
    /// 获取用户友好的错误消息
    /// </summary>
    string GetUserFriendlyMessage(Exception exception);

    /// <summary>
    /// 判断是否可重试
    /// </summary>
    bool CanRetry(Exception exception);

    #region ServiceResult支持（从IExceptionHandler合并）

    /// <summary>
    /// 处理异常并返回用户友好的结果
    /// </summary>
    ServiceResult<T> HandleException<T>(Exception exception, string methodName, string? context = null);

    /// <summary>
    /// 处理异常并返回无数据的结果
    /// </summary>
    ServiceResult HandleExceptionWithResult(Exception exception, string methodName, string? context = null);

    /// <summary>
    /// 安全执行操作，自动处理异常
    /// </summary>
    Task<ServiceResult<T>> SafeExecuteAsync<T>(Func<Task<ServiceResult<T>>> operation, string methodName, string? context = null);

    /// <summary>
    /// 安全执行无返回值的操作，自动处理异常
    /// </summary>
    Task<ServiceResult> SafeExecuteAsync(Func<Task<ServiceResult>> operation, string methodName, string? context = null);

    #endregion
}
