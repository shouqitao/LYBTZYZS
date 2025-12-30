using System.Reactive.Disposables;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 对话框ViewModel轻量级基类
    /// OpenSpec: migrate-to-communitytoolkit-mvvm
    ///
    /// 使用CommunityToolkit.Mvvm源生成器:
    /// - [ObservableProperty] 替代 SetProperty
    /// - [RelayCommand] 替代 DelegateCommand
    ///
    /// 专为IDialogAware对话框设计，不包含导航功能
    /// </summary>
    public abstract partial class DialogViewModelBase : ObservableObject, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private bool _disposed;

        #region 服务

        /// <summary>
        /// 日志记录器
        /// </summary>
        protected ILogger Logger { get; }

        #endregion

        #region 可观察属性

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotLoading))]
        private bool _isLoading;

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

        /// <summary>
        /// 是否未在加载
        /// </summary>
        public bool IsNotLoading => !IsLoading;

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        #endregion

        #region 构造函数

        protected DialogViewModelBase(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory?.CreateLogger(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        #endregion

        #region 属性变更回调

        /// <summary>
        /// IsLoading属性变更时调用（源生成器回调）
        /// </summary>
        partial void OnIsLoadingChanged(bool value)
        {
            OnIsLoadingChangedCore(value);
        }

        /// <summary>
        /// 派生类可重写以响应IsLoading变更
        /// </summary>
        protected virtual void OnIsLoadingChangedCore(bool value) { }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 清除错误
        /// </summary>
        protected void ClearError()
        {
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// 添加可释放对象
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
