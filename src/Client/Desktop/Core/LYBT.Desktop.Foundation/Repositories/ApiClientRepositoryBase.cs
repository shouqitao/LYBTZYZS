using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;
using System.Runtime.ExceptionServices;

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
    /// 统一异常处理：记录错误日志后重新抛出（保留原始堆栈）
    /// </summary>
    protected void HandleException(Exception ex, string operation)
    {
        Logger.LogError(ex, "[REPO] {LogPrefix}.{Operation} failed", LogPrefix, operation);
        ExceptionDispatchInfo.Capture(ex).Throw();
    }

    /// <summary>
    /// 执行无返回值的异步操作，统一日志和异常处理
    /// </summary>
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
            throw; // unreachable — HandleException 已抛出，此行满足编译器
        }
    }

    /// <summary>
    /// 执行有返回值的异步操作，统一日志和异常处理（支持带参数的日志消息）
    /// </summary>
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
            throw; // unreachable — HandleException 已抛出，此行满足编译器
        }
    }

    /// <summary>
    /// 批量删除执行模板 — 统一 try/catch + 日志；失败/异常时返回失败结果 DTO（不抛异常）
    /// </summary>
    protected async Task<BatchOperationResultDto?> ExecuteBatchDeleteAsync(
        Func<Task<ApiResponse<BatchOperationResultDto>>> func,
        string operation,
        string failureMessage,
        int totalCount)
    {
        try
        {
            Logger.LogInformation("[REPO] {LogPrefix}.{Operation} - Count={Count}", LogPrefix, operation, totalCount);

            var response = await func();
            if (!response.Success || response.Data == null)
            {
                return new BatchOperationResultDto
                {
                    TotalCount = totalCount,
                    FailureCount = totalCount,
                    IsSuccess = false,
                    Message = response.Message ?? failureMessage
                };
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] {LogPrefix}.{Operation} failed", LogPrefix, operation);
            return new BatchOperationResultDto
            {
                TotalCount = totalCount,
                FailureCount = totalCount,
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    /// <summary>
    /// 批量导入执行模板 — 统一 try/catch + 日志；业务拒绝（Success=false，422）记 Information，基础设施异常记 Error
    /// </summary>
    protected async Task<TResult?> ExecuteImportAsync<TResult>(
        Func<Task<ApiResponse<TResult>>> func,
        string operation,
        int count,
        CancellationToken ct = default)
    {
        try
        {
            Logger.LogInformation("[REPO] {LogPrefix}.{Operation} started - Count={Count}", LogPrefix, operation, count);

            var response = await func();
            if (!response.Success || response.Data == null)
            {
                Logger.LogInformation(
                    "[REPO] {LogPrefix}.{Operation} business rejection: {Message} - Count={Count}",
                    LogPrefix,
                    operation,
                    response.Message ?? "unknown",
                    count);
                return default;
            }

            Logger.LogInformation(
                "[REPO] {LogPrefix}.{Operation} completed - Count={Count}",
                LogPrefix,
                operation,
                count);
            return response.Data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REPO] {LogPrefix}.{Operation} failed - Count={Count}", LogPrefix, operation, count);
            return default;
        }
    }
}
