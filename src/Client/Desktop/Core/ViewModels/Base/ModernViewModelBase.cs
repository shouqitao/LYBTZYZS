using System;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.ViewModels.Base
{
    /// <summary>
    /// 现代化ViewModel基类 - 第2阶段架构重构
    /// 合并了CoreViewModel、ServiceViewModel、ModernViewModelBase的功能
    /// 提供所有ViewModel的核心功能，避免功能重复
    /// </summary>
    public abstract class ModernViewModelBase : BindableBase, IDisposable
    {
        #region 核心依赖服务
        
        protected readonly IEventAggregator EventAggregator;
        protected readonly ILoggerFactory LoggerFactory;
        protected readonly ILogger Logger;
        protected readonly IErrorHandlingService? ErrorHandlingService;
        
        #endregion

        #region 生命周期管理
        
        private readonly CompositeDisposable _disposables = new();
        private bool _disposed = false;
        
        #endregion

        #region 状态属性
        
        private bool _isLoading = false;
        private bool _isBusy = false;
        private string _statusMessage = string.Empty;
        private bool _hasError = false;
        private string _errorMessage = string.Empty;
        
        /// <summary>
        /// 是否正在加载数据
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
        /// 是否正在执行操作（通用繁忙状态）
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
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
            protected set => SetProperty(ref _hasError, value);
        }
        
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            protected set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    HasError = !string.IsNullOrWhiteSpace(value);
                }
            }
        }
        
        #endregion

        #region 构造函数
        
        protected ModernViewModelBase(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IErrorHandlingService? errorHandlingService = null)
        {
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            Logger = loggerFactory.CreateLogger(GetType());
            ErrorHandlingService = errorHandlingService;
            
            InitializeCommands();
            SubscribeToEvents();
        }
        
        #endregion

        #region 生命周期方法
        
        /// <summary>
        /// 初始化命令 - 子类重写以添加命令
        /// </summary>
        protected virtual void InitializeCommands()
        {
            // 子类实现
        }
        
        /// <summary>
        /// 订阅事件 - 子类重写以订阅事件
        /// </summary>
        protected virtual void SubscribeToEvents()
        {
            // 子类实现
            // 所有事件订阅应该通过AddDisposable添加
        }
        
        /// <summary>
        /// 加载状态变化时触发
        /// </summary>
        protected virtual void OnLoadingStateChanged(bool isLoading)
        {
            // 子类可重写以响应加载状态变化
        }
        
        /// <summary>
        /// 触发所有命令的CanExecute检查
        /// </summary>
        protected virtual void RaiseCanExecuteChanged()
        {
            // 子类实现具体的命令刷新逻辑
        }
        
        #endregion

        #region 错误处理
        
        /// <summary>
        /// 安全执行异步操作 - 第3阶段质量优化增强
        /// </summary>
        protected async Task ExecuteSafelyAsync(
            Func<Task> operation, 
            string? operationName = null,
            bool showProgressMessage = true)
        {
            try
            {
                IsBusy = true;
                ClearError();
                
                if (showProgressMessage)
                {
                    StatusMessage = $"正在{operationName ?? "执行操作"}...";
                }
                
                await operation().ConfigureAwait(false);
                
                if (showProgressMessage)
                {
                    StatusMessage = $"{operationName ?? "操作"}完成";
                }
            }
            catch (TaskCanceledException)
            {
                StatusMessage = $"{operationName ?? "操作"}已取消";
                Logger.LogInformation("{Operation}已取消", operationName ?? "操作");
            }
            catch (Exception ex)
            {
                StatusMessage = $"{operationName ?? "操作"}失败";
                await HandleErrorAsync(ex, operationName);
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        /// <summary>
        /// 安全执行有返回值的异步操作
        /// </summary>
        protected async Task<T?> ExecuteSafelyAsync<T>(
            Func<Task<T>> operation, 
            string? operationName = null, 
            T? defaultValue = default,
            bool showProgressMessage = true)
        {
            try
            {
                IsBusy = true;
                ClearError();
                
                if (showProgressMessage)
                {
                    StatusMessage = $"正在{operationName ?? "执行操作"}...";
                }
                
                var result = await operation().ConfigureAwait(false);
                
                if (showProgressMessage)
                {
                    StatusMessage = $"{operationName ?? "操作"}完成";
                }
                
                return result;
            }
            catch (TaskCanceledException)
            {
                StatusMessage = $"{operationName ?? "操作"}已取消";
                Logger.LogInformation("{Operation}已取消", operationName ?? "操作");
                return defaultValue;
            }
            catch (Exception ex)
            {
                StatusMessage = $"{operationName ?? "操作"}失败";
                await HandleErrorAsync(ex, operationName);
                return defaultValue;
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        /// <summary>
        /// 执行操作并处理错误（向后兼容）
        /// </summary>
        [Obsolete("请使用ExecuteSafelyAsync代替")]
        protected async Task ExecuteWithErrorHandlingAsync(Func<Task> action, string? operationName = null)
        {
            await ExecuteSafelyAsync(action, operationName);
        }
        
        /// <summary>
        /// 执行操作并处理错误（同步版本）
        /// </summary>
        protected void ExecuteSafely(Action action, string? operationName = null)
        {
            try
            {
                IsBusy = true;
                ClearError();
                action();
                StatusMessage = $"{operationName ?? "操作"}完成";
            }
            catch (Exception ex)
            {
                StatusMessage = $"{operationName ?? "操作"}失败";
                HandleError(ex, operationName ?? "操作");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        /// <summary>
        /// 异步处理错误
        /// </summary>
        protected virtual async Task HandleErrorAsync(Exception ex, string? context = null)
        {
            Logger.LogError(ex, "错误发生在: {Context}", context ?? "未知操作");
            ErrorMessage = GetErrorMessage(ex);
            
            if (ErrorHandlingService != null)
            {
                await ErrorHandlingService.HandleExceptionAsync(ex, new ErrorContext 
                {
                    Operation = context ?? "未知操作",
                    Module = GetType().Namespace?.Split('.').LastOrDefault() ?? "Unknown",
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                // 降级处理：显示基本错误消息
                ShowErrorMessage(ErrorMessage);
            }
        }
        
        /// <summary>
        /// 处理错误（同步版本）
        /// </summary>
        protected virtual void HandleError(Exception ex, string? context = null)
        {
            Logger.LogError(ex, "错误发生在: {Context}", context ?? "未知操作");
            ErrorMessage = GetErrorMessage(ex);
            
            ErrorHandlingService?.HandleException(ex, new ErrorContext 
            {
                Operation = context ?? "操作",
                Module = GetType().Namespace?.Split('.').LastOrDefault() ?? "Unknown",
                Timestamp = DateTime.UtcNow
            });
        }
        
        /// <summary>
        /// 获取错误消息
        /// </summary>
        protected virtual string GetErrorMessage(Exception ex)
        {
            return ex switch
            {
                ValidationException => "输入数据验证失败",
                UnauthorizedAccessException => "权限不足",
                TimeoutException => "操作超时",
                TaskCanceledException => "操作已取消",
                _ => "操作失败，请重试"
            };
        }
        
        /// <summary>
        /// 显示错误消息（降级方案）
        /// </summary>
        protected void ShowErrorMessage(string message)
        {
            RunOnUIThread(() =>
            {
                System.Windows.MessageBox.Show(
                    message, 
                    "错误", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            });
        }
        
        /// <summary>
        /// 清除错误状态
        /// </summary>
        protected void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }
        
        #endregion

        #region 状态管理
        
        /// <summary>
        /// 设置状态消息
        /// </summary>
        protected void SetStatus(string message)
        {
            StatusMessage = message;
            Logger.LogDebug("状态更新: {Message}", message);
        }
        
        /// <summary>
        /// 清除状态消息
        /// </summary>
        protected void ClearStatus()
        {
            StatusMessage = string.Empty;
        }
        
        #endregion

        #region 资源管理
        
        /// <summary>
        /// 添加需要释放的资源
        /// </summary>
        protected void AddDisposable(IDisposable disposable)
        {
            _disposables.Add(disposable);
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
        /// 释放资源的核心实现
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            
            if (disposing)
            {
                _disposables?.Dispose();
                OnDisposing();
            }
            
            _disposed = true;
        }
        
        /// <summary>
        /// 释放时的额外清理工作
        /// </summary>
        protected virtual void OnDisposing()
        {
            // 子类可重写以添加额外的清理逻辑
        }
        
        #endregion

        #region 辅助方法
        
        /// <summary>
        /// 在UI线程上执行操作
        /// </summary>
        protected void RunOnUIThread(Action action)
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(action);
        }
        
        /// <summary>
        /// 延迟执行操作
        /// </summary>
        protected async Task DelayAsync(int milliseconds, CancellationToken cancellationToken = default)
        {
            await Task.Delay(milliseconds, cancellationToken);
        }
        
        #endregion
    }
}