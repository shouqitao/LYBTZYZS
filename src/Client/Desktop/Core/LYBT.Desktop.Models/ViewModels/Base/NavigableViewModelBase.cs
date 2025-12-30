using System.Reactive.Disposables;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Prism.Regions;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 可导航ViewModel基类
    /// OpenSpec: migrate-to-communitytoolkit-mvvm
    ///
    /// 继承ObservableValidator(支持验证特性)，添加:
    /// - 导航服务 (IRegionManager)
    /// - 对话框服务 (ICommonDialogService)
    /// - 用户通知服务 (IUserNotificationService)
    /// - 会话管理 (ISessionManager)
    /// - 数据验证 (通过ObservableValidator内置支持)
    /// </summary>
    public abstract partial class NavigableViewModelBase : ObservableValidator, INavigationAware, IRegionMemberLifetime, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private bool _disposed;

        #region 服务

        protected readonly IRegionManager RegionManager;
        protected readonly ISessionManager? SessionManager;
        protected readonly IUserNotificationService? UserNotificationService;
        protected readonly ICommonDialogService? CommonDialogService;

        /// <summary>
        /// 日志记录器
        /// </summary>
        protected ILogger Logger { get; }

        #endregion

        #region 可观察属性

        /// <summary>
        /// 页面标题
        /// </summary>
        [ObservableProperty]
        private string _pageTitle = string.Empty;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotLoading))]
        private bool _isLoading;

        /// <summary>
        /// 是否正在执行操作
        /// </summary>
        [ObservableProperty]
        private bool _isBusy;

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

        /// <summary>
        /// 是否在导航离开时保持活动
        /// </summary>
        public virtual bool KeepAlive => false;

        #endregion

        #region 构造函数

        protected NavigableViewModelBase(
            IRegionManager regionManager,
            ILoggerFactory loggerFactory,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null,
            ICommonDialogService? commonDialogService = null)
        {
            RegionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            Logger = loggerFactory?.CreateLogger(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));
            SessionManager = sessionManager;
            UserNotificationService = userNotificationService;
            CommonDialogService = commonDialogService;
        }

        #endregion

        #region 导航命令

        /// <summary>
        /// 返回主页命令
        /// </summary>
        [RelayCommand]
        protected virtual void NavigateToHome()
        {
            try
            {
                var homeViewName = GetHomeViewName();
                Logger.LogDebug("返回主页: {HomeViewName}", homeViewName);
                NavigateTo("ContentRegion", homeViewName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "返回主页失败");
                HandleError(ex, "返回主页");
            }
        }

        #endregion

        #region 导航方法

        /// <summary>
        /// 导航到指定视图
        /// </summary>
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
                HandleError(ex, "导航");
            }
        }

        /// <summary>
        /// 导航返回
        /// </summary>
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
                HandleError(ex, "导航回退");
            }
        }

        /// <summary>
        /// 导航返回并传递参数
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
                    if (currentEntry != null && journal.CanGoBack)
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
                        Logger.LogDebug("导航回退成功: {RegionName}", regionName);
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
                HandleError(ex, "导航回退");
            }
        }

        /// <summary>
        /// 获取主页视图名称
        /// </summary>
        protected virtual string GetHomeViewName()
        {
            var sessionManager = SessionManager;
            if (sessionManager == null)
            {
                try
                {
                    sessionManager = ContainerLocator.Container?.Resolve<ISessionManager>();
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "无法从容器获取 SessionManager");
                }
            }

            var role = sessionManager?.CurrentUser?.Role;
            return role switch
            {
                UserRole.Admin or UserRole.SuperAdmin => "AdminHomeView",
                UserRole.Doctor => "ClinicalHomeView",
                _ => "AdminHomeView"
            };
        }

        #endregion

        #region INavigationAware

        /// <inheritdoc/>
        public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <inheritdoc/>
        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
            Logger.LogDebug("离开页面: {PageTitle}", PageTitle);
        }

        /// <inheritdoc/>
        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            Logger.LogDebug("进入页面: {ViewType}", GetType().Name);
            try
            {
                ProcessNavigationParameters(navigationContext.Parameters);
                _ = Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        await InitializeAsync(navigationContext.Parameters);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "InitializeAsync 执行失败");
                        HandleError(ex, "数据初始化");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "页面导航处理失败");
                HandleError(ex, "页面加载");
            }
        }

        /// <summary>
        /// 处理导航参数
        /// </summary>
        protected virtual void ProcessNavigationParameters(NavigationParameters parameters) { }

        /// <summary>
        /// 初始化异步
        /// </summary>
        protected virtual Task InitializeAsync(NavigationParameters parameters) => Task.CompletedTask;

        #endregion

        #region 对话框方法

        /// <summary>
        /// 显示成功消息
        /// </summary>
        protected virtual async Task ShowSuccessMessageAsync(string message)
        {
            if (CommonDialogService != null)
            {
                await CommonDialogService.ShowInfoAsync(message, "成功");
                return;
            }
            Logger.LogWarning("CommonDialogService不可用，成功消息未显示: {Message}", message);
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        protected virtual async Task ShowErrorMessageAsync(string message)
        {
            if (CommonDialogService != null)
            {
                await CommonDialogService.ShowErrorAsync(message, "错误");
                return;
            }
            Logger.LogError("CommonDialogService不可用，错误消息未显示: {Message}", message);
        }

        /// <summary>
        /// 显示警告消息
        /// </summary>
        protected virtual async Task ShowWarningMessageAsync(string message)
        {
            if (CommonDialogService != null)
            {
                await CommonDialogService.ShowWarningAsync(message, "警告");
                return;
            }
            Logger.LogWarning("CommonDialogService不可用，警告消息未显示: {Message}", message);
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        protected virtual async Task<bool> ShowConfirmMessageAsync(string message, string title = "确认")
        {
            if (CommonDialogService != null)
            {
                return await CommonDialogService.ShowConfirmAsync(message, title);
            }
            Logger.LogWarning("CommonDialogService不可用，确认对话框未显示: {Message}", message);
            return false;
        }

        #endregion

        #region 错误处理

        /// <summary>
        /// 处理错误
        /// </summary>
        protected virtual void HandleError(Exception ex, string? context = null)
        {
            Logger.LogError(ex, "错误发生在: {Context}", context ?? "未知操作");
            ErrorMessage = GetUserFriendlyMessage(ex);
        }

        /// <summary>
        /// 获取用户友好的错误消息
        /// </summary>
        protected virtual string GetUserFriendlyMessage(Exception ex) => ex switch
        {
            System.ComponentModel.DataAnnotations.ValidationException => "输入数据验证失败",
            UnauthorizedAccessException => "权限不足",
            TimeoutException => "操作超时",
            TaskCanceledException => "操作已取消",
            _ => "操作失败，请重试"
        };

        #endregion

        #region 辅助方法

        /// <summary>
        /// 设置忙碌状态
        /// </summary>
        protected void SetIsBusy(bool isBusy, string? message = null)
        {
            IsBusy = isBusy;
            if (!string.IsNullOrEmpty(message))
            {
                StatusMessage = message;
            }
            else if (!isBusy)
            {
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        protected virtual string GetCurrentUserInfo()
        {
            return SessionManager?.CurrentUser?.RealName ?? "未知用户";
        }

        /// <summary>
        /// 是否已登录
        /// </summary>
        protected virtual bool IsUserLoggedIn()
        {
            return SessionManager?.IsAuthenticated ?? false;
        }

        /// <summary>
        /// 清除错误
        /// </summary>
        protected void ClearError()
        {
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// 在UI线程上执行操作
        /// </summary>
        protected void RunOnUIThread(Action action)
        {
            if (Application.Current?.Dispatcher == null)
            {
                action();
                return;
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Application.Current.Dispatcher.Invoke(action);
            }
        }

        /// <summary>
        /// 在UI线程上异步执行操作
        /// </summary>
        protected Task RunOnUIThreadAsync(Func<Task> action)
        {
            if (Application.Current?.Dispatcher == null)
                return action();

            return Application.Current.Dispatcher.InvokeAsync(action).Task;
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
