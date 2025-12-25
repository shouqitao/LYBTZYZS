using System.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 加载状态管理服务接口
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 提供加载状态、繁忙状态的统一管理，支持嵌套加载计数和线程安全
    /// </summary>
    public interface ILoadingStateManager : INotifyPropertyChanged
    {
        /// <summary>是否正在加载</summary>
        bool IsLoading { get; set; }

        /// <summary>是否繁忙（用于UI禁用）</summary>
        bool IsBusy { get; set; }

        /// <summary>繁忙消息</summary>
        string BusyMessage { get; set; }

        /// <summary>嵌套加载计数</summary>
        int LoadingCount { get; }

        /// <summary>
        /// 在加载状态下执行异步操作
        /// </summary>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="message">加载消息</param>
        /// <param name="isBusy">是否同时设置繁忙状态</param>
        Task ExecuteWithLoadingAsync(Func<Task> action, string? message = null, bool isBusy = false);

        /// <summary>
        /// 在加载状态下执行异步操作并返回结果
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="message">加载消息</param>
        /// <param name="isBusy">是否同时设置繁忙状态</param>
        /// <returns>操作结果</returns>
        Task<T> ExecuteWithLoadingAsync<T>(Func<Task<T>> action, string? message = null, bool isBusy = false);

        /// <summary>开始加载</summary>
        /// <param name="message">加载消息</param>
        void BeginLoading(string? message = null);

        /// <summary>结束加载</summary>
        void EndLoading();

        /// <summary>重置所有状态</summary>
        void Reset();
    }
}
