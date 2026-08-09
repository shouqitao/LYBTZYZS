using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Infrastructure.ExceptionHandling;

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
    /// 获取用户友好的错误消息
    /// </summary>
    string GetUserFriendlyMessage(Exception exception);

    #region 全局异常处理

    /// <summary>
    /// 注册全局异常处理器（AppDomain.UnhandledException, TaskScheduler.UnobservedTaskException）
    /// optimize-desktop-core: 统一全局异常处理入口
    /// </summary>
    void RegisterGlobalExceptionHandlers();

    #endregion

    #region Result支持（从IExceptionHandler合并）

    /// <summary>
    /// 处理异常并返回用户友好的结果
    /// </summary>
    Result<T> HandleException<T>(Exception exception, string methodName, string? context = null);

    /// <summary>
    /// 处理异常并返回无数据的结果
    /// </summary>
    Result HandleExceptionWithResult(Exception exception, string methodName, string? context = null);

    #endregion
}
