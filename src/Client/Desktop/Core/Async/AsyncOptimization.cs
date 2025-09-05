using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Async
{
    /// <summary>
    /// 异步操作优化辅助类
    /// </summary>
    public static class AsyncOptimization
    {
        /// <summary>
        /// 并行ForEach优化版本
        /// </summary>
        public static async Task ParallelForEachAsync<T>(
            IEnumerable<T> source,
            Func<T, Task> body,
            int maxDegreeOfParallelism = 4,
            CancellationToken cancellationToken = default)
        {
            var throttler = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
            var tasks = source.Select(async item =>
            {
                await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await body(item).ConfigureAwait(false);
                }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// 批处理异步操作
        /// </summary>
        public static async Task<IEnumerable<TResult>> BatchAsync<TSource, TResult>(
            IEnumerable<TSource> source,
            Func<TSource, Task<TResult>> selector,
            int batchSize = 10,
            CancellationToken cancellationToken = default)
        {
            var results = new List<TResult>();
            var batches = source.Batch(batchSize);

            foreach (var batch in batches)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var batchResults = await Task.WhenAll(
                    batch.Select(item => selector(item))
                ).ConfigureAwait(false);
                results.AddRange(batchResults);
            }

            return results;
        }

        /// <summary>
        /// 带超时的异步操作
        /// </summary>
        public static async Task<T> WithTimeout<T>(
            this Task<T> task,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var completedTask = await Task.WhenAny(
                task,
                Task.Delay(Timeout.Infinite, linkedCts.Token)
            ).ConfigureAwait(false);

            if (completedTask == task)
            {
                return await task.ConfigureAwait(false);
            }

            throw new TimeoutException($"操作超时：{timeout}");
        }

        /// <summary>
        /// 批处理扩展方法
        /// </summary>
        private static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            var batch = new List<T>(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Count > 0)
            {
                yield return batch;
            }
        }
    }

    /// <summary>
    /// 异步懒加载
    /// </summary>
    public class AsyncLazy<T>
    {
        private readonly Lazy<Task<T>> _lazy;

        public AsyncLazy(Func<Task<T>> taskFactory)
        {
            _lazy = new Lazy<Task<T>>(() => System.Threading.Tasks.Task.Run(taskFactory));
        }

        public AsyncLazy(Func<T> valueFactory)
        {
            _lazy = new Lazy<Task<T>>(() => System.Threading.Tasks.Task.Run(valueFactory));
        }

        public TaskAwaiter<T> GetAwaiter()
        {
            return _lazy.Value.GetAwaiter();
        }

        public Task<T> Task => _lazy.Value;
        public bool IsValueCreated => _lazy.IsValueCreated;
    }

    /// <summary>
    /// 异步信号量（避免lock）
    /// </summary>
    public class AsyncSemaphore
    {
        private readonly SemaphoreSlim _semaphore;

        public AsyncSemaphore(int initialCount = 1)
        {
            _semaphore = new SemaphoreSlim(initialCount, initialCount);
        }

        public async Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new Releaser(_semaphore);
        }

        private class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public Releaser(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }

    /// <summary>
    /// 异步集合操作优化
    /// </summary>
    public static class AsyncCollectionExtensions
    {
        /// <summary>
        /// 异步Select
        /// </summary>
        public static async Task<IEnumerable<TResult>> SelectAsync<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, Task<TResult>> selector)
        {
            var tasks = source.Select(selector);
            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步Where
        /// </summary>
        public static async Task<IEnumerable<T>> WhereAsync<T>(
            this IEnumerable<T> source,
            Func<T, Task<bool>> predicate)
        {
            var results = new List<T>();
            foreach (var item in source)
            {
                if (await predicate(item).ConfigureAwait(false))
                {
                    results.Add(item);
                }
            }
            return results;
        }

        /// <summary>
        /// 异步FirstOrDefault
        /// </summary>
        public static async Task<T?> FirstOrDefaultAsync<T>(
            this IEnumerable<T> source,
            Func<T, Task<bool>> predicate) where T : class
        {
            foreach (var item in source)
            {
                if (await predicate(item).ConfigureAwait(false))
                {
                    return item;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// ConfigureAwait最佳实践分析器
    /// </summary>
    public class ConfigureAwaitAnalyzer
    {
        private readonly ILogger<ConfigureAwaitAnalyzer>? _logger;

        public ConfigureAwaitAnalyzer(ILogger<ConfigureAwaitAnalyzer>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// 分析方法是否需要ConfigureAwait(false)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ShouldUseConfigureAwaitFalse(string context)
        {
            // UI相关操作不应该使用ConfigureAwait(false)
            var uiContexts = new[] { "ViewModel", "View", "Window", "Control", "Dispatcher" };
            if (uiContexts.Any(ctx => context.Contains(ctx, StringComparison.OrdinalIgnoreCase)))
            {
                _logger?.LogTrace($"UI上下文，不使用ConfigureAwait(false): {context}");
                return false;
            }

            // 库代码应该使用ConfigureAwait(false)
            var libraryContexts = new[] { "Service", "Repository", "Helper", "Utility", "Manager" };
            if (libraryContexts.Any(ctx => context.Contains(ctx, StringComparison.OrdinalIgnoreCase)))
            {
                _logger?.LogTrace($"库代码，使用ConfigureAwait(false): {context}");
                return true;
            }

            // 默认使用ConfigureAwait(false)
            return true;
        }
    }

    /// <summary>
    /// 任务调度器优化
    /// </summary>
    public class OptimizedTaskScheduler : TaskScheduler
    {
        private readonly BlockingCollection<Task> _tasks = new();
        private readonly Thread[] _threads;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public OptimizedTaskScheduler(int concurrencyLevel = 0)
        {
            if (concurrencyLevel == 0)
            {
                concurrencyLevel = Environment.ProcessorCount;
            }

            _threads = new Thread[concurrencyLevel];
            for (int i = 0; i < concurrencyLevel; i++)
            {
                _threads[i] = new Thread(ProcessTasks)
                {
                    IsBackground = true,
                    Name = $"OptimizedTaskScheduler-{i}"
                };
                _threads[i].Start();
            }
        }

        protected override IEnumerable<Task>? GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // 允许内联执行
            return Thread.CurrentThread.Name?.StartsWith("OptimizedTaskScheduler") == true
                && TryExecuteTask(task);
        }

        private void ProcessTasks()
        {
            foreach (var task in _tasks.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                TryExecuteTask(task);
            }
        }

        public void Shutdown()
        {
            _cancellationTokenSource.Cancel();
            _tasks.CompleteAdding();

            foreach (var thread in _threads)
            {
                thread.Join(TimeSpan.FromSeconds(5));
            }
        }
    }

    /// <summary>
    /// 异步操作性能监控
    /// </summary>
    public class AsyncPerformanceMonitor
    {
        private readonly ConcurrentDictionary<string, OperationStatistics> _statistics = new();
        private readonly ILogger<AsyncPerformanceMonitor>? _logger;

        public AsyncPerformanceMonitor(ILogger<AsyncPerformanceMonitor>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// 监控异步操作
        /// </summary>
        public async Task<T> MonitorAsync<T>(string operationName, Func<Task<T>> operation)
        {
            var sw = Stopwatch.StartNew();
            var stats = _statistics.GetOrAdd(operationName, _ => new OperationStatistics { Name = operationName });

            try
            {
                var result = await operation().ConfigureAwait(false);
                sw.Stop();

                stats.RecordSuccess(sw.ElapsedMilliseconds);

                if (sw.ElapsedMilliseconds > 1000)
                {
                    _logger?.LogWarning($"慢操作检测: {operationName} 耗时 {sw.ElapsedMilliseconds}ms");
                }

                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                stats.RecordFailure(sw.ElapsedMilliseconds);
                _logger?.LogError(ex, $"异步操作失败: {operationName}");
                throw;
            }
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public IReadOnlyDictionary<string, OperationStatistics> GetStatistics()
        {
            return _statistics;
        }

        /// <summary>
        /// 操作统计
        /// </summary>
        public class OperationStatistics
        {
            public string Name { get; set; } = string.Empty;
            private long _totalCalls;
            private long _successCount;
            private long _failureCount;

            public long TotalCalls => _totalCalls;
            public long SuccessCount => _successCount;
            public long FailureCount => _failureCount;
            public double AverageMs { get; private set; }
            public double MaxMs { get; private set; }
            public double MinMs { get; private set; } = double.MaxValue;

            public void RecordSuccess(double elapsedMs)
            {
                Interlocked.Increment(ref _totalCalls);
                Interlocked.Increment(ref _successCount);
                UpdateStatistics(elapsedMs);
            }

            public void RecordFailure(double elapsedMs)
            {
                Interlocked.Increment(ref _totalCalls);
                Interlocked.Increment(ref _failureCount);
                UpdateStatistics(elapsedMs);
            }

            private void UpdateStatistics(double elapsedMs)
            {
                // 简化的统计更新（实际应该使用线程安全的方式）
                AverageMs = ((AverageMs * (TotalCalls - 1)) + elapsedMs) / TotalCalls;
                MaxMs = Math.Max(MaxMs, elapsedMs);
                MinMs = Math.Min(MinMs, elapsedMs);
            }
        }
    }

    /// <summary>
    /// 数据流优化
    /// </summary>
    public class DataflowOptimization
    {
        /// <summary>
        /// 创建优化的转换块
        /// </summary>
        public static TransformBlock<TInput, TOutput> CreateTransformBlock<TInput, TOutput>(
            Func<TInput, Task<TOutput>> transform,
            int maxDegreeOfParallelism = 4,
            int boundedCapacity = 100)
        {
            return new TransformBlock<TInput, TOutput>(
                transform,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = maxDegreeOfParallelism,
                    BoundedCapacity = boundedCapacity,
                    EnsureOrdered = false // 不保证顺序以提高性能
                });
        }

        /// <summary>
        /// 创建批处理块
        /// </summary>
        public static BatchBlock<T> CreateBatchBlock<T>(int batchSize, TimeSpan? timeout = null)
        {
            var block = new BatchBlock<T>(batchSize);

            if (timeout.HasValue)
            {
                // 定时触发批处理
                var timer = new Timer(_ => block.TriggerBatch(), null, timeout.Value, timeout.Value);
            }

            return block;
        }
    }
}
