using System.Collections.Concurrent;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services
{

    /// <summary>
    /// API调用优化服务
    /// 提供请求防抖、批量处理、智能重试等功能
    /// </summary>
    public class ApiOptimizationService
    {
        private readonly ILogger<ApiOptimizationService> _logger;
        private readonly ConcurrentDictionary<string, DebounceRequest> _debounceRequests;
        private readonly ConcurrentDictionary<string, BatchRequest> _batchRequests;
        private readonly Timer _cleanupTimer;

        // 配置参数
        private readonly TimeSpan _defaultDebounceDelay = TimeSpan.FromMilliseconds(300);

        private readonly TimeSpan _defaultBatchWindow = TimeSpan.FromMilliseconds(500);
        private readonly int _maxRetryAttempts = 3;
        private readonly TimeSpan _baseRetryDelay = TimeSpan.FromSeconds(1);

        public ApiOptimizationService(ILogger<ApiOptimizationService> logger)
        {
            _logger = logger;
            _debounceRequests = new ConcurrentDictionary<string, DebounceRequest>();
            _batchRequests = new ConcurrentDictionary<string, BatchRequest>();

            // 定期清理过期的请求
            _cleanupTimer = new Timer(CleanupExpiredRequests, null,
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        #region 防抖处理

        /// <summary>
        /// 防抖执行API请求
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="key">请求标识键</param>
        /// <param name="apiCall">API调用委托</param>
        /// <param name="delay">防抖延迟，null使用默认值</param>
        /// <returns>API调用结果</returns>
        public async Task<T> DebounceAsync<T>(string key, Func<Task<T>> apiCall, TimeSpan? delay = null)
        {
            var debounceDelay = delay ?? _defaultDebounceDelay;
            var requestKey = $"debounce_{key}";

            var request = _debounceRequests.AddOrUpdate(
                requestKey,
                k => new DebounceRequest<T>
                {
                    Key = k,
                    ApiCall = apiCall,
                    TaskCompletionSource = new TaskCompletionSource<object>(),
                    CreatedAt = DateTime.UtcNow,
                    Delay = debounceDelay
                },
                (k, existing) =>
                {
                    // 取消现有的定时器并重置
                    existing.CancellationTokenSource?.Cancel();
                    existing.TaskCompletionSource = new TaskCompletionSource<object>();
                    existing.ApiCall = apiCall;
                    existing.CreatedAt = DateTime.UtcNow;
                    return existing;
                });

            // 启动延迟执行
            request.CancellationTokenSource = new CancellationTokenSource();

            _ = Task.Delay(debounceDelay, request.CancellationTokenSource.Token)
                .ContinueWith(
                    async t =>
                {
                    if (!t.IsCanceled)
                    {
                        try
                        {
                            _logger.LogDebug("执行防抖API请求: {Key}", key);
                            var result = await ((Func<Task<T>>)request.ApiCall)();
                            request.TaskCompletionSource.SetResult(result!);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "防抖API请求失败: {Key}", key);
                            request.TaskCompletionSource.SetException(ex);
                        }
                        finally
                        {
                            _debounceRequests.TryRemove(requestKey, out _);
                        }
                    }
                }, TaskContinuationOptions.OnlyOnRanToCompletion);

            return (T)await request.TaskCompletionSource.Task;
        }

        #endregion 防抖处理

        #region 批量处理

        /// <summary>
        /// 批量执行API请求
        /// </summary>
        /// <typeparam name="TInput">输入类型</typeparam>
        /// <typeparam name="TOutput">输出类型</typeparam>
        /// <param name="batchKey">批处理标识键</param>
        /// <param name="input">输入数据</param>
        /// <param name="batchApiCall">批量API调用委托</param>
        /// <param name="batchWindow">批处理窗口时间</param>
        /// <returns>对应输入的结果</returns>
        public async Task<TOutput> BatchAsync<TInput, TOutput>(
            string batchKey,
            TInput input,
            Func<List<TInput>, Task<List<TOutput>>> batchApiCall,
            TimeSpan? batchWindow = null)
        {
            var windowTime = batchWindow ?? _defaultBatchWindow;
            var requestKey = $"batch_{batchKey}";

            var batchRequest = _batchRequests.AddOrUpdate(
                requestKey,
                k => new BatchRequest<TInput, TOutput>
                {
                    Key = k,
                    BatchApiCall = batchApiCall,
                    Inputs = new List<TInput>(),
                    TaskCompletionSources = new List<TaskCompletionSource<TOutput>>(),
                    CreatedAt = DateTime.UtcNow,
                    BatchWindow = windowTime
                },
                (k, existing) =>
                {
                    // 如果批处理窗口还在有效期内，加入现有批次
                    if (DateTime.UtcNow - existing.CreatedAt < existing.BatchWindow)
                    {
                        return (BatchRequest<TInput, TOutput>)existing;
                    }

                    // 否则创建新的批次
                    return new BatchRequest<TInput, TOutput>
                    {
                        Key = k,
                        BatchApiCall = batchApiCall,
                        Inputs = new List<TInput>(),
                        TaskCompletionSources = new List<TaskCompletionSource<TOutput>>(),
                        CreatedAt = DateTime.UtcNow,
                        BatchWindow = windowTime
                    };
                });

            var typedBatchRequest = (BatchRequest<TInput, TOutput>)batchRequest;
            var tcs = new TaskCompletionSource<TOutput>();

            lock (typedBatchRequest)
            {
                typedBatchRequest.Inputs.Add(input);
                typedBatchRequest.TaskCompletionSources.Add(tcs);

                // 如果是第一个请求，启动批处理定时器
                if (typedBatchRequest.Inputs.Count == 1)
                {
                    _ = Task.Delay(windowTime).ContinueWith(async _ =>
                    {
                        await ExecuteBatch(typedBatchRequest);
                        _batchRequests.TryRemove(requestKey, out var _);
                    });
                }
            }

            return await tcs.Task;
        }

        private async Task ExecuteBatch<TInput, TOutput>(BatchRequest<TInput, TOutput> batchRequest)
        {
            try
            {
                _logger.LogDebug(
                    "执行批量API请求: {Key}, 数量: {Count}",
                    batchRequest.Key, batchRequest.Inputs.Count);

                var results = await batchRequest.BatchApiCall(batchRequest.Inputs);

                // 将结果分发给各个等待的任务
                for (int i = 0; i < batchRequest.TaskCompletionSources.Count; i++)
                {
                    if (i < results.Count)
                    {
                        batchRequest.TaskCompletionSources[i].SetResult(results[i]);
                    }
                    else
                    {
                        batchRequest.TaskCompletionSources[i].SetException(
                            new InvalidOperationException("批处理结果数量不匹配"));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量API请求失败: {Key}", batchRequest.Key);

                // 将异常传递给所有等待的任务
                foreach (var tcs in batchRequest.TaskCompletionSources)
                {
                    tcs.SetException(ex);
                }
            }
        }

        #endregion 批量处理

        #region 重试机制

        /// <summary>
        /// 带重试的API请求执行
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="apiCall">API调用委托</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <param name="baseDelay">基础重试延迟</param>
        /// <param name="shouldRetry">重试条件判断</param>
        /// <returns>API调用结果</returns>
        public async Task<T> RetryAsync<T>(
            Func<Task<T>> apiCall,
            int? maxRetries = null,
            TimeSpan? baseDelay = null,
            Func<Exception, int, bool>? shouldRetry = null)
        {
            var maxAttempts = maxRetries ?? _maxRetryAttempts;
            var retryDelay = baseDelay ?? _baseRetryDelay;
            var retryCriteria = shouldRetry ?? DefaultShouldRetry;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    return await apiCall();
                }
                catch (Exception ex) when (attempt < maxAttempts && retryCriteria(ex, attempt))
                {
                    var delay = CalculateExponentialBackoff(retryDelay, attempt);

                    _logger.LogWarning(
                        ex,
                        "API请求失败，第 {Attempt}/{MaxAttempts} 次重试，{Delay}ms 后重试",
                        attempt, maxAttempts, delay.TotalMilliseconds);

                    await Task.Delay(delay);
                }
            }

            // 最后一次尝试，不捕获异常
            return await apiCall();
        }

        /// <summary>
        /// 默认重试条件（网络错误和超时错误）
        /// </summary>
        private bool DefaultShouldRetry(Exception exception, int attemptNumber)
        {
            // 这里可以根据具体的异常类型判断是否应该重试
            return exception is TaskCanceledException ||
                   exception is TimeoutException ||
                   (exception is HttpRequestException httpEx && IsRetryableHttpError(httpEx));
        }

        private bool IsRetryableHttpError(HttpRequestException httpEx)
        {
            // 判断HTTP错误是否可重试（如网络错误、服务器临时错误等）
            return httpEx.Message.Contains("timeout") ||
                   httpEx.Message.Contains("connection") ||
                   httpEx.Message.Contains("network");
        }

        /// <summary>
        /// 计算指数退避延迟
        /// </summary>
        private TimeSpan CalculateExponentialBackoff(TimeSpan baseDelay, int attemptNumber)
        {
            var exponentialDelay = TimeSpan.FromMilliseconds(
                baseDelay.TotalMilliseconds * Math.Pow(2, attemptNumber - 1));

            // 添加随机抖动避免惊群效应
            var jitter = Random.Shared.Next(0, (int)(exponentialDelay.TotalMilliseconds * 0.1));

            return exponentialDelay.Add(TimeSpan.FromMilliseconds(jitter));
        }

        #endregion 重试机制

        #region 清理机制

        /// <summary>
        /// 清理过期的请求
        /// </summary>
        private void CleanupExpiredRequests(object? state)
        {
            var now = DateTime.UtcNow;
            var expiredThreshold = TimeSpan.FromMinutes(5);

            // 清理过期的防抖请求
            var expiredDebounceKeys = _debounceRequests
                .Where(kvp => now - kvp.Value.CreatedAt > expiredThreshold)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredDebounceKeys)
            {
                if (_debounceRequests.TryRemove(key, out var request))
                {
                    request.CancellationTokenSource?.Cancel();
                    _logger.LogDebug("清理过期的防抖请求: {Key}", key);
                }
            }

            // 清理过期的批处理请求
            var expiredBatchKeys = _batchRequests
                .Where(kvp => now - kvp.Value.CreatedAt > expiredThreshold)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredBatchKeys)
            {
                if (_batchRequests.TryRemove(key, out _))
                {
                    _logger.LogDebug("清理过期的批处理请求: {Key}", key);
                }
            }
        }

        #endregion 清理机制

        #region IDisposable

        public void Dispose()
        {
            _cleanupTimer?.Dispose();

            // 取消所有未完成的防抖请求
            foreach (var request in _debounceRequests.Values)
            {
                request.CancellationTokenSource?.Cancel();
            }
        }

        #endregion IDisposable
    }

    #region 内部数据结构

    /// <summary>
    /// 防抖请求
    /// </summary>
    internal abstract class DebounceRequest
    {
        public string Key { get; set; } = string.Empty;
        public object ApiCall { get; set; } = null!;
        public TaskCompletionSource<object> TaskCompletionSource { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public TimeSpan Delay { get; set; }
        public CancellationTokenSource? CancellationTokenSource { get; set; }
    }

    /// <summary>
    /// 泛型防抖请求
    /// </summary>
    internal class DebounceRequest<T> : DebounceRequest
    {
        public new Func<Task<T>> ApiCall { get; set; } = null!;
    }

    /// <summary>
    /// 批处理请求
    /// </summary>
    internal abstract class BatchRequest
    {
        public string Key { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public TimeSpan BatchWindow { get; set; }
    }

    /// <summary>
    /// 泛型批处理请求
    /// </summary>
    internal class BatchRequest<TInput, TOutput> : BatchRequest
    {
        public Func<List<TInput>, Task<List<TOutput>>> BatchApiCall { get; set; } = null!;
        public List<TInput> Inputs { get; set; } = new();
        public List<TaskCompletionSource<TOutput>> TaskCompletionSources { get; set; } = new();
    }

    #endregion 内部数据结构
}
