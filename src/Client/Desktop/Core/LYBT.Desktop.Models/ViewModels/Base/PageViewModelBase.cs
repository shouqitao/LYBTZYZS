using System.Net;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Http;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Logging;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 页面ViewModel基类 - 提供API调用和HTTP错误处理功能
    /// OpenSpec: standardize-viewmodel-framework
    ///
    /// 继承自NavigableViewModelBase，添加:
    /// - API服务访问
    /// - 刷新命令
    /// - 页面描述
    /// - HTTP错误处理（401/409/5xx等）
    /// - 安全执行包装
    /// </summary>
    public abstract partial class PageViewModelBase : NavigableViewModelBase
    {
        #region 依赖服务

        protected readonly IApiService ApiService;

        #endregion

        #region 可观察属性

        /// <summary>
        /// 页面描述
        /// </summary>
        [ObservableProperty]
        private string _pageDescription = string.Empty;

        #endregion

        #region 构造函数

        protected PageViewModelBase(
            ILoggerFactory loggerFactory,
            IEventAggregator eventAggregator,
            IRegionManager regionManager,
            IApiService apiService,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null,
            ICommonDialogService? commonDialogService = null)
            : base(loggerFactory, eventAggregator, regionManager, sessionManager, userNotificationService, commonDialogService)
        {
            ApiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        #endregion

        #region 刷新命令

        /// <summary>
        /// 刷新页面数据命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRefresh))]
        protected virtual async Task RefreshAsync()
        {
            await ExecuteWithErrorHandlingAsync(
                OnRefreshAsync,
                "刷新数据");
        }

        /// <summary>
        /// 是否可以执行刷新
        /// </summary>
        protected virtual bool CanRefresh() => !IsBusy;

        /// <summary>
        /// 刷新数据的实际逻辑（子类重写）
        /// </summary>
        protected virtual Task OnRefreshAsync() => Task.CompletedTask;

        #endregion

        #region 导航重写

        /// <summary>
        /// 导航到此页面时调用
        /// </summary>
        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            try
            {
                ProcessNavigationParameters(navigationContext.Parameters);

                // 使用基类的 InitializeAsync，这里不重复调用
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "页面导航处理失败");
                _ = HandleExceptionAsync(ex, "页面加载");
            }
        }

        /// <summary>
        /// 处理导航参数（子类重写）
        /// </summary>
        protected virtual void ProcessNavigationParameters(NavigationParameters parameters) { }

        /// <summary>
        /// 页面初始化（子类重写）
        /// </summary>
        protected virtual Task InitializeAsync(NavigationParameters parameters) => Task.CompletedTask;

        /// <summary>
        /// 首次导航时的初始化
        /// </summary>
        protected override async Task InitializeAsync(NavigationContext context)
        {
            await InitializeAsync(context.Parameters);
        }

        #endregion

        #region 安全执行方法

        /// <summary>
        /// 安全执行异步操作（带HTTP状态码处理）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="fallbackValue">失败时的回退值</param>
        /// <param name="callerName">调用方法名（自动填充）</param>
        /// <returns>操作结果或回退值</returns>
        protected async Task<T?> SafeExecuteAsync<T>(
            Func<Task<T>> action,
            string? operationName = null,
            T? fallbackValue = default,
            [CallerMemberName] string? callerName = null)
        {
            var opName = operationName ?? callerName ?? "操作";

            try
            {
                IsBusy = true;
                ClearError();
                return await action().ConfigureAwait(false);
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorizedAsync(opName);
                return fallbackValue;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                await HandleConflictAsync(opName);
                return fallbackValue;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.GatewayTimeout ||
                                          ex.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                await HandleServiceUnavailableAsync(opName);
                return fallbackValue;
            }
            catch (ApiException ex)
            {
                await HandleApiExceptionAsync(ex, opName);
                return fallbackValue;
            }
            catch (TaskCanceledException)
            {
                Logger.LogInformation("{Operation}已取消", opName);
                StatusMessage = $"{opName}已取消";
                return fallbackValue;
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, opName);
                return fallbackValue;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 安全执行无返回值的异步操作
        /// </summary>
        protected async Task<bool> SafeExecuteAsync(
            Func<Task> action,
            string? operationName = null,
            [CallerMemberName] string? callerName = null)
        {
            var result = await SafeExecuteAsync(async () =>
            {
                await action().ConfigureAwait(false);
                return true;
            }, operationName, false, callerName);

            return result;
        }

        #endregion

        #region HTTP错误处理

        /// <summary>
        /// 处理401未授权响应
        /// </summary>
        protected virtual async Task HandleUnauthorizedAsync(string operationName)
        {
            Logger.LogWarning("会话已过期或未授权: {Operation}", operationName);
            ErrorMessage = "登录已过期，请重新登录";

            await RunOnUIThreadAsync(() =>
            {
                SessionManager?.ClearSession();
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// 处理409冲突响应
        /// </summary>
        protected virtual async Task HandleConflictAsync(string operationName)
        {
            Logger.LogWarning("数据冲突: {Operation}", operationName);

            var shouldRefresh = await ShowConfirmMessageAsync(
                "数据已被其他用户修改，是否刷新获取最新数据？",
                "数据冲突");

            if (shouldRefresh)
            {
                await OnConflictRefreshRequestedAsync();
            }

            ErrorMessage = "数据已被修改，请刷新后重试";
        }

        /// <summary>
        /// 处理服务不可用响应
        /// </summary>
        protected virtual async Task HandleServiceUnavailableAsync(string operationName)
        {
            Logger.LogWarning("服务暂时不可用: {Operation}", operationName);
            ErrorMessage = "服务暂时不可用，请稍后重试";
            await ShowErrorMessageAsync("服务暂时不可用，请稍后重试");
        }

        /// <summary>
        /// 处理API异常
        /// </summary>
        protected virtual async Task HandleApiExceptionAsync(ApiException ex, string operationName)
        {
            var correlationId = TraceContext.TraceIdOrNew;
            var trackingCode = ClientErrorMessageMapper.GetShortTrackingCode();

            Logger.LogWarning(ex, "API请求失败: {Operation}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                operationName, ex.StatusCode, correlationId);

            var userMessage = ClientErrorMessageMapper.GetUserMessageFromStatusCode((int)ex.StatusCode);
            var messageWithTracking = $"{userMessage} (追踪码: {trackingCode})";

            ErrorMessage = messageWithTracking;
            StatusMessage = $"{operationName}失败";

            await ShowErrorMessageAsync(messageWithTracking);
        }

        /// <summary>
        /// 处理一般异常
        /// </summary>
        protected virtual async Task HandleExceptionAsync(Exception ex, string operationName)
        {
            var trackingCode = ClientErrorMessageMapper.GetShortTrackingCode();

            Logger.LogError(ex, "操作失败: {Operation}", operationName);

            var userMessage = GetUserFriendlyMessage(ex);
            ErrorMessage = $"{userMessage} (追踪码: {trackingCode})";
            StatusMessage = $"{operationName}失败";

            await ShowErrorMessageAsync(ErrorMessage);
        }

        /// <summary>
        /// 冲突后刷新数据（子类重写）
        /// </summary>
        protected virtual Task OnConflictRefreshRequestedAsync() => Task.CompletedTask;

        /// <summary>
        /// 获取用户友好的错误消息
        /// </summary>
        protected virtual string GetUserFriendlyMessage(Exception ex)
        {
            return ex switch
            {
                System.ComponentModel.DataAnnotations.ValidationException => "输入数据验证失败",
                UnauthorizedAccessException => "权限不足",
                TimeoutException => "操作超时",
                TaskCanceledException => "操作已取消",
                _ => "操作失败，请重试"
            };
        }

        #endregion
    }
}
