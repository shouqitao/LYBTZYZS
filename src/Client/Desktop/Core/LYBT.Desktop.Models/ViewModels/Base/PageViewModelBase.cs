using System.Net;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Http;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Logging;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 页面ViewModel基类 - 提供导航、对话框、安全执行功能
    /// OpenSpec: migrate-to-communitytoolkit-mvvm
    ///
    /// 继承自ValidatingViewModelBase，实现INavigationAware
    /// 构造函数参数从7个减少到5个
    /// </summary>
    public abstract partial class PageViewModelBase : ValidatingViewModelBase, INavigationAware
    {
        #region 依赖服务

        protected readonly IRegionManager RegionManager;
        protected readonly ICommonDialogService DialogService;
        protected readonly IApiService ApiService;
        protected readonly ISessionManager SessionManager;

        #endregion

        #region 可观察属性

        /// <summary>
        /// 页面标题
        /// </summary>
        [ObservableProperty]
        private string _pageTitle = string.Empty;

        #endregion

        #region 构造函数

        protected PageViewModelBase(
            IRegionManager regionManager,
            ICommonDialogService dialogService,
            IApiService apiService,
            ISessionManager sessionManager,
            ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
            RegionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            ApiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        }

        #endregion

        #region 导航命令

        /// <summary>
        /// 返回主页命令
        /// </summary>
        [RelayCommand]
        protected virtual void NavigateToHome()
        {
            var homeView = GetHomeViewName();
            NavigateTo("ContentRegion", homeView);
        }

        #endregion

        #region 导航方法

        /// <summary>
        /// 导航到指定视图
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <param name="viewName">视图名称</param>
        /// <param name="parameters">导航参数</param>
        protected virtual void NavigateTo(string regionName, string viewName, NavigationParameters? parameters = null)
        {
            try
            {
                Logger.LogDebug("导航到视图: {ViewName} (区域: {RegionName})", viewName, regionName);
                RegionManager.RequestNavigate(regionName, viewName, parameters ?? new NavigationParameters());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航失败: {ViewName}", viewName);
                _ = ShowErrorMessageAsync($"导航失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 导航回退
        /// </summary>
        /// <param name="regionName">区域名称</param>
        protected virtual void NavigateBack(string regionName)
        {
            try
            {
                var region = RegionManager.Regions[regionName];
                if (region?.NavigationService?.Journal?.CanGoBack == true)
                {
                    region.NavigationService.Journal.GoBack();
                    Logger.LogDebug("导航回退成功: {RegionName}", regionName);
                }
                else
                {
                    Logger.LogWarning("无法回退，导航历史为空: {RegionName}", regionName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航回退失败");
                _ = ShowErrorMessageAsync($"导航回退失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 导航回退（带参数）
        /// </summary>
        protected virtual void NavigateBack(string regionName, NavigationParameters parameters)
        {
            try
            {
                var region = RegionManager.Regions[regionName];
                if (region?.NavigationService?.Journal?.CanGoBack == true)
                {
                    var journal = region.NavigationService.Journal;
                    var currentEntry = journal.CurrentEntry;

                    if (currentEntry != null)
                    {
                        journal.GoBack();

                        var currentView = region.ActiveViews.FirstOrDefault();
                        if (currentView != null)
                        {
                            var dataContext = currentView.GetType().GetProperty("DataContext")?.GetValue(currentView);
                            if (dataContext is INavigationAware navigationAware)
                            {
                                var navigationContext = new NavigationContext(
                                    region.NavigationService,
                                    new Uri(currentEntry.Uri.OriginalString, UriKind.Relative),
                                    parameters);
                                navigationAware.OnNavigatedTo(navigationContext);
                            }
                        }

                        Logger.LogDebug("导航回退成功（带参数）: {RegionName}", regionName);
                    }
                }
                else
                {
                    Logger.LogWarning("无法回退，导航历史为空: {RegionName}", regionName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航回退失败");
                _ = ShowErrorMessageAsync($"导航回退失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 是否可以回退
        /// </summary>
        protected virtual bool CanNavigateBack(string regionName)
        {
            try
            {
                return RegionManager.Regions[regionName]?.NavigationService?.Journal?.CanGoBack ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取主页视图名称（根据用户角色）
        /// </summary>
        protected virtual string GetHomeViewName()
        {
            var role = SessionManager.CurrentUser?.Role;

            return role switch
            {
                UserRole.Admin or UserRole.SuperAdmin => "AdminHomeView",
                UserRole.Doctor => "ClinicalHomeView",
                _ => "AdminHomeView"
            };
        }

        #endregion

        #region 对话框方法

        /// <summary>
        /// 显示成功消息
        /// </summary>
        protected virtual async Task ShowSuccessMessageAsync(string message)
        {
            await DialogService.ShowInfoAsync(message, "成功");
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        protected virtual async Task ShowErrorMessageAsync(string message)
        {
            await DialogService.ShowErrorAsync(message, "错误");
        }

        /// <summary>
        /// 显示警告消息
        /// </summary>
        protected virtual async Task ShowWarningMessageAsync(string message)
        {
            await DialogService.ShowWarningAsync(message, "警告");
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">确认消息</param>
        /// <param name="title">对话框标题</param>
        /// <returns>用户是否确认</returns>
        protected virtual async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            return await DialogService.ShowConfirmAsync(message, title);
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
                SessionManager.ClearSession();
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// 处理409冲突响应
        /// </summary>
        protected virtual async Task HandleConflictAsync(string operationName)
        {
            Logger.LogWarning("数据冲突: {Operation}", operationName);

            var shouldRefresh = await ShowConfirmationAsync(
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

        #region 用户信息

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        protected virtual string GetCurrentUserInfo()
        {
            return SessionManager.CurrentUser?.RealName ?? "未知用户";
        }

        /// <summary>
        /// 检查用户是否已登录
        /// </summary>
        protected virtual bool IsUserLoggedIn()
        {
            return SessionManager.IsAuthenticated;
        }

        #endregion

        #region INavigationAware实现

        /// <summary>
        /// 导航到此页面时调用
        /// </summary>
        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            Logger.LogDebug("进入页面: {PageTitle}", PageTitle);

            try
            {
                ProcessNavigationParameters(navigationContext.Parameters);

                _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        await InitializeAsync(navigationContext.Parameters);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "InitializeAsync执行失败");
                        await HandleExceptionAsync(ex, "数据初始化");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "页面导航处理失败");
                _ = HandleExceptionAsync(ex, "页面加载");
            }
        }

        /// <summary>
        /// 是否为导航目标
        /// </summary>
        public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <summary>
        /// 离开此页面时调用
        /// </summary>
        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
            Logger.LogDebug("离开页面: {PageTitle}", PageTitle);
        }

        /// <summary>
        /// 处理导航参数（子类重写）
        /// </summary>
        protected virtual void ProcessNavigationParameters(NavigationParameters parameters) { }

        /// <summary>
        /// 页面初始化（子类重写）
        /// </summary>
        protected virtual Task InitializeAsync(NavigationParameters parameters) => Task.CompletedTask;

        #endregion
    }
}
