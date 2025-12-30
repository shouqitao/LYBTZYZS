using System.Reactive.Disposables;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 核心ViewModel基类 - 提供最小必要功能
    /// OpenSpec: migrate-to-communitytoolkit-mvvm
    ///
    /// 设计原则:
    /// - 最小依赖: 仅需ILoggerFactory
    /// - 源生成器优先: 使用[ObservableProperty]
    /// - 单一职责: 仅提供基础状态管理
    /// </summary>
    public abstract partial class CoreViewModelBase : ObservableObject, IDisposable
    {
        #region 私有字段

        private readonly CompositeDisposable _disposables = new();
        private bool _disposed;

        #endregion

        #region 受保护属性

        /// <summary>
        /// 日志记录器
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// 日志记录器工厂
        /// </summary>
        protected ILoggerFactory LoggerFactory { get; }

        #endregion

        #region 可观察属性

        /// <summary>
        /// 是否正在执行操作
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        /// <summary>
        /// 状态消息
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// 错误消息
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string _errorMessage = string.Empty;

        #endregion

        #region 计算属性

        /// <summary>
        /// 是否未在忙碌状态
        /// </summary>
        public bool IsNotBusy => !IsBusy;

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        #endregion

        #region 构造函数

        protected CoreViewModelBase(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            Logger = loggerFactory.CreateLogger(GetType());
        }

        #endregion

        #region 属性变更回调

        /// <summary>
        /// IsBusy属性变更时调用（源生成器回调）
        /// </summary>
        partial void OnIsBusyChanged(bool value)
        {
            OnIsBusyChangedCore(value);
        }

        /// <summary>
        /// 派生类可重写以响应IsBusy变更
        /// </summary>
        protected virtual void OnIsBusyChangedCore(bool value) { }

        #endregion

        #region 状态管理

        /// <summary>
        /// 设置忙碌状态
        /// </summary>
        /// <param name="isBusy">是否忙碌</param>
        /// <param name="message">状态消息</param>
        protected void SetBusy(bool isBusy, string? message = null)
        {
            IsBusy = isBusy;
            if (!string.IsNullOrEmpty(message))
            {
                StatusMessage = message;
            }
            else if (!isBusy)
            {
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 清除错误状态
        /// </summary>
        protected void ClearError()
        {
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// 设置错误消息
        /// </summary>
        /// <param name="message">错误消息</param>
        protected void SetError(string message)
        {
            ErrorMessage = message;
            Logger.LogWarning("设置错误消息: {Message}", message);
        }

        #endregion

        #region UI线程操作

        /// <summary>
        /// 在UI线程上执行操作
        /// </summary>
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
        protected Task RunOnUIThreadAsync(Func<Task> action)
        {
            if (Application.Current?.Dispatcher == null)
            {
                return action();
            }

            return Application.Current.Dispatcher.InvokeAsync(action).Task;
        }

        #endregion

        #region Disposable管理

        /// <summary>
        /// 添加可释放对象到管理集合
        /// </summary>
        protected void AddDisposable(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _disposables.Dispose();
                OnDisposing();
            }

            _disposed = true;
        }

        /// <summary>
        /// 子类可重写以执行清理逻辑
        /// </summary>
        protected virtual void OnDisposing() { }

        #endregion
    }
}
