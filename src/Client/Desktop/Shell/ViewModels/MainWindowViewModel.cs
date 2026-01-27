using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.HealthCheck;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Shell.ViewModels;

/// <summary>
/// 主窗口视图模型 - 用户登录状态管理、界面导航控制、键盘快捷键
/// OpenSpec: standardize-viewmodel-framework - 迁移到CoreViewModelBase
/// </summary>
public partial class MainWindowViewModel : CoreViewModelBase
{
    #region 依赖服务

    private readonly IMainWindowServicesFacade _servicesFacade;
    private readonly IHealthCheckCoordinator _healthCheckCoordinator;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly MenuManager _menuManager;
    private readonly IActiveConsultationService _activeConsultationService;
    private readonly IApplicationTickService _tickService;
    private readonly IUserActivityTracker _userActivityTracker;
    private readonly ITokenLifecycleService _tokenLifecycleService;
    private readonly ITokenStorageService _tokenStorageService;
    private readonly ILoginCoordinator _loginCoordinator;

    /// <summary>
    /// 区域管理器
    /// </summary>
    protected IRegionManager RegionManager { get; }

    /// <summary>
    /// 通用对话框服务
    /// </summary>
    protected ICommonDialogService? CommonDialogService { get; }

    /// <summary>
    /// 用户通知服务
    /// </summary>
    protected IUserNotificationService? UserNotificationService { get; }

    #endregion

    #region 可观察属性

    /// <summary>
    /// 窗口标题
    /// </summary>
    [ObservableProperty]
    private string _title = SystemConstants.SystemTitle;

    /// <summary>
    /// 当前登录用户
    /// OpenSpec: dto-architecture-specification - 统一使用UserDetailDto
    /// </summary>
    [ObservableProperty]
    private UserDetailDto? _currentUser;

