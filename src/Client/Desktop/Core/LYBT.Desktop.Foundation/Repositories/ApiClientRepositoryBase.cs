using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Repositories;

/// <summary>
/// 客户端 API 仓储基类 — 统一 try/catch + 日志 + 异常处理模板
/// </summary>
/// <typeparam name="TListDto">列表 DTO 类型</typeparam>
/// <typeparam name="TDetailDto">详情 DTO 类型</typeparam>
public abstract class ApiClientRepositoryBase<TListDto, TDetailDto>
{
    protected readonly ILogger Logger;

    protected ApiClientRepositoryBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 日志前缀 — 子类可重写，默认为类名
    /// </summary>
    protected virtual string LogPrefix => GetType().Name;

    /// <summary>
    /// 统一异常处理：记录错误日志后重新抛出
    /// </summary>
    protected void HandleException(Exception ex, string operation, params object?[] args)
    {
        var message = args.Length > 0
            ? $"[REPO] {LogPrefix}.{operation} failed"
            : $"[REPO] {LogPrefix}.{operation} failed";

        Logger.LogError(ex, message, args);
        throw ex;
    }

    /// <summary>
    /// 执行无返回值的异步操作，统一日志和异常处理
    /// </summary>
    /// <param name="action">实际操作</param>
    /// <param name="operation">操作名称（用于日志）</param>
    /// <param name="logLevel">操作开始时的日志级别</param>
    protected async Task ExecuteAsync(
        Func<Task> action,
        string operation,
        LogLevel logLevel = LogLevel.Debug)
    {
        try
        {
            Logger.Log(logLevel, "[REPO] {LogPrefix}.{Operation}", LogPrefix, operation);
            await action();
        }
        catch (Exception ex)
        {
            HandleException(ex, operation);
        }
    }

    /// <summary>
    /// 执行有返回值的异步操作，统一日志和异常处理
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="func">实际操作</param>
    /// <param name="operation">操作名称（用于日志）</param>
    /// <param name="logLevel">操作开始时的日志级别</param>
    protected async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> func,
        string operation,
        LogLevel logLevel = LogLevel.Debug)
    {
        try
        {
            Logger.Log(logLevel, "[REPO] {LogPrefix}.{Operation}", LogPrefix, operation);
            return await func();
        }
        catch (Exception ex)
        {
            HandleException(ex, operation);
            throw; // unreachable, but satisfies compiler
        }
    }

    /// <summary>
    /// 执行有返回值的异步操作，统一日志和异常处理（支持带参数的日志消息）
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="func">实际操作</param>
    /// <param name="operation">操作名称（用于日志）</param>
    /// <param name="logMessage">带占位符的日志消息</param>
    /// <param name="logArgs">日志参数</param>
    /// <param name="logLevel">操作开始时的日志级别</param>
    protected async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> func,
        string operation,
        string logMessage,
        object?[] logArgs,
        LogLevel logLevel = LogLevel.Debug)
    {
        try
        {
            Logger.Log(logLevel, logMessage, logArgs);
            return await func();
        }
        catch (Exception ex)
        {
            HandleException(ex, operation);
            throw; // unreachable, but satisfies compiler
        }
    }
}
