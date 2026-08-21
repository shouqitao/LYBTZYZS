using Microsoft.Extensions.Logging;
using System.Runtime.ExceptionServices;

namespace LYBT.Desktop.Foundation.Repositories;

/// <summary>
/// 仓储执行辅助（T3.4）
/// 将 ApiClientRepositoryBase 的 ExecuteAsync/HandleException 抽为组合式静态辅助，
/// 替代继承链 ApiClientRepositoryBase → EntityApiClientRepositoryBase
/// </summary>
public static class RepositoryExecutionHelper
{
    public static void HandleException(ILogger logger, string logPrefix, Exception ex, string operation)
    {
        logger.LogError(ex, "[REPO] {LogPrefix}.{Operation} failed", logPrefix, operation);
        ExceptionDispatchInfo.Capture(ex).Throw();
    }

    public static async Task ExecuteAsync(ILogger logger, string logPrefix, Func<Task> action, string operation, LogLevel logLevel = LogLevel.Debug)
    {
        try
        {
            logger.Log(logLevel, "[REPO] {LogPrefix}.{Operation}", logPrefix, operation);
            await action();
        }
        catch (Exception ex)
        {
            HandleException(logger, logPrefix, ex, operation);
        }
    }

    public static async Task<TResult> ExecuteAsync<TResult>(ILogger logger, string logPrefix, Func<Task<TResult>> func, string operation, LogLevel logLevel = LogLevel.Debug)
    {
        try
        {
            logger.Log(logLevel, "[REPO] {LogPrefix}.{Operation}", logPrefix, operation);
            return await func();
        }
        catch (Exception ex)
        {
            HandleException(logger, logPrefix, ex, operation);
            throw;
        }
    }
}
