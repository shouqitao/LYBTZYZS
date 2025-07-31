using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Events;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Common;

namespace LYBT.WPF.Client.Core.ViewModels
{
    /// <summary>
    /// 基础ViewModel，提供通用功能和规范化实现
    /// </summary>
    public abstract class BaseViewModel : BindableBase, IDisposable
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
                SetProperty(ref _isLoading, value);
                OnLoadingStateChanged(value);
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
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; protected set; }

        /// <summary>
        /// 清除错误命令
        /// </summary>
        public DelegateCommand ClearErrorCommand { get; protected set; }

        protected BaseViewModel(IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            
            RefreshCommand = new DelegateCommand(ExecuteRefresh, CanExecuteRefresh);
            ClearErrorCommand = new DelegateCommand(ExecuteClearError, CanExecuteClearError);
        }

        /// <summary>
        /// 初始化ViewModel，在子类中重写此方法执行初始化逻辑
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                await OnInitializeAsync();
            }
            catch (Exception ex)
            {
                HandleError("初始化失败", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 子类重写此方法实现具体的初始化逻辑
        /// </summary>
        protected virtual Task OnInitializeAsync() => Task.CompletedTask;

        /// <summary>
        /// 加载状态改变时调用
        /// </summary>
        protected virtual void OnLoadingStateChanged(bool isLoading)
        {
            RefreshCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 处理API响应
        /// </summary>
        protected void HandleApiResponse<T>(ApiResponse<T> response, string? successMessage = null)
        {
            if (response.IsSuccess)
            {
                ClearError();
                if (!string.IsNullOrEmpty(successMessage))
                {
                    StatusMessage = successMessage;
                }
            }
            else
            {
                ErrorMessage = response.Message ?? "操作失败";
            }
        }

        /// <summary>
        /// 处理异常
        /// </summary>
        protected void HandleError(string operation, Exception ex)
        {
            ErrorMessage = $"{operation}: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] {operation} 异常: {ex}");
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
        /// 显示确认对话框
        /// </summary>
        protected bool ShowConfirmDialog(string message, string title = "确认")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        /// <summary>
        /// 显示信息对话框
        /// </summary>
        protected void ShowInfoDialog(string message, string title = "信息")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        protected void ShowErrorDialog(string message, string title = "错误")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #region 命令实现

        protected virtual async void ExecuteRefresh()
        {
            await InitializeAsync();
        }

        protected virtual bool CanExecuteRefresh()
        {
            return !IsLoading;
        }

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
                    // 释放托管资源
                    OnDisposing();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// 子类重写此方法实现自定义的资源释放逻辑
        /// </summary>
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