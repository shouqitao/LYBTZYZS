using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Mvvm;

namespace LYBT.Desktop.Models.ViewModels
{
    /// <summary>
    /// ViewModel基类 - 简化版本
    /// 遵循"适度设计、拒绝过度工程"原则，提供核心MVVM功能
    /// </summary>
    public abstract class ViewModelBase : BindableBase, IDisposable
    {
        protected readonly ILogger Logger;
        protected readonly IEventAggregator? EventAggregator;
        
        private bool _isLoading;
        private bool _isBusy;
        private string _statusMessage = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _disposed;

        protected ViewModelBase(ILogger logger, IEventAggregator? eventAggregator = null)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            EventAggregator = eventAggregator;
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 是否繁忙
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
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
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        /// <summary>
        /// 安全执行异步操作
        /// </summary>
        protected async Task ExecuteSafelyAsync(Func<Task> operation, string? operationName = null)
        {
            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;
                StatusMessage = $"正在{operationName ?? "执行操作"}...";

                await operation();

                StatusMessage = $"{operationName ?? "操作"}完成";
            }
            catch (TaskCanceledException)
            {
                StatusMessage = $"{operationName ?? "操作"}已取消";
                Logger.LogInformation("{Operation}已取消", operationName ?? "操作");
            }
            catch (Exception ex)
            {
                ErrorMessage = GetErrorMessage(ex);
                StatusMessage = $"{operationName ?? "操作"}失败";
                Logger.LogError(ex, "执行操作时发生错误: {Operation}", operationName ?? "操作");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 安全执行有返回值的异步操作
        /// </summary>
        protected async Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> operation, string? operationName = null)
        {
            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;
                StatusMessage = $"正在{operationName ?? "执行操作"}...";

                var result = await operation();

                StatusMessage = $"{operationName ?? "操作"}完成";
                return result;
            }
            catch (TaskCanceledException)
            {
                StatusMessage = $"{operationName ?? "操作"}已取消";
                Logger.LogInformation("{Operation}已取消", operationName ?? "操作");
                return default;
            }
            catch (Exception ex)
            {
                ErrorMessage = GetErrorMessage(ex);
                StatusMessage = $"{operationName ?? "操作"}失败";
                Logger.LogError(ex, "执行操作时发生错误: {Operation}", operationName ?? "操作");
                return default;
            }
            finally
            {
                IsBusy = false;
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
        /// 设置状态消息
        /// </summary>
        protected void SetStatus(string message)
        {
            StatusMessage = message;
        }

        /// <summary>
        /// 获取友好的错误消息
        /// </summary>
        protected virtual string GetErrorMessage(Exception ex)
        {
            return ex switch
            {
                UnauthorizedAccessException => "权限不足",
                TimeoutException => "操作超时",
                TaskCanceledException => "操作已取消",
                _ => "操作失败，请重试"
            };
        }

        /// <summary>
        /// 在UI线程上执行操作
        /// </summary>
        protected void RunOnUIThread(Action action)
        {
            if (System.Windows.Application.Current?.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(action);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            if (_disposed) return;
            
            OnDisposing();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放时的清理工作 - 子类可重写
        /// </summary>
        protected virtual void OnDisposing()
        {
            // 子类实现具体的清理逻辑
        }
    }
}