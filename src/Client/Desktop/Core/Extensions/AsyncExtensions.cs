using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace LYBT.Desktop.Core.Extensions
{
    /// <summary>
    /// 异步扩展方法 - 第3阶段质量优化
    /// 提供安全的异步操作扩展，避免死锁和超时
    /// </summary>
    public static class AsyncExtensions
    {
        /// <summary>
        /// 安全地获取异步任务结果，避免死锁
        /// </summary>
        public static T GetResultSafely<T>(this Task<T> task)
        {
            return task.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 安全地执行异步任务，避免死锁
        /// </summary>
        public static void GetResultSafely(this Task task)
        {
            task.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 带超时的异步执行
        /// </summary>
        public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token)).ConfigureAwait(false);
            
            if (completedTask == task)
            {
                cts.Cancel(); // 取消延迟任务
                return await task.ConfigureAwait(false);
            }
            
            throw new TimeoutException($"操作超时，超时时间: {timeout}");
        }

        /// <summary>
        /// 带超时的异步执行（无返回值）
        /// </summary>
        public static async Task WithTimeout(this Task task, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token)).ConfigureAwait(false);
            
            if (completedTask == task)
            {
                cts.Cancel(); // 取消延迟任务
                await task.ConfigureAwait(false);
                return;
            }
            
            throw new TimeoutException($"操作超时，超时时间: {timeout}");
        }

        /// <summary>
        /// 带取消令牌的异步执行
        /// </summary>
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }
            
            return await task.ConfigureAwait(false);
        }

        /// <summary>
        /// 带重试的异步执行
        /// </summary>
        public static async Task<T> WithRetry<T>(
            this Func<Task<T>> taskFactory,
            int maxRetries = 3,
            TimeSpan? retryDelay = null,
            Func<Exception, int, bool>? shouldRetry = null)
        {
            var delay = retryDelay ?? TimeSpan.FromSeconds(1);
            shouldRetry ??= (_, attempt) => attempt < maxRetries;

            Exception? lastException = null;
            
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await taskFactory().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (attempt < maxRetries && shouldRetry(ex, attempt))
                    {
                        await Task.Delay(delay).ConfigureAwait(false);
                        // 指数退避
                        delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            
            throw lastException ?? new InvalidOperationException("重试失败");
        }

        /// <summary>
        /// 异步操作转换为后台任务
        /// </summary>
        public static void FireAndForget(
            this Task task,
            Action<Exception>? errorHandler = null)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted && errorHandler != null)
                {
                    var exception = t.Exception?.Flatten();
                    if (exception != null)
                    {
                        errorHandler(exception);
                    }
                }
            }, TaskScheduler.Default);
        }

        /// <summary>
        /// 并行执行多个异步任务，带进度报告
        /// </summary>
        public static async Task<T[]> WhenAllWithProgress<T>(
            IProgress<double> progress,
            params Task<T>[] tasks)
        {
            var results = new T[tasks.Length];
            var completed = 0;
            var total = tasks.Length;

            async Task<T> WrapTask(Task<T> task, int index)
            {
                var result = await task.ConfigureAwait(false);
                results[index] = result;
                
                var completedCount = Interlocked.Increment(ref completed);
                progress?.Report((double)completedCount / total);
                
                return result;
            }

            var wrappedTasks = tasks.Select((task, index) => WrapTask(task, index));
            await Task.WhenAll(wrappedTasks).ConfigureAwait(false);
            
            return results;
        }

        /// <summary>
        /// 顺序执行多个异步任务
        /// </summary>
        public static async Task<TResult> Sequential<TResult>(
            params Func<Task>[] taskFactories)
        {
            foreach (var taskFactory in taskFactories)
            {
                await taskFactory().ConfigureAwait(false);
            }
            
            return default!;
        }

        /// <summary>
        /// 延迟执行异步操作
        /// </summary>
        public static async Task DelayedExecution(
            TimeSpan delay,
            Func<Task> operation,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            await operation().ConfigureAwait(false);
        }

        /// <summary>
        /// 确保在UI线程执行
        /// </summary>
        public static async Task RunOnUIThread(Func<Task> operation)
        {
            if (System.Windows.Application.Current?.Dispatcher != null)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(operation);
            }
            else
            {
                await operation().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 确保在后台线程执行
        /// </summary>
        public static Task<T> RunOnBackgroundThread<T>(Func<T> operation)
        {
            return Task.Run(operation);
        }

        /// <summary>
        /// 异步操作结果缓存
        /// </summary>
        public static Func<Task<T>> Memoize<T>(this Func<Task<T>> factory)
        {
            var lazy = new Lazy<Task<T>>(() => Task.Run(factory));
            return () => lazy.Value;
        }

        /// <summary>
        /// 限制并发执行数量
        /// </summary>
        public static async Task<T[]> ThrottleAsync<T>(
            IEnumerable<Func<Task<T>>> taskFactories,
            int maxConcurrency)
        {
            using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = taskFactories.Select(async taskFactory =>
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    return await taskFactory().ConfigureAwait(false);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            
            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步操作的断路器模式
        /// </summary>
        public class CircuitBreaker
        {
            private readonly int _threshold;
            private readonly TimeSpan _timeout;
            private int _failureCount = 0;
            private DateTime _lastFailureTime = DateTime.MinValue;
            private readonly object _lock = new();

            public CircuitBreaker(int threshold = 3, TimeSpan? timeout = null)
            {
                _threshold = threshold;
                _timeout = timeout ?? TimeSpan.FromMinutes(1);
            }

            public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
            {
                lock (_lock)
                {
                    if (_failureCount >= _threshold)
                    {
                        if (DateTime.UtcNow - _lastFailureTime < _timeout)
                        {
                            throw new InvalidOperationException("断路器已打开，操作被阻止");
                        }
                        
                        // 重置断路器
                        _failureCount = 0;
                    }
                }

                try
                {
                    var result = await operation().ConfigureAwait(false);
                    
                    lock (_lock)
                    {
                        _failureCount = 0; // 成功时重置计数
                    }
                    
                    return result;
                }
                catch (Exception)
                {
                    lock (_lock)
                    {
                        _failureCount++;
                        _lastFailureTime = DateTime.UtcNow;
                    }
                    throw;
                }
            }
        }
    }
}