using LYBT.Shared.Models.Contracts.Common;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Events;

namespace LYBT.Desktop.Core.ViewModels.Base
{
    /// <summary>
    /// 核心ViewModel基类
    /// 提供最基础的通用功能：加载状态、状态消息、错误处理
    /// </summary>
    public abstract class CoreViewModel : BindableBase, IDisposable
    {
        protected readonly IEventAggregator EventAggregator;
        private bool _isLoading;
        private string _statusMessage = string.Empty;
        private bool _hasError;
        private string _errorMessage = string.Empty;
        private bool _disposed;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnLoadingStateChanged(value);
                    RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                HasError = !string.IsNullOrEmpty(value);
            }
        }

        /// <summary>
        /// 清除错误命令
        /// </summary>
        public DelegateCommand ClearErrorCommand { get; }

        protected CoreViewModel(IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            ClearErrorCommand = new DelegateCommand(ExecuteClearError, CanExecuteClearError);
        }

        /// <summary>
        /// 加载状态改变时调用
        /// </summary>
        protected virtual void OnLoadingStateChanged(bool isLoading)
        {
            // 子类可以重写此方法
        }

        /// <summary>
        /// 所有Command的CanExecute状态更新 - 子类可重写
        /// </summary>
        protected virtual void RaiseCanExecuteChanged()
        {
            ClearErrorCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 清除错误状态
        /// </summary>
        protected void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        /// <summary>
        /// 设置状态消息
        /// </summary>
        protected void SetStatus(string message)
        {
            StatusMessage = message;
        }

        /// <summary>
        /// 清除状态消息
        /// </summary>
        protected void ClearStatus()
        {
            StatusMessage = string.Empty;
        }

        /// <summary>
        /// 处理基础异常
        /// </summary>
        protected virtual void HandleError(string operation, Exception ex)
        {
            ErrorMessage = $"{operation}: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] {operation} 异常: {ex}");
        }

        /// <summary>
        /// 安全执行异步操作
        /// </summary>
        protected async Task ExecuteAsync(Func<Task> operation, string? operationName = null)
        {
            try
            {
                IsLoading = true;
                ClearError();
                await operation();
            }
            catch (Exception ex)
            {
                HandleError(operationName ?? "操作", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #region 命令实现

        protected virtual void ExecuteClearError()
        {
            ClearError();
            ClearStatus();
        }

        protected virtual bool CanExecuteClearError()
        {
            return HasError;
        }

        #endregion

        #region IDisposable 实现

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    OnDisposing();
                }
                _disposed = true;
            }
        }

        protected virtual void OnDisposing()
        {
            // 子类可以重写此方法进行清理
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}