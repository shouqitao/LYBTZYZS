namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 异步执行服务接口
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 提供安全异步执行、重试机制、UI线程调度
    /// </summary>
    public interface IAsyncExecutor
    {
        /// <summary>
        /// 安全执行异步操作（捕获并处理异常）
        /// </summary>
        /// <param name="action">异步操作</param>
        /// <param name="onError">错误处理回调</param>
        /// <returns>是否执行成功</returns>
        Task<bool> ExecuteSafelyAsync(Func<Task> action, Action<Exception>? onError = null);

        /// <summary>
        /// 安全执行异步操作并返回结果
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="action">异步操作</param>
        /// <param name="defaultValue">失败时的默认值</param>
        /// <param name="onError">错误处理回调</param>
        /// <returns>操作结果或默认值</returns>
        Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> action, T? defaultValue = default, Action<Exception>? onError = null);

        /// <summary>
        /// 带重试的异步执行
        /// </summary>
        /// <param name="action">异步操作</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <param name="retryDelay">重试间隔（毫秒）</param>
        /// <param name="shouldRetry">判断是否需要重试的条件</param>
        /// <returns>是否执行成功</returns>
        Task<bool> ExecuteWithRetryAsync(
            Func<Task> action,
            int maxRetries = 3,
            int retryDelay = 1000,
            Func<Exception, bool>? shouldRetry = null);

        /// <summary>
        /// 带重试的异步执行并返回结果
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="action">异步操作</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <param name="retryDelay">重试间隔（毫秒）</param>
        /// <param name="shouldRetry">判断是否需要重试的条件</param>
        /// <returns>操作结果</returns>
        Task<T?> ExecuteWithRetryAsync<T>(
            Func<Task<T>> action,
            int maxRetries = 3,
            int retryDelay = 1000,
            Func<Exception, bool>? shouldRetry = null);

        /// <summary>
        /// 在UI线程执行操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        void ExecuteOnUIThread(Action action);

        /// <summary>
        /// 在UI线程异步执行操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        Task ExecuteOnUIThreadAsync(Action action);

        /// <summary>
        /// 带超时的异步执行
        /// </summary>
        /// <param name="action">异步操作</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否在超时前完成</returns>
        Task<bool> ExecuteWithTimeoutAsync(
            Func<CancellationToken, Task> action,
            TimeSpan timeout,
            CancellationToken cancellationToken = default);
    }
}
