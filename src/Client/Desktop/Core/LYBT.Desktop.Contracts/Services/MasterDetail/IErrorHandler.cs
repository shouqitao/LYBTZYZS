namespace LYBT.Desktop.Contracts.Services.MasterDetail;

/// <summary>
/// 错误处理接口
/// OpenSpec: unify-desktop-architecture (Phase 1.2)
/// 为MasterDetail模式提供统一的错误处理
/// 注：实现时委托给IUserNotificationService
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// 处理异常
    /// </summary>
    /// <param name="ex">异常对象</param>
    /// <param name="context">错误上下文描述</param>
    void HandleError(Exception ex, string? context = null);

    /// <summary>
    /// 处理异常（异步）
    /// </summary>
    /// <param name="ex">异常对象</param>
    /// <param name="context">错误上下文描述</param>
    Task HandleErrorAsync(Exception ex, string? context = null);

    /// <summary>
    /// 获取安全的错误消息（用于显示给用户）
    /// </summary>
    /// <param name="ex">异常对象</param>
    /// <returns>用户友好的错误消息</returns>
    string GetSafeErrorMessage(Exception ex);

    /// <summary>
    /// 获取操作失败的安全消息
    /// </summary>
    /// <param name="operation">操作名称</param>
    /// <param name="ex">异常对象</param>
    /// <returns>用户友好的操作失败消息</returns>
    string GetOperationFailureMessage(string operation, Exception ex);

    /// <summary>
    /// 记录错误日志（不显示给用户）
    /// </summary>
    /// <param name="ex">异常对象</param>
    /// <param name="context">错误上下文描述</param>
    void LogError(Exception ex, string? context = null);
}