    /// <summary>
    /// 用户登录状态
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoggedIn))]
    private bool _isLoggedIn;

    /// <summary>
    /// 当前系统时间
    /// </summary>
    [ObservableProperty]
    private DateTime _currentTime = DateTime.Now;

    /// <summary>
    /// API健康状态
    /// </summary>
    [ObservableProperty]
    private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;

    /// <summary>
    /// poc-drawer-layout: Drawer是否打开
    /// </summary>
    [ObservableProperty]
    private bool _isDrawerOpen;

    #endregion

    #region 计算属性

    /// <summary>
    /// 是否未登录状态，用于界面绑定
    /// </summary>
    public bool IsNotLoggedIn => !IsLoggedIn;

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// OpenSpec: enhance-viewmodel-architecture - 使用IViewModelServices聚合服务
    /// </summary>
    public MainWindowViewModel(
        IViewModelServices services,
        IMainWindowServicesFacade servicesFacade,
        IUserNotificationService userNotificationService,
        IHealthCheckCoordinator healthCheckCoordinator,
        INavigationCoordinator navigationCoordinator,
        MenuManager menuManager,
        IActiveConsultationService activeConsultationService,
        IApplicationTickService tickService,
        IUserActivityTracker userActivityTracker,
        ITokenLifecycleService tokenLifecycleService,
        ITokenStorageService tokenStorageService,
        ILoginCoordinator loginCoordinator)
        : base(services)
    {
        RegionManager = services.RegionManager;
        CommonDialogService = services.CommonDialogService;
        UserNotificationService = userNotificationService;

        _servicesFacade = servicesFacade ?? throw new ArgumentNullException(nameof(servicesFacade));
        _healthCheckCoordinator = healthCheckCoordinator ?? throw new ArgumentNullException(nameof(healthCheckCoordinator));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
        _activeConsultationService = activeConsultationService ?? throw new ArgumentNullException(nameof(activeConsultationService));
        _tickService = tickService ?? throw new ArgumentNullException(nameof(tickService));
        _userActivityTracker = userActivityTracker ?? throw new ArgumentNullException(nameof(userActivityTracker));
        _tokenLifecycleService = tokenLifecycleService ?? throw new ArgumentNullException(nameof(tokenLifecycleService));
        _tokenStorageService = tokenStorageService ?? throw new ArgumentNullException(nameof(tokenStorageService));
        _loginCoordinator = loginCoordinator ?? throw new ArgumentNullException(nameof(loginCoordinator));

        InitializeViewModel();
    }

    #endregion

    #region 委托命令属性

    /// <summary>
    /// 显示控件示例命令 - 委托给MenuManager
    /// </summary>
    public ICommand ShowControlExamplesCommand => _menuManager.ShowControlExamplesCommand;

    /// <summary>
    /// 快速添加患者命令(Ctrl+N) - 委托给MenuManager
    /// </summary>
    public ICommand QuickAddPatientCommand => _menuManager.QuickAddPatientCommand;

    /// <summary>
    /// 快速开始看诊命令(Ctrl+Shift+C) - 委托给MenuManager
    /// </summary>
    public ICommand QuickStartMedicalCaseCommand => _menuManager.QuickStartMedicalCaseCommand;

    /// <summary>
    /// 显示帮助命令 (F1) - 委托给MenuManager
    /// </summary>
    public ICommand ShowHelpCommand => _menuManager.ShowHelpCommand;

    /// <summary>
    /// 显示设置命令 (Ctrl+,) - 委托给MenuManager
    /// </summary>
    public ICommand ShowSettingsCommand => _menuManager.ShowSettingsCommand;

    /// <summary>
    /// 主题切换命令 - 委托给MenuManager
    /// </summary>
    public ICommand ToggleThemeCommand => _menuManager.ToggleThemeCommand;

    /// <summary>
    /// 全局保存命令 (Ctrl+S) - 委托给MenuManager
    /// </summary>
    public ICommand SaveAllCommand => _menuManager.SaveAllCommand;

    /// <summary>
    /// 全局刷新命令 (F5) - 委托给MenuManager
    /// </summary>
    public ICommand RefreshAllCommand => _menuManager.RefreshAllCommand;

    /// <summary>
    /// 全局打印命令 (Ctrl+P) - 委托给MenuManager
    /// </summary>
    public ICommand PrintCommand => _menuManager.PrintCommand;

    /// <summary>
    /// 全局导出命令 - 委托给MenuManager
    /// </summary>
    public ICommand ExportCommand => _menuManager.ExportCommand;

    /// <summary>
    /// 全局撤销命令 (Ctrl+Z) - 委托给MenuManager
    /// </summary>
    public ICommand UndoCommand => _menuManager.UndoCommand;

    /// <summary>
    /// 全局重做命令 (Ctrl+Y) - 委托给MenuManager
    /// </summary>
    public ICommand RedoCommand => _menuManager.RedoCommand;

    /// <summary>
    /// 账户设置命令 - OpenSpec: migrate-views-to-role-modules
    /// </summary>
    public ICommand EditProfileCommand => _menuManager.EditProfileCommand;

    /// <summary>
    /// 导航到主页命令 - OpenSpec: fix-button-navigation-system
    /// </summary>
    public ICommand NavigateToHomeCommand => _menuManager.NavigateToHomeCommand;

    /// <summary>
    /// 导航到系统设置命令 - OpenSpec: unify-navigation-architecture (ADR-5修正: Sidebar全局入口)
    /// </summary>
    public ICommand NavigateToSystemSettingsCommand => _menuManager.NavigateToSystemSettingsCommand;

    #endregion

    #region RelayCommand

    /// <summary>
    /// 退出登录命令
    /// </summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            if (_activeConsultationService.HasActiveConsultation)
            {
                var leaveResult = await _activeConsultationService.RequestLeaveAsync();
                if (!leaveResult.CanLeave)
                {
                    Logger.LogDebug("用户选择继续停留，取消退出登录");
                    return;
                }
                Logger.LogInformation("活跃医案已处理（选择: {Choice}），继续退出登录", leaveResult.Choice);
            }
            else
            {
                var result = await ShowConfirmationAsync("确定要退出登录吗？");
                if (!result)
                {
                    return;
                }
            }

            // 执行退出登录
            await PerformLogoutAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "退出登录时发生异常");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("退出登录", ex));
        }
    }

    /// <summary>
    /// 重试API健康检查命令
    /// </summary>
    [RelayCommand]
    private async Task RetryHealthCheckAsync()
    {
        Logger.LogInformation("用户手动触发 API 健康检查");
        await _healthCheckCoordinator.CheckNowAsync();
    }

    /// <summary>
    /// poc-drawer-layout: 切换Drawer命令 (Ctrl+M)
    /// </summary>
    [RelayCommand]
    private void ToggleDrawer()
    {
        IsDrawerOpen = !IsDrawerOpen;
        Logger.LogDebug("Drawer状态切换: {IsOpen}", IsDrawerOpen);
    }

    /// <summary>
    /// poc-drawer-layout: 关闭Drawer命令 (Escape)
    /// </summary>
    [RelayCommand]
    private void CloseDrawer()
    {
        if (IsDrawerOpen)
        {
            IsDrawerOpen = false;
            Logger.LogDebug("Drawer已关闭");
        }
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 执行完整的ViewModel初始化
    /// </summary>
    private void InitializeViewModel()
    {
        InitializeClock();
        InitializeHealthCheck();
        InitializeEvents();
    }

    /// <summary>
    /// 初始化时钟计时器
    /// </summary>
    private void InitializeClock()
    {
        _tickService.Tick += OnTick;
        _tickService.Start();
        // OpenSpec: simplify-auth-architecture - SessionExpiring订阅已移除
        _userActivityTracker.SessionExpired += OnSessionExpired;
    }

    /// <summary>
    /// 初始化API健康检查
    /// </summary>
    private void InitializeHealthCheck()
    {
        _healthCheckCoordinator.StatusChanged += OnHealthStatusChanged;
        _healthCheckCoordinator.Start();
    }

    /// <summary>
    /// 初始化事件订阅
    /// </summary>
    private void InitializeEvents()
    {
        // 订阅LoginCoordinator的登录成功事件（取代EventAggregator的LoginSuccessEvent）
        _loginCoordinator.LoginSucceeded += OnLoginCoordinatorSuccess;
        // OpenSpec: unify-event-system - 使用AuthEvents聚合类
        Events.Subscribe<AuthEvents.PasswordChangedEvent, PasswordChangedPayload>(OnPasswordChanged);
        Events.Subscribe<TokenLifecycleStateChangedEvent, TokenLifecycleStateChangedEventArgs>(OnTokenLifecycleStateChanged);
        _navigationCoordinator.SubscribeToRegionCollection();
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 统一Tick处理 - 时钟更新
    /// </summary>
    private void OnTick(object? sender, ApplicationTickEventArgs e)
    {
        // UI线程更新时间显示（避免应用关闭时空引用）
        Application.Current?.Dispatcher.BeginInvoke(() => CurrentTime = DateTime.Now);
    }

    /// <summary>
    /// 健康状态变更事件处理
    /// </summary>
    private void OnHealthStatusChanged(object? sender, HealthStatusChangedEventArgs e)
    {
        Application.Current?.Dispatcher.BeginInvoke(() => ApiStatus = e.CurrentStatus);
    }

    // OpenSpec: simplify-auth-architecture - OnSessionExpiring方法已移除

    /// <summary>
    /// 会话已过期事件处理 - 执行自动登出
    /// </summary>
    private async void OnSessionExpired(object? sender, EventArgs e)
    {
        Logger.LogWarning("用户会话因不活跃已过期，执行自动登出");

        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await ShowSuccessMessageAsync("您的会话因长时间未操作已过期，请重新登录。");

            // 执行登出
            await PerformLogoutAsync();
        });
    }

    /// <summary>
    /// Token生命周期状态变更事件处理
    /// Issue #1864: 客户端Token生命周期管理
    /// 移除Warning对话框，静默处理，仅在Token真正过期时提示
    /// </summary>
    private async void OnTokenLifecycleStateChanged(TokenLifecycleStateChangedEventArgs args)
    {
        Logger.LogDebug("Token生命周期状态变更: {Previous} -> {Current}", args.PreviousState, args.CurrentState);

        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            switch (args.CurrentState)
            {
                case TokenLifecycleState.Warning:
                    // 静默处理，仅记录日志，不打扰用户（Token会自动刷新）
                    var remainingMinutes = args.RemainingTime?.TotalMinutes ?? 0;
                    Logger.LogDebug("Token即将过期，剩余时间: {RemainingMinutes:F1} 分钟，系统将自动刷新", remainingMinutes);
                    break;

                case TokenLifecycleState.Expired:
                    await HandleTokenExpiredAsync();
                    break;
            }
        });
    }

    /// <summary>
    /// LoginCoordinator登录成功事件处理
    /// 负责更新UI状态（LoginCoordinator已处理模块加载和导航）
    /// </summary>
    private void OnLoginCoordinatorSuccess(object? sender, LoginSuccessEventArgs args)
    {
        var user = args.User;

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // 更新UI状态
            IsLoggedIn = true;
            CurrentUser = user;

            // 设置窗口标题
            bool isAdmin = user.UserName?.Equals(SystemConstants.SuperAdminUsername, StringComparison.OrdinalIgnoreCase) == true
                           || user.Role == UserRole.Admin;
            var userDisplayName = string.IsNullOrEmpty(user.RealName) ? user.UserName : user.RealName;
            Title = $"凌隐宝堂中医诊所诊疗系统 - {userDisplayName} ({(isAdmin ? "管理员" : "医生")})";

            // 清理登录区域
            _navigationCoordinator.ClearLoginRegion();

            // 启动用户活动追踪
            _userActivityTracker.StartTracking();

            // Issue #1864: 启动Token生命周期监控
            _ = StartTokenLifecycleMonitoringAsync();

            Logger.LogInformation("登录成功UI更新完成 [用户: {Username}, 自动登录: {IsAutoLogin}]",
                user.UserName, args.IsAutoLogin);
        });
    }

    /// <summary>
    /// 密码修改成功事件处理
    /// </summary>
    /// <remarks>OpenSpec: unify-event-system - 使用AuthEvents.PasswordChangedEvent</remarks>
    private void OnPasswordChanged(PasswordChangedPayload payload)
    {
        Logger.LogInformation("收到密码修改成功事件 [用户: {UserName}]，导航到登录界面", payload.UserName);
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            CurrentUser = null;
            IsLoggedIn = false;
            Title = "凌隐宝堂中医诊所诊疗系统";
            _navigationCoordinator.ClearContentRegion();
            _navigationCoordinator.ShowLoginDialog();
        });
    }

    #endregion

    #region 业务逻辑

    /// <summary>
    /// 处理Token已过期
    /// </summary>
    private async Task HandleTokenExpiredAsync()
    {
        Logger.LogWarning("Token已过期，执行自动登出");

        await ShowSuccessMessageAsync("您的登录凭证已过期，请重新登录。");

        // 重置Token生命周期服务
        _tokenLifecycleService.Reset();

        // 执行登出
        await PerformLogoutAsync();
    }

    /// <summary>
    /// 执行实际的退出登录操作
    /// </summary>
    private Task PerformLogoutAsync()
    {
        _userActivityTracker.StopTracking();
        _tokenLifecycleService.Reset(); // Issue #1864: 重置Token生命周期
        CurrentUser = null;
        IsLoggedIn = false;
        Title = "凌隐宝堂中医诊所诊疗系统";

        _navigationCoordinator.ClearContentRegion();
        _navigationCoordinator.ShowLoginDialog();
        _ = Task.Run(async () =>
        {
            try
            {
                await _servicesFacade.AuthenticationService.LogoutAsync();
                // OpenSpec: unify-event-system - 使用AuthEvents聚合类
                EventAggregator.GetEvent<AuthEvents.LogoutCompletedEvent>().Publish(new LogoutCompletedPayload
                {
                    LocalLogoutCompleted = true,
                    ServerLogoutCompleted = true
                });
            }
            catch (Exception ex) { Logger.LogWarning(ex, "后台登出处理异常"); }
        });
        return Task.CompletedTask;
    }

    /// <summary>
    /// 启动Token生命周期监控
    /// Issue #1864: 客户端Token生命周期管理
    /// </summary>
    private async Task StartTokenLifecycleMonitoringAsync()
    {
        try
        {
            var loginResponse = await _tokenStorageService.GetLoginResponseAsync();

            if (loginResponse != null && loginResponse.ExpiresAt > DateTime.UtcNow)
            {
                _tokenLifecycleService.StartMonitoring(loginResponse.ExpiresAt);
                Logger.LogInformation("Token生命周期监控已启动 [过期时间: {ExpiresAt}]", loginResponse.ExpiresAt);
            }
            else
            {
                Logger.LogWarning("无法启动Token生命周期监控：LoginResponse为空或已过期");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "启动Token生命周期监控时发生异常");
        }
    }

    /// <summary>
    /// 检查登录状态
    /// </summary>
    private async Task CheckLoginStatusAsync()
    {
        try { _navigationCoordinator.ShowLoginDialog(); }
        catch (Exception ex) { await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("初始化登录界面", ex)); _navigationCoordinator.ShowLoginDialog(); }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 窗口加载完成回调
    /// </summary>
    public async Task OnWindowLoadedAsync()
    {
        await Task.Delay(500);
        await CheckLoginStatusAsync();
    }

    /// <summary>
    /// 请求关闭应用程序（显示确认框）
    /// remove-titlebar-add-close-button: 供Alt+F4调用，仅在登录界面可用
    /// </summary>
    /// <returns>用户是否确认关闭</returns>
    public async Task<bool> RequestCloseApplicationAsync()
    {
        var confirmed = await ShowConfirmationAsync("确定要退出程序吗？", "退出确认");
        if (confirmed)
        {
            Application.Current.Shutdown();
        }
        return confirmed;
    }

    #endregion

    #region 对话框辅助方法

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
    protected virtual async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
    {
        if (CommonDialogService != null)
        {
            return await CommonDialogService.ShowConfirmAsync(message, title);
        }
        Logger.LogWarning("CommonDialogService不可用，确认对话框未显示: {Message}，默认返回false", message);
        return false;
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// 重写OnDisposing方法，清理资源防止内存泄漏
    /// </summary>
    protected override void OnDisposing()
    {
        try
        {
            CleanupTickSubscription();
            CleanupHealthCheckCoordinator();
            UnsubscribeLoginEvent();
            _navigationCoordinator.UnsubscribeFromRegionCollection();
            _tokenLifecycleService.Dispose(); // Issue #1864: 释放Token生命周期服务
        }
        catch (Exception ex) { Logger.LogError(ex, "资源清理异常"); }
        finally { base.OnDisposing(); }
    }

    /// <summary>
    /// 清理Tick订阅和用户活动追踪
    /// </summary>
    private void CleanupTickSubscription()
    {
        _tickService.Tick -= OnTick;
        // OpenSpec: simplify-auth-architecture - SessionExpiring订阅已移除
        _userActivityTracker.SessionExpired -= OnSessionExpired;
        _userActivityTracker.StopTracking();
    }

    /// <summary>
    /// 清理健康检查协调器订阅
    /// </summary>
    private void CleanupHealthCheckCoordinator()
    {
        try
        {
            _healthCheckCoordinator.StatusChanged -= OnHealthStatusChanged;
            _healthCheckCoordinator.Dispose();
        }
        catch (Exception ex) { Logger.LogError(ex, "清理健康检查协调器失败"); }
    }

    /// <summary>
    /// 取消登录事件订阅
    /// </summary>
    private void UnsubscribeLoginEvent()
    {
        try { _loginCoordinator.LoginSucceeded -= OnLoginCoordinatorSuccess; }
        catch (Exception ex) { Logger.LogError(ex, "取消LoginCoordinator事件订阅失败"); }
    }

    #endregion
}
