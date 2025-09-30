using LYBT.Desktop.Services.Exceptions;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Extensions;

/// <summary>
/// Service异常处理扩展方法 - DT-006技术债务修复
/// 简化Service类中的异常处理调用
/// </summary>
public static class ServiceExceptionExtensions
{
    /// <summary>
    /// 安全执行异步操作，自动处理异常
    /// </summary>
    /// <typeparam name="T">返回数据类型</typeparam>
    /// <param name="operation">要执行的异步操作</param>
    /// <param name="exceptionHandler">异常处理器</param>
    /// <param name="methodName">方法名称（用于日志记录）</param>
    /// <param name="context">操作上下文（可选）</param>
    /// <returns>操作结果</returns>
    public static async Task<ServiceResult<T>> ExecuteSafelyAsync<T>(
        this Func<Task<ServiceResult<T>>> operation,
        Exceptions.IExceptionHandler exceptionHandler,
        string methodName,
        string? context = null)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            if (exceptionHandler is StandardExceptionHandler standardHandler)
            {
                return standardHandler.HandleException<T>(ex, methodName, context);
            }
            // 回退到默认处理
            return ServiceResult<T>.Failure("操作失败，请稍后重试");
        }
    }

    /// <summary>
    /// 安全执行无返回值的异步操作，自动处理异常
    /// </summary>
    /// <param name="operation">要执行的异步操作</param>
    /// <param name="exceptionHandler">异常处理器</param>
    /// <param name="methodName">方法名称（用于日志记录）</param>
    /// <param name="context">操作上下文（可选）</param>
    /// <returns>操作结果</returns>
    public static async Task<ServiceResult> ExecuteSafelyAsync(
        this Func<Task<ServiceResult>> operation,
        Exceptions.IExceptionHandler exceptionHandler,
        string methodName,
        string? context = null)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            if (exceptionHandler is StandardExceptionHandler standardHandler)
            {
                return standardHandler.HandleException(ex, methodName, context);
            }
            // 回退到默认处理
            return ServiceResult.Failure("操作失败，请稍后重试");
        }
    }

    /// <summary>
    /// 安全执行同步操作，自动处理异常
    /// </summary>
    /// <typeparam name="T">返回数据类型</typeparam>
    /// <param name="operation">要执行的同步操作</param>
    /// <param name="exceptionHandler">异常处理器</param>
    /// <param name="methodName">方法名称（用于日志记录）</param>
    /// <param name="context">操作上下文（可选）</param>
    /// <returns>操作结果</returns>
    public static ServiceResult<T> ExecuteSafely<T>(
        this Func<ServiceResult<T>> operation,
        Exceptions.IExceptionHandler exceptionHandler,
        string methodName,
        string? context = null)
    {
        try
        {
            return operation();
        }
        catch (Exception ex)
        {
            if (exceptionHandler is StandardExceptionHandler standardHandler)
            {
                return standardHandler.HandleException<T>(ex, methodName, context);
            }
            // 回退到默认处理
            return ServiceResult<T>.Failure("操作失败，请稍后重试");
        }
    }

    /// <summary>
    /// 安全执行无返回值的同步操作，自动处理异常
    /// </summary>
    /// <param name="operation">要执行的同步操作</param>
    /// <param name="exceptionHandler">异常处理器</param>
    /// <param name="methodName">方法名称（用于日志记录）</param>
    /// <param name="context">操作上下文（可选）</param>
    /// <returns>操作结果</returns>
    public static ServiceResult ExecuteSafely(
        this Func<ServiceResult> operation,
        Exceptions.IExceptionHandler exceptionHandler,
        string methodName,
        string? context = null)
    {
        try
        {
            return operation();
        }
        catch (Exception ex)
        {
            if (exceptionHandler is StandardExceptionHandler standardHandler)
            {
                return standardHandler.HandleException(ex, methodName, context);
            }
            // 回退到默认处理
            return ServiceResult.Failure("操作失败，请稍后重试");
        }
    }

    /// <summary>
    /// 为Service类提供便捷的异常处理方法
    /// </summary>
    /// <typeparam name="T">返回数据类型</typeparam>
    /// <param name="service">服务实例</param>
    /// <param name="operation">要执行的操作</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="methodName">方法名称</param>
    /// <param name="context">操作上下文（可选）</param>
    /// <returns>操作结果</returns>
    public static async Task<ServiceResult<T>> HandleExceptionAsync<T>(
        this object service,
        Func<Task<ServiceResult<T>>> operation,
        ILogger logger,
        string methodName,
        string? context = null)
    {
        var exceptionHandler = new StandardExceptionHandler(
            (ILogger<StandardExceptionHandler>)logger);

        return await operation.ExecuteSafelyAsync(exceptionHandler, methodName, context);
    }

    /// <summary>
    /// 为Service类提供便捷的无返回值异常处理方法
    /// </summary>
    /// <param name="service">服务实例</param>
    /// <param name="operation">要执行的操作</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="methodName">方法名称</param>
    /// <param name="context">操作上下文（可选）</param>
    /// <returns>操作结果</returns>
    public static async Task<ServiceResult> HandleExceptionAsync(
        this object service,
        Func<Task<ServiceResult>> operation,
        ILogger logger,
        string methodName,
        string? context = null)
    {
        var exceptionHandler = new StandardExceptionHandler(
            (ILogger<StandardExceptionHandler>)logger);

        return await operation.ExecuteSafelyAsync(exceptionHandler, methodName, context);
    }
}
