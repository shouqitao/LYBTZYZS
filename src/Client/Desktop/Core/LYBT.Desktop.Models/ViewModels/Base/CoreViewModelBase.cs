using System.Reactive.Disposables;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Events;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 核心ViewModel基类 - 提供最小必要功能
    /// OpenSpec: enhance-viewmodel-architecture
    ///
    /// 设计原则:
    /// - 服务聚合: 使用IViewModelServices简化构造函数
    /// - 源生成器优先: 使用[ObservableProperty]
    /// - 单一职责: 仅提供基础状态管理
    /// </summary>
    public abstract partial class CoreViewModelBase : ObservableObject, IDisposable
    {
        #region 私有字段

        private readonly CompositeDisposable _disposables = new();
        private EventSubscriptionManager? _eventManager;
        private bool _disposed;

        #endregion

        #region 受保护属性

        /// <summary>
        /// ViewModel服务聚合
        /// OpenSpec: enhance-viewmodel-architecture
        /// </summary>
        protected IViewModelServices Services { get; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// 日志记录器工厂
        /// </summary>
        protected ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Prism事件聚合器
        /// </summary>
        protected IEventAggregator EventAggregator { get; }

        /// <summary>
        /// 事件订阅管理器 (延迟初始化)
        /// 使用此属性订阅事件，Dispose时自动清理
        /// </summary>
        protected EventSubscriptionManager Events => _eventManager ??= new EventSubscriptionManager(EventAggregator);

        /// <summary>
        /// UI线程调度器
        /// </summary>
        protected IUiThreadDispatcher UiDispatcher => Services.UiThreadDispatcher;

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

        /// <summary>
        /// 构造函数 - 使用IViewModelServices聚合服务
        /// OpenSpec: enhance-viewmodel-architecture
        /// </summary>
        /// <param name="services">ViewModel服务聚合</param>
        protected CoreViewModelBase(IViewModelServices services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            LoggerFactory = services.LoggerFactory;
            EventAggregator = services.EventAggregator;
            Logger = services.LoggerFactory.CreateLogger(GetType());
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

        #region 异步执行包装

        /// <summary>
        /// 异步执行包装 - 统一异常处理
        /// </summary>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="operationName">操作名称（用于日志和错误消息）</param>
        /// <param name="showBusy">是否显示忙碌状态</param>
        /// <param name="showErrorToUser">是否向用户显示错误消息</param>
        protected async Task ExecuteWithErrorHandlingAsync(
            Func<Task> action,
            string operationName,
            bool showBusy = true,
            bool showErrorToUser = true)
        {
            try
            {
                if (showBusy) SetBusy(true, $"正在{operationName}...");
                ClearError();
                await action();
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("{Operation} 已取消", operationName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{Operation} 失败", operationName);
                if (showErrorToUser)
                {
                    SetError($"{operationName}失败: {ex.Message}");
                }
            }
            finally
            {
                if (showBusy) SetBusy(false);
            }
        }

        /// <summary>
        /// 异步执行包装 (带返回值)
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="defaultValue">失败时的默认值</param>
        /// <param name="showBusy">是否显示忙碌状态</param>
        /// <param name="showErrorToUser">是否向用户显示错误消息</param>
        /// <returns>操作结果或默认值</returns>
        protected async Task<T?> ExecuteWithErrorHandlingAsync<T>(
            Func<Task<T>> action,
            string operationName,
            T? defaultValue = default,
            bool showBusy = true,
            bool showErrorToUser = true)
        {
            try
            {
                if (showBusy) SetBusy(true, $"正在{operationName}...");
                ClearError();
                return await action();
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("{Operation} 已取消", operationName);
                return defaultValue;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{Operation} 失败", operationName);
                if (showErrorToUser)
                {
                    SetError($"{operationName}失败: {ex.Message}");
                }
                return defaultValue;
            }
            finally
            {
                if (showBusy) SetBusy(false);
            }
        }

        #endregion

        #region UI线程操作

        /// <summary>
        /// 在UI线程上执行操作
        /// </summary>
        protected void RunOnUIThread(Action action)
        {
            UiDispatcher.Invoke(action);
        }

        /// <summary>
        /// 在UI线程上异步执行操作
        /// </summary>
        protected Task RunOnUIThreadAsync(Func<Task> action)
        {
            return UiDispatcher.InvokeAsync(action);
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
                _eventManager?.Dispose();
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
