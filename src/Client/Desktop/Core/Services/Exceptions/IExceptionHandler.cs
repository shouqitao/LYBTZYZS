using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Services.Exceptions;

/// <summary>
/// 统一异常处理器接口 - DT-006技术债务修复
/// </summary>
/// <summary>
/// 统一异常处理器接口 - DT-006技术债务修复
/// </summary>
public interface IExceptionHandler
{
    /// <summary>
    /// 处理异常并返回用户友好的结果
    /// </summary>
    /// <typeparam name="T">返回数据类型</typeparam>
    /// <param name="exception">异常实例</param>
    /// <param name="methodName">发生异常的方法名</param>
    /// <param name="context">异常上下文信息</param>
    /// <returns>包含错误信息的ServiceResult</returns>
    ServiceResult<T> HandleException<T>(Exception exception, string methodName, string? context = null);

    /// <summary>
    /// 处理异常并返回无数据的结果
    /// </summary>
    /// <param name="exception">异常实例</param>
    /// <param name="methodName">发生异常的方法名</param>
    /// <param name="context">异常上下文信息</param>
    /// <returns>包含错误信息的ServiceResult</returns>
    ServiceResult HandleException(Exception exception, string methodName, string? context = null);

    /// <summary>
    /// 安全执行操作，自动处理异常
    /// </summary>
    /// <typeparam name="T">返回数据类型</typeparam>
    /// <param name="operation">要执行的异步操作</param>
    /// <param name="methodName">方法名称（用于日志记录）</param>
    /// <param name="context">操作上下文（可选）</param>
    /// <returns>操作结果</returns>
    Task<ServiceResult<T>> HandleException<T>(Func<Task<ServiceResult<T>>> operation, string methodName, string? context = null);

    /// <summary>
    /// 安全执行无返回值的操作，自动处理异常
    /// </summary>
    /// <param name="operation">要执行的异步操作</param>
    /// <param name="methodName">方法名称（用于日志记录）</param>
    /// <param name="context">操作上下文（可选）</param>
    /// <returns>操作结果</returns>
    Task<ServiceResult> HandleException(Func<Task<ServiceResult>> operation, string methodName, string? context = null);

    /// <summary>
    /// 安全执行支持取消令牌的操作，自动处理异常 - DT-011取消令牌支持
    /// </summary>
    /// <typeparam name="T">返回数据类型</typeparam>
    /// <param name="operation">要执行的支持取消的异步操作</param>
    /// <param name="methodName">方法名称（用于日志记录）</param>
    /// <param name="context">操作上下文（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<ServiceResult<T>> HandleException<T>(Func<CancellationToken, Task<ServiceResult<T>>> operation, string methodName, string? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 安全执行支持取消令牌的无返回值操作，自动处理异常 - DT-011取消令牌支持
    /// </summary>
    /// <param name="operation">要执行的支持取消的异步操作</param>
    /// <param name="methodName">方法名称（用于日志记录）</param>
    /// <param name="context">操作上下文（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<ServiceResult> HandleException(Func<CancellationToken, Task<ServiceResult>> operation, string methodName, string? context = null, CancellationToken cancellationToken = default);
}
