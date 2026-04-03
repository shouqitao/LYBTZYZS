using System;
using System.Threading.Tasks;

namespace LYBT.Desktop.Infrastructure.Extensions;

/// <summary>
/// Task扩展方法
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// 安全触发异步任务，不等待完成（Safe Fire and Forget）
    /// 用于从同步方法（如事件处理器）安全启动异步操作
    /// </summary>
    /// <param name="task">要执行的任务</param>
    /// <param name="onException">异常处理回调（可选）</param>
    /// <param name="continueOnCapturedContext">是否在捕获的上下文中继续执行（默认false）</param>
    public static void SafeFireAndForget(
        this Task task,
        Action<Exception>? onException = null,
        bool continueOnCapturedContext = false)
    {
        _ = SafeFireAndForgetInternal(task, onException, continueOnCapturedContext);
    }

    /// <summary>
    /// 安全触发异步任务，不等待完成（带返回值版本）
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="task">要执行的任务</param>
    /// <param name="onException">异常处理回调（可选）</param>
    /// <param name="continueOnCapturedContext">是否在捕获的上下文中继续执行（默认false）</param>
    public static void SafeFireAndForget<T>(
        this Task<T> task,
        Action<Exception>? onException = null,
        bool continueOnCapturedContext = false)
    {
        _ = SafeFireAndForgetInternal(task, onException, continueOnCapturedContext);
    }

    private static async Task SafeFireAndForgetInternal(
        Task task,
        Action<Exception>? onException,
        bool continueOnCapturedContext)
    {
        try
        {
            await task.ConfigureAwait(continueOnCapturedContext);
        }
        catch (Exception ex) when (onException != null)
        {
            onException(ex);
        }
    }
}
