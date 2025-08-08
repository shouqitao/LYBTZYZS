using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Events;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;

namespace LYBT.WPF.Client.Core.ViewModels
{
    /// <summary>
    /// 基础ViewModel，提供通用功能和规范化实现
    /// </summary>
    public abstract class BaseViewModel : BindableBase, IDisposable
    {
        protected readonly IEventAggregator EventAggregator;
        protected readonly IErrorHandlingService ErrorHandlingService;
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
        /// 异步刷新命令
        /// </summary>
        public DelegateCommand RefreshAsyncCommand { get; protected set; }

        /// <summary>
        /// 清除错误命令
        /// </summary>
        public DelegateCommand ClearErrorCommand { get; protected set; }

        protected BaseViewModel(IEventAggregator eventAggregator, IErrorHandlingService errorHandlingService)
        {
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            ErrorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));

            RefreshCommand = new DelegateCommand(ExecuteRefresh, CanExecuteRefresh);
            RefreshAsyncCommand = new DelegateCommand(async () => await ExecuteRefreshAsync(), CanExecuteRefresh);
            ClearErrorCommand = new DelegateCommand(ExecuteClearError, CanExecuteClearError);
        }

        /// <summary>
        /// 兼容性构造函数，用于现有代码
        /// 注意：推荐使用包含IErrorHandlingService的构造函数以获得完整功能
        /// </summary>
        protected BaseViewModel(IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            
            // 尝试从当前应用程序的服务容器获取ErrorHandlingService
            try
            {
                if (Prism.Ioc.ContainerLocator.Container != null)
                {
                    ErrorHandlingService = Prism.Ioc.ContainerLocator.Container.Resolve<IErrorHandlingService>();
                }
                else
                {
                    ErrorHandlingService = null!;
                }
            }
            catch
            {
                // 如果无法解析服务，将使用兼容性处理方式
                ErrorHandlingService = null!;
            }

            RefreshCommand = new DelegateCommand(ExecuteRefresh, CanExecuteRefresh);
            RefreshAsyncCommand = new DelegateCommand(async () => await ExecuteRefreshAsync(), CanExecuteRefresh);
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
        protected void HandleApiResponse<T>(ServiceResult<T> response, string? successMessage = null)
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
                ErrorMessage = response.ErrorMessage ?? "操作失败";
            }
        }

        /// <summary>
        /// 处理异常
        /// </summary>
        protected void HandleError(string operation, Exception ex)
        {
            if (ErrorHandlingService != null)
            {
                var context = CreateErrorContext(operation);
                var handledError = ErrorHandlingService.HandleException(ex, context);
                ErrorMessage = handledError.UserMessage;
                
                // 异步显示详细错误（如果需要）
                if (handledError.Severity >= ErrorSeverity.Error)
                {
                    _ = ErrorHandlingService.ShowErrorAsync(handledError, false); // 不显示对话框，只记录日志
                }
            }
            else
            {
                // 兼容性处理
                ErrorMessage = $"{operation}: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] {operation} 异常: {ex}");
            }
        }

        /// <summary>
        /// 异步处理异常
        /// </summary>
        protected async Task HandleErrorAsync(string operation, Exception ex, bool showDialog = true)
        {
            if (ErrorHandlingService != null)
            {
                var context = CreateErrorContext(operation);
                var handledError = await ErrorHandlingService.HandleExceptionAsync(ex, context);
                ErrorMessage = handledError.UserMessage;
                
                if (showDialog && handledError.RequiresUserAcknowledgment)
                {
                    await ErrorHandlingService.ShowErrorAsync(handledError);
                }
            }
            else
            {
                // 兼容性处理
                HandleError(operation, ex);
            }
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

        /// <summary>
        /// 安全执行异步操作，自动处理异常和加载状态
        /// </summary>
        protected async Task ExecuteAsync(Func<Task> operation, string? operationName = null, bool showErrorDialog = true)
        {
            try
            {
                IsLoading = true;
                ClearError();
                await operation();
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(operationName ?? "操作", ex, showErrorDialog);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 安全执行异步操作并返回结果
        /// </summary>
        protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string? operationName = null, bool showErrorDialog = true)
        {
            try
            {
                IsLoading = true;
                ClearError();
                return await operation();
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(operationName ?? "操作", ex, showErrorDialog);
                return default;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 使用错误处理服务安全执行操作
        /// </summary>
        protected async Task<bool> ExecuteSafelyAsync(Func<Task> operation, string? operationName = null, bool showErrorDialog = true)
        {
            if (ErrorHandlingService != null)
            {
                var context = CreateErrorContext(operationName ?? "操作");
                return await ErrorHandlingService.ExecuteSafelyAsync(operation, context, showErrorDialog);
            }
            else
            {
                // 兼容性处理
                try
                {
                    await operation();
                    return true;
                }
                catch (Exception ex)
                {
                    HandleError(operationName ?? "操作", ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// 使用错误处理服务安全执行操作并返回结果
        /// </summary>
        protected async Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> operation, string? operationName = null, bool showErrorDialog = true)
        {
            if (ErrorHandlingService != null)
            {
                var context = CreateErrorContext(operationName ?? "操作");
                return await ErrorHandlingService.ExecuteSafelyAsync(operation, context, showErrorDialog);
            }
            else
            {
                // 兼容性处理
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    HandleError(operationName ?? "操作", ex);
                    return default;
                }
            }
        }

        /// <summary>
        /// 创建异步命令
        /// </summary>
        protected DelegateCommand CreateAsyncCommand(Func<Task> executeMethod, Func<bool>? canExecuteMethod = null)
        {
            return new DelegateCommand(
                async () => await ExecuteAsync(executeMethod),
                canExecuteMethod ?? (() => true)
            );
        }

        /// <summary>
        /// 创建带参数的异步命令
        /// </summary>
        protected DelegateCommand<T> CreateAsyncCommand<T>(Func<T, Task> executeMethod, Func<T, bool>? canExecuteMethod = null)
        {
            return new DelegateCommand<T>(
                async param => await ExecuteAsync(() => executeMethod(param)),
                canExecuteMethod ?? (param => true)
            );
        }

        /// <summary>
        /// 创建错误上下文
        /// </summary>
        protected virtual ErrorContext CreateErrorContext(string operationName)
        {
            var context = new ErrorContext
            {
                OperationName = operationName,
                ModuleName = GetType().Namespace?.Split('.').LastOrDefault() ?? "Unknown",
                ViewName = GetType().Name.Replace("ViewModel", "")
            };

            // 可以在子类中重写此方法添加更多上下文信息
            OnCreateErrorContext(context);

            return context;
        }

        /// <summary>
        /// 子类可重写此方法添加特定的错误上下文信息
        /// </summary>
        protected virtual void OnCreateErrorContext(ErrorContext context)
        {
            // 子类可以重写此方法添加特定信息
        }

        #region 命令实现

        protected virtual void ExecuteRefresh()
        {
            _ = ExecuteRefreshAsync();
        }

        protected virtual async Task ExecuteRefreshAsync()
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