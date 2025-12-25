using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 轻量级ViewModel基类
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 最小化基类，仅提供INotifyPropertyChanged支持
    /// 使用CommunityToolkit.Mvvm的ObservableObject作为基类
    /// </summary>
    public abstract partial class LightViewModelBase : ObservableObject, IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// 在UI线程上执行操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        protected void RunOnUIThread(Action action)
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

        /// <summary>
        /// 在UI线程上异步执行操作
        /// </summary>
        /// <param name="action">要执行的异步操作</param>
        /// <returns>任务</returns>
        protected Task RunOnUIThreadAsync(Func<Task> action)
        {
            if (Application.Current?.Dispatcher == null)
                return action();

            return Application.Current.Dispatcher.InvokeAsync(action).Task;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                OnDisposing();
            }

            _disposed = true;
        }

        /// <summary>
        /// 子类可重写以执行清理逻辑
        /// </summary>
        protected virtual void OnDisposing() { }
    }
}
