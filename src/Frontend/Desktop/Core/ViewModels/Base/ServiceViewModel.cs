using System;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Events;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;

namespace LYBT.WPF.Client.Core.ViewModels.Base
{
    /// <summary>
    /// 服务调用ViewModel基类
    /// 提供API调用、错误处理服务集成、初始化等功能
    /// </summary>
    public abstract class ServiceViewModel : CoreViewModel
    {
        protected readonly IErrorHandlingService ErrorHandlingService;

        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; protected set; }

        /// <summary>
        /// 异步刷新命令
        /// </summary>
        public DelegateCommand RefreshAsyncCommand { get; protected set; }

        public ServiceViewModel(IEventAggregator eventAggregator, IErrorHandlingService errorHandlingService)
            : base(eventAggregator)
        {
            ErrorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            
            RefreshCommand = new DelegateCommand(ExecuteRefresh, CanExecuteRefresh);
            RefreshAsyncCommand = new DelegateCommand(async () => await ExecuteRefreshAsync(), CanExecuteRefresh);
        }

        /// <summary>
        /// 兼容性构造函数（用于现有代码）
        /// </summary>
        public ServiceViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
        {
            // 尝试从容器解析ErrorHandlingService
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
                ErrorHandlingService = null!;
            }

            RefreshCommand = new DelegateCommand(ExecuteRefresh, CanExecuteRefresh);
            RefreshAsyncCommand = new DelegateCommand(async () => await ExecuteRefreshAsync(), CanExecuteRefresh);
        }

        /// <summary>
        /// 初始化ViewModel
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
        /// 使用错误处理服务处理异常
        /// </summary>
        protected override void HandleError(string operation, Exception ex)
        {
            if (ErrorHandlingService != null)
            {
                var context = CreateErrorContext(operation);
                var handledError = ErrorHandlingService.HandleException(ex, context);
                ErrorMessage = handledError.UserMessage;
                
                if (handledError.Severity >= ErrorSeverity.Error)
                {
                    _ = ErrorHandlingService.ShowErrorAsync(handledError, false);
                }
            }
            else
            {
                base.HandleError(operation, ex);
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
                HandleError(operation, ex);
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

        protected override void OnLoadingStateChanged(bool isLoading)
        {
            base.OnLoadingStateChanged(isLoading);
            RefreshCommand.RaiseCanExecuteChanged();
            RefreshAsyncCommand.RaiseCanExecuteChanged();
        }

        #region 刷新命令实现

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

        #endregion
    }
}