using System;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Services;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Extensions
{
    /// <summary>
    /// SmartLoadingManager扩展方法 - 简化实际使用
    /// </summary>
    public static class SmartLoadingExtensions
    {
        /// <summary>
        /// 执行带加载状态的异步操作
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="loadingManager">加载管理器</param>
        /// <param name="operationId">操作唯一标识</param>
        /// <param name="operation">异步操作</param>
        /// <param name="message">加载提示信息</param>
        /// <param name="layer">加载层级</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        public static async Task<T> ExecuteWithLoadingAsync<T>(
            this ISmartLoadingManager loadingManager,
            string operationId,
            Func<CancellationToken, Task<T>> operation,
            string message = "处理中...",
            int layer = 1,
            CancellationToken cancellationToken = default)
        {
            using var loadingOperation = loadingManager.StartLoading(operationId, message, layer, false, cancellationToken);

            try
            {
                var result = await operation(loadingOperation.CancellationToken);
                loadingOperation.Complete();
                return result;
            }
            catch (OperationCanceledException)
            {
                // 操作被取消，正常流程
                throw;
            }
            catch (Exception)
            {
                // 异常时自动完成加载状态
                loadingOperation.Complete();
                throw;
            }
        }

        /// <summary>
        /// 执行带加载状态的异步操作（无返回值）
        /// </summary>
        /// <param name="loadingManager">加载管理器</param>
        /// <param name="operationId">操作唯一标识</param>
        /// <param name="operation">异步操作</param>
        /// <param name="message">加载提示信息</param>
        /// <param name="layer">加载层级</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async Task ExecuteWithLoadingAsync(
            this ISmartLoadingManager loadingManager,
            string operationId,
            Func<CancellationToken, Task> operation,
            string message = "处理中...",
            int layer = 1,
            CancellationToken cancellationToken = default)
        {
            using var loadingOperation = loadingManager.StartLoading(operationId, message, layer, false, cancellationToken);

            try
            {
                await operation(loadingOperation.CancellationToken);
                loadingOperation.Complete();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                loadingOperation.Complete();
                throw;
            }
        }

        /// <summary>
        /// 执行带进度跟踪的异步操作
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="loadingManager">加载管理器</param>
        /// <param name="operationId">操作唯一标识</param>
        /// <param name="operation">带进度报告的异步操作</param>
        /// <param name="message">加载提示信息</param>
        /// <param name="layer">加载层级</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        public static async Task<T> ExecuteWithProgressAsync<T>(
            this ISmartLoadingManager loadingManager,
            string operationId,
            Func<IProgress<ProgressInfo>, CancellationToken, Task<T>> operation,
            string message = "处理中...",
            int layer = 1,
            CancellationToken cancellationToken = default)
        {
            using var loadingOperation = loadingManager.StartLoading(operationId, message, layer, true, cancellationToken);

            var progress = new Progress<ProgressInfo>(info =>
            {
                loadingOperation.UpdateProgress(info.Percentage, info.Message);
            });

            try
            {
                var result = await operation(progress, loadingOperation.CancellationToken);
                loadingOperation.Complete();
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                loadingOperation.Complete();
                throw;
            }
        }

        /// <summary>
        /// 执行批量操作，每个子操作更新进度
        /// </summary>
        /// <param name="loadingManager">加载管理器</param>
        /// <param name="operationId">操作唯一标识</param>
        /// <param name="items">要处理的项目列表</param>
        /// <param name="itemProcessor">单个项目处理函数</param>
        /// <param name="message">加载提示信息</param>
        /// <param name="layer">加载层级</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果列表</returns>
        public static async Task<TResult[]> ExecuteBatchWithProgressAsync<TItem, TResult>(
            this ISmartLoadingManager loadingManager,
            string operationId,
            TItem[] items,
            Func<TItem, CancellationToken, Task<TResult>> itemProcessor,
            string message = "批量处理中...",
            int layer = 1,
            CancellationToken cancellationToken = default)
        {
            if (items.Length == 0)
            {
                return Array.Empty<TResult>();
            }

            using var loadingOperation = loadingManager.StartLoading(operationId, message, layer, true, cancellationToken);

            var results = new TResult[items.Length];

            try
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (loadingOperation.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var item = items[i];
                    results[i] = await itemProcessor(item, loadingOperation.CancellationToken);

                    var progress = (int)((i + 1.0) / items.Length * 100);
                    loadingOperation.UpdateProgress(progress, $"{message} ({i + 1}/{items.Length})");
                }

                loadingOperation.Complete();
                return results;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                loadingOperation.Complete();
                throw;
            }
        }

        /// <summary>
        /// 创建分层加载操作 - 用于复杂的多步骤操作
        /// </summary>
        /// <param name="loadingManager">加载管理器</param>
        /// <param name="baseOperationId">基础操作ID</param>
        /// <param name="steps">操作步骤</param>
        /// <param name="baseMessage">基础消息</param>
        /// <param name="layer">加载层级</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>所有步骤的结果</returns>
        public static async Task<object[]> ExecuteMultiStepAsync(
            this ISmartLoadingManager loadingManager,
            string baseOperationId,
            MultiStepOperation[] steps,
            string baseMessage = "执行多步骤操作...",
            int layer = 1,
            CancellationToken cancellationToken = default)
        {
            using var mainOperation = loadingManager.StartLoading(baseOperationId, baseMessage, layer, true, cancellationToken);

            var results = new object[steps.Length];

            try
            {
                for (int i = 0; i < steps.Length; i++)
                {
                    if (mainOperation.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var step = steps[i];
                    var stepOperationId = $"{baseOperationId}_step_{i}";

                    // 更新主操作进度
                    var overallProgress = (int)(i / (double)steps.Length * 100);
                    mainOperation.UpdateProgress(overallProgress, step.Description);

                    // 执行步骤
                    results[i] = await loadingManager.ExecuteWithLoadingAsync(
                        stepOperationId,
                        step.Operation,
                        step.Description,
                        layer + 1, // 使用更深的层级
                        mainOperation.CancellationToken
                    );
                }

                mainOperation.UpdateProgress(100, "所有步骤完成");
                mainOperation.Complete();
                return results;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                mainOperation.Complete();
                throw;
            }
        }
    }

    /// <summary>
    /// 进度信息
    /// </summary>
    public class ProgressInfo
    {
        public int Percentage { get; set; }
        public string? Message { get; set; }

        public ProgressInfo(int percentage, string? message = null)
        {
            Percentage = percentage;
            Message = message;
        }
    }

    /// <summary>
    /// 多步骤操作定义
    /// </summary>
    public class MultiStepOperation
    {
        public string Description { get; set; } = string.Empty;
        public Func<CancellationToken, Task<object>> Operation { get; set; } = null!;

        public MultiStepOperation(string description, Func<CancellationToken, Task<object>> operation)
        {
            Description = description;
            Operation = operation;
        }
    }
}
