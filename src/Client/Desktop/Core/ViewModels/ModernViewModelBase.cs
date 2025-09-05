using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels
{
    /// <summary>
    /// UltraThink Phase 3.1: 现代化ViewModel基类
    /// 
    /// 核心特性:
    /// 1. 统一Command管理（未来集成Source Generator）
    /// 2. 统一错误处理和加载状态
    /// 3. 统一事件聚合器集成
    /// 4. 零DelegateCommand CS8618警告
    /// </summary>
    public abstract class ModernViewModelBase : BindableBase, IDisposable
    {
        #region 核心依赖服务

        protected readonly IEventAggregator EventAggregator;
        protected readonly IErrorHandlingService? ErrorHandlingService;

        #endregion

        #region 基础状态属性

        private bool _isLoading = false;
        private string _statusMessage = string.Empty;
        private bool _hasError = false;
        private string _errorMessage = string.Empty;
        private bool _disposed = false;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            protected set
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
            protected set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            private set => SetProperty(ref _hasError, value);
        }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    HasError = !string.IsNullOrEmpty(value);
                }
            }
        }

        #endregion

        #region 统一Command属性 (零警告)

        /// <summary>
        /// 清除错误命令 - 所有ViewModel通用
        /// </summary>
        public DelegateCommand ClearErrorCommand { get; }

        /// <summary>
        /// 刷新命令 - 大多数ViewModel通用
        /// </summary>
        public DelegateCommand RefreshCommand { get; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 标准构造函数（推荐）
        /// </summary>
        protected ModernViewModelBase(
            IEventAggregator eventAggregator,
            IErrorHandlingService? errorHandlingService = null)
        {
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            ErrorHandlingService = errorHandlingService;

            // 统一初始化Command（零警告）
            ClearErrorCommand = new DelegateCommand(ExecuteClearError, CanExecuteClearError);
            RefreshCommand = new DelegateCommand(async () => await ExecuteRefreshAsync(), CanExecuteRefresh);
        }

        /// <summary>
        /// 兼容性构造函数（向后兼容）
        /// </summary>
        protected ModernViewModelBase(IEventAggregator eventAggregator)
            : this(eventAggregator, TryResolveErrorHandlingService())
        {
        }

        /// <summary>
        /// 简化构造函数（使用容器解析）
        /// </summary>
        protected ModernViewModelBase()
            : this(GetEventAggregatorFromContainer(), TryResolveErrorHandlingService())
        {
        }

        #endregion

        #region 核心虚方法（子类重写）

        /// <summary>
        /// 初始化异步方法 - 子类重写实现具体逻辑
        /// </summary>
        protected virtual Task OnInitializeAsync() => Task.CompletedTask;

        /// <summary>
        /// 刷新异步方法 - 子类重写实现具体逻辑
        /// </summary>
        protected virtual Task OnRefreshAsync() => OnInitializeAsync();

        /// <summary>
        /// 加载状态改变回调 - 子类可重写
        /// </summary>
        protected virtual void OnLoadingStateChanged(bool isLoading)
        {
            // 默认实现：无操作，子类可重写
        }

        /// <summary>
        /// 所有Command的CanExecute状态更新 - 子类可重写
        /// </summary>
        protected virtual void RaiseCanExecuteChanged()
        {
            ClearErrorCommand.RaiseCanExecuteChanged();
            RefreshCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 统一错误处理

        /// <summary>
        /// 设置错误消息
        /// </summary>
        protected void SetError(string message)
        {
            ErrorMessage = message;
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
        /// 清除状态消息
        /// </summary>
        protected void ClearStatus()
        {
            StatusMessage = string.Empty;
        }

        /// <summary>
        /// 处理异常 - 统一错误处理
        /// </summary>
        protected virtual async Task HandleErrorAsync(string operation, Exception ex, bool showDialog = true)
        {
            if (ErrorHandlingService != null)
            {
                try
                {
                    var context = new ErrorContext
                    {
                        OperationName = operation,
                        ModuleName = GetType().Namespace?.Split('.').LastOrDefault() ?? "Unknown",
                        ViewName = GetType().Name.Replace("ViewModel", "")
                    };

                    var handledError = await ErrorHandlingService.HandleExceptionAsync(ex, context);
                    ErrorMessage = handledError.UserMessage;

                    if (showDialog && handledError.RequiresUserAcknowledgment)
                    {
                        await ErrorHandlingService.ShowErrorAsync(handledError);
                    }
                }
                catch (Exception handlingEx)
                {
                    // 错误处理服务本身异常时的fallback
                    ErrorMessage = $"{operation}失败: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] 错误处理服务异常: {handlingEx}");
                }
            }
            else
            {
                // Fallback: 基础错误处理
                ErrorMessage = $"{operation}失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] {operation} 异常: {ex}");
            }
        }

        #endregion

        #region 统一异步执行器

        /// <summary>
        /// 安全执行异步操作（带统一错误处理）
        /// </summary>
        protected async Task<T?> ExecuteAsync<T>(
            Func<Task<T>> operation,
            string? operationName = null,
            bool showErrorDialog = true)
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
        /// 安全执行异步操作（无返回值）
        /// </summary>
        protected async Task<bool> ExecuteAsync(
            Func<Task> operation,
            string? operationName = null,
            bool showErrorDialog = true)
        {
            try
            {
                IsLoading = true;
                ClearError();
                await operation();
                return true;
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(operationName ?? "操作", ex, showErrorDialog);
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Command实现

        /// <summary>
        /// 清除错误命令实现
        /// </summary>
        private void ExecuteClearError()
        {
            ClearError();
            ClearStatus();
        }

        /// <summary>
        /// 清除错误命令CanExecute
        /// </summary>
        private bool CanExecuteClearError()
        {
            return HasError;
        }

        /// <summary>
        /// 刷新命令实现
        /// </summary>
        private async Task ExecuteRefreshAsync()
        {
            await ExecuteAsync(OnRefreshAsync, "刷新数据");
        }

        /// <summary>
        /// 刷新命令CanExecute
        /// </summary>
        private bool CanExecuteRefresh()
        {
            return !IsLoading;
        }

        #endregion

        #region 依赖服务解析（兼容性）

        /// <summary>
        /// 从容器解析EventAggregator
        /// </summary>
        private static IEventAggregator GetEventAggregatorFromContainer()
        {
            try
            {
                return (IEventAggregator?)Prism.Ioc.ContainerLocator.Container?.Resolve(typeof(IEventAggregator))
                    ?? new EventAggregator();
            }
            catch
            {
                return new EventAggregator();
            }
        }

        /// <summary>
        /// 尝试从容器解析ErrorHandlingService
        /// </summary>
        private static IErrorHandlingService? TryResolveErrorHandlingService()
        {
            try
            {
                return (IErrorHandlingService?)Prism.Ioc.ContainerLocator.Container?.Resolve(typeof(IErrorHandlingService));
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region IDisposable实现

        protected virtual void OnDisposing()
        {
            // 子类可重写进行清理
        }

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
