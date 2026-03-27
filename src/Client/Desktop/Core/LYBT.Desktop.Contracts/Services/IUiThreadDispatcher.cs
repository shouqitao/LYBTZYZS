using System.Windows.Threading;

namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// UI线程调度抽象接口
    /// 解耦ViewModel对WPF Dispatcher的直接依赖，提升可测试性
    /// </summary>
    public interface IUiThreadDispatcher
    {
        /// <summary>
        /// 同步执行操作到UI线程
        /// 如果已在UI线程则直接执行
        /// </summary>
        void Invoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal);

        /// <summary>
        /// 同步执行函数到UI线程并返回结果
        /// </summary>
        T Invoke<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal);

        /// <summary>
        /// 异步执行操作到UI线程，返回可等待的Task
        /// </summary>
        Task InvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal);

        /// <summary>
        /// 异步执行函数到UI线程，返回带结果的Task
        /// </summary>
        Task<T> InvokeAsync<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal);

        /// <summary>
        /// 异步调度操作到UI线程（Fire-and-Forget）
        /// 不等待完成
        /// </summary>
        void BeginInvoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal);

        /// <summary>
        /// 检查当前线程是否为UI线程
        /// </summary>
        bool CheckAccess();
    }
}
