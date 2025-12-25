using System.Windows;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 异步执行服务实现
    /// OpenSpec: refactor-viewmodel-composition
    /// </summary>
    public class AsyncExecutor : IAsyncExecutor
    {
        private readonly ILogger<AsyncExecutor>? _logger;

        public AsyncExecutor(ILogger<AsyncExecutor>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<bool> ExecuteSafelyAsync(Func<Task> action, Action<Exception>? onError = null)
        {
            try
            {
                await action();
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Safe execution failed");
                onError?.Invoke(ex);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> action, T? defaultValue = default, Action<Exception>? onError = null)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Safe execution failed");
                onError?.Invoke(ex);
                return defaultValue;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExecuteWithRetryAsync(
            Func<Task> action,
            int maxRetries = 3,
            int retryDelay = 1000,
            Func<Exception, bool>? shouldRetry = null)
        {
            shouldRetry ??= _ => true;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await action();
                    return true;
                }
                catch (Exception ex) when (attempt < maxRetries && shouldRetry(ex))
                {
                    _logger?.LogWarning(ex, "Retry attempt {Attempt}/{MaxRetries}", attempt + 1, maxRetries);
                    await Task.Delay(retryDelay * (attempt + 1)); // 递增延迟
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "All retry attempts failed");
                    return false;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public async Task<T?> ExecuteWithRetryAsync<T>(
            Func<Task<T>> action,
            int maxRetries = 3,
            int retryDelay = 1000,
            Func<Exception, bool>? shouldRetry = null)
        {
            shouldRetry ??= _ => true;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex) when (attempt < maxRetries && shouldRetry(ex))
                {
                    _logger?.LogWarning(ex, "Retry attempt {Attempt}/{MaxRetries}", attempt + 1, maxRetries);
                    await Task.Delay(retryDelay * (attempt + 1));
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "All retry attempts failed");
                    return default;
                }
            }

            return default;
        }

        /// <inheritdoc/>
        public void ExecuteOnUIThread(Action action)
        {
            if (Application.Current?.Dispatcher == null)
            {
                action();
                return;
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Application.Current.Dispatcher.Invoke(action);
            }
        }

        /// <inheritdoc/>
        public Task ExecuteOnUIThreadAsync(Action action)
        {
            if (Application.Current?.Dispatcher == null)
            {
                action();
                return Task.CompletedTask;
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }

            return Application.Current.Dispatcher.InvokeAsync(action).Task;
        }

        /// <inheritdoc/>
        public async Task<bool> ExecuteWithTimeoutAsync(
            Func<CancellationToken, Task> action,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                await action(cts.Token);
                return true;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger?.LogWarning("Operation timed out after {Timeout}", timeout);
                return false;
            }
        }
    }
}
