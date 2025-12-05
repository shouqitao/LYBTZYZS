using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Commands;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Shell.Services;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Shell.ViewModels;

/// <summary>主窗口视图模型 - 用户登录状态管理、界面导航控制、键盘快捷键</summary>
public class MainWindowViewModel : UnifiedViewModelBase
{
    private readonly IMainWindowServicesFacade _servicesFacade;
    private readonly IApiHealthCheckService _apiHealthCheckService;
    private readonly NavigationManager _navigationManager;
    private readonly MenuManager _menuManager;
    private readonly IActiveConsultationService _activeConsultationService;
    private readonly IApplicationTickService _tickService;
    private readonly IUserActivityTracker _userActivityTracker;
    private readonly ITokenLifecycleService _tokenLifecycleService;
    private readonly ITokenStorageService _tokenStorageService;
    private readonly ILoginCoordinator _loginCoordinator;

    /// <summary>构造函数</summary>
    public MainWindowViewModel(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        IMainWindowServicesFacade servicesFacade,
        ILoggerFactory loggerFactory,
        LYBT.Desktop.Infrastructure.Interfaces.IUserNotificationService userNotificationService,
        IApiHealthCheckService apiHealthCheckService,
        NavigationManager navigationManager,
        MenuManager menuManager,
        IActiveConsultationService activeConsultationService,
        IApplicationTickService tickService,
        IUserActivityTracker userActivityTracker,
        ITokenLifecycleService tokenLifecycleService,
        ITokenStorageService tokenStorageService,
        ILoginCoordinator loginCoordinator,
        ICommonDialogService commonDialogService)
        : base(eventAggregator, loggerFactory, regionManager, null, userNotificationService, commonDialogService)
    {
        _servicesFacade = servicesFacade ?? throw new ArgumentNullException(nameof(servicesFacade));
        _apiHealthCheckService = apiHealthCheckService ?? throw new ArgumentNullException(nameof(apiHealthCheckService));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
        _activeConsultationService = activeConsultationService ?? throw new ArgumentNullException(nameof(activeConsultationService));
        _tickService = tickService ?? throw new ArgumentNullException(nameof(tickService));
        _userActivityTracker = userActivityTracker ?? throw new ArgumentNullException(nameof(userActivityTracker));
        _tokenLifecycleService = tokenLifecycleService ?? throw new ArgumentNullException(nameof(tokenLifecycleService));
        _tokenStorageService = tokenStorageService ?? throw new ArgumentNullException(nameof(tokenStorageService));
        _loginCoordinator = loginCoordinator ?? throw new ArgumentNullException(nameof(loginCoordinator));

        InitializeViewModel();
    }

    private string _title = SystemConstants.SystemTitle;
    private UserDto? _currentUser;
    private bool _isLoggedIn = false;
    private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;
    private long _lastHealthCheckTick;
    private const int HealthCheckIntervalSeconds = 10;

    // poc-drawer-layout: Drawer状态
    private bool _isDrawerOpen = false;

    /// <summary>获取或设置窗口标题</summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>获取或设置当前登录用户</summary>
    public UserDto? CurrentUser
    {
        get => _currentUser;
        set => SetProperty(ref _currentUser, value);
    }

    /// <summary>获取或设置用户登录状态</summary>
    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set { SetProperty(ref _isLoggedIn, value); RaisePropertyChanged(nameof(IsNotLoggedIn)); }
    }

    /// <summary>获取或设置当前系统时间显示</summary>
    public string CurrentTime
    {
        get => _currentTime;
        set => SetProperty(ref _currentTime, value);
    }

    /// <summary>获取或设置 API 健康状态</summary>
    public ApiHealthStatus ApiStatus
    {
        get => _apiStatus;
        set => SetProperty(ref _apiStatus, value);
    }

    /// <summary>获取是否未登录状态，用于界面绑定</summary>
    public bool IsNotLoggedIn => !_isLoggedIn;

    /// <summary>poc-drawer-layout: Drawer是否打开</summary>
    public bool IsDrawerOpen
    {
        get => _isDrawerOpen;
        set => SetProperty(ref _isDrawerOpen, value);
    }

    #region 命令属性

    /// <summary>退出登录命令</summary>
    public DelegateCommand LogoutCommand { get; set; } = null!;

    /// <summary>重试 API 健康检查命令</summary>
    public DelegateCommand RetryHealthCheckCommand { get; set; } = null!;

    /// <summary>显示控件示例命令 - 委托给MenuManager</summary>
    public DelegateCommand ShowControlExamplesCommand => _menuManager.ShowControlExamplesCommand;

    /// <summary>快速添加患者命令(Ctrl+N) - 委托给MenuManager</summary>
    public DelegateCommand QuickAddPatientCommand => _menuManager.QuickAddPatientCommand;

    /// <summary>快速开始诊疗命令(Ctrl+Shift+C) - 委托给MenuManager</summary>
    public DelegateCommand QuickStartConsultationCommand => _menuManager.QuickStartConsultationCommand;

    /// <summary>显示帮助命令 (F1) - 委托给MenuManager</summary>
    public DelegateCommand ShowHelpCommand => _menuManager.ShowHelpCommand;

    /// <summary>显示设置命令 (Ctrl+,) - 委托给MenuManager</summary>
    public DelegateCommand ShowSettingsCommand => _menuManager.ShowSettingsCommand;

    /// <summary>主题切换命令 - 委托给MenuManager</summary>
    public DelegateCommand ToggleThemeCommand => _menuManager.ToggleThemeCommand;

    /// <summary>全局保存命令 (Ctrl+S) - 委托给MenuManager</summary>
    public ICommand SaveAllCommand => _menuManager.SaveAllCommand;

    /// <summary>全局刷新命令 (F5) - 委托给MenuManager</summary>
    public ICommand RefreshAllCommand => _menuManager.RefreshAllCommand;

    /// <summary>全局打印命令 (Ctrl+P) - 委托给MenuManager</summary>
    public ICommand PrintCommand => _menuManager.PrintCommand;

    /// <summary>全局导出命令 - 委托给MenuManager</summary>
    public ICommand ExportCommand => _menuManager.ExportCommand;

    /// <summary>全局撤销命令 (Ctrl+Z) - 委托给MenuManager</summary>
    public ICommand UndoCommand => _menuManager.UndoCommand;

    /// <summary>全局重做命令 (Ctrl+Y) - 委托给MenuManager</summary>
    public ICommand RedoCommand => _menuManager.RedoCommand;

    /// <summary>poc-drawer-layout: 切换Drawer命令 (Ctrl+M)</summary>
    public DelegateCommand ToggleDrawerCommand { get; private set; } = null!;

    /// <summary>poc-drawer-layout: 关闭Drawer命令 (Escape)</summary>
    public DelegateCommand CloseDrawerCommand { get; private set; } = null!;

    /// <summary>poc-drawer-layout: 修改个人资料命令 - 委托给MenuManager</summary>
    public DelegateCommand EditProfileCommand => _menuManager.EditProfileCommand;

    /// <summary>poc-drawer-layout: 修改密码命令 - 委托给MenuManager</summary>
    public DelegateCommand ChangePasswordCommand => _menuManager.ChangePasswordCommand;

    #endregion

    /// <summary>初始化核心命令</summary>
    private new void InitializeCommands()
    {
        LogoutCommand = new DelegateCommand(async () => await ExecuteLogoutAsync().ConfigureAwait(false));
        RetryHealthCheckCommand = new DelegateCommand(async () => await ExecuteRetryHealthCheckAsync().ConfigureAwait(false));

        // poc-drawer-layout: Drawer命令
        ToggleDrawerCommand = new DelegateCommand(ExecuteToggleDrawer);
        CloseDrawerCommand = new DelegateCommand(ExecuteCloseDrawer);

        Logger.LogDebug("核心命令已初始化");
    }

    /// <summary>poc-drawer-layout: 切换Drawer状态</summary>
    private void ExecuteToggleDrawer()
    {
        IsDrawerOpen = !IsDrawerOpen;
        Logger.LogDebug("Drawer状态切换: {IsOpen}", IsDrawerOpen);
    }

    /// <summary>poc-drawer-layout: 关闭Drawer</summary>
    private void ExecuteCloseDrawer()
    {
        if (IsDrawerOpen)
        {
            IsDrawerOpen = false;
            Logger.LogDebug("Drawer已关闭");
        }
    }

    /// <summary>初始化时钟计时器</summary>
    private void InitializeClock()
    {
        _tickService.Tick += OnTick;
        _tickService.Start();
        _userActivityTracker.SessionExpiring += OnSessionExpiring;
        _userActivityTracker.SessionExpired += OnSessionExpired;
    }

    /// <summary>初始化API健康检查</summary>
    private void InitializeHealthCheck()
    {
        _lastHealthCheckTick = _tickService.TickCount;
        _ = Task.Run(async () => await OnHealthCheckTickAsync());
    }

    /// <summary>健康检查定时器Tick事件处理</summary>
    private async Task OnHealthCheckTickAsync()
    {
        try
        {
            ApiStatus = ApiHealthStatus.Checking;
            var status = await _apiHealthCheckService.CheckHealthAsync(timeout: 5000);
            ApiStatus = status;

            if (status == ApiHealthStatus.Unhealthy)
            {
                Logger.LogWarning("API 健康检查失败: {ErrorMessage}", _apiHealthCheckService.LastErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "执行 API 健康检查时发生异常");
            ApiStatus = ApiHealthStatus.Unhealthy;
        }
    }

    /// <summary>执行重试API健康检查</summary>
    private async Task ExecuteRetryHealthCheckAsync()
    {
        Logger.LogInformation("用户手动触发 API 健康检查");
        await OnHealthCheckTickAsync();
    }

    /// <summary>初始化事件订阅</summary>
    private void InitializeEvents()
    {
        // 订阅LoginCoordinator的登录成功事件（取代EventAggregator的LoginSuccessEvent）
        _loginCoordinator.LoginSucceeded += OnLoginCoordinatorSuccess;
        EventAggregator.GetEvent<PasswordChangedEvent>().Subscribe(OnPasswordChanged);
        EventAggregator.GetEvent<TokenLifecycleStateChangedEvent>().Subscribe(OnTokenLifecycleStateChanged);
        _navigationManager.SubscribeToRegionCollection();
    }

    /// <summary>执行完整的ViewModel初始化</summary>
    private void InitializeViewModel()
    {
        InitializeClock();
        InitializeHealthCheck();
        InitializeCommands();
        InitializeEvents();
    }

    /// <summary>统一Tick处理 - 时钟更新和健康检查</summary>
    private void OnTick(object? sender, ApplicationTickEventArgs e)
    {
        CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        if (e.TickCount - _lastHealthCheckTick >= HealthCheckIntervalSeconds)
        {
            _lastHealthCheckTick = e.TickCount;
            _ = OnHealthCheckTickAsync();
        }
    }

    /// <summary>会话即将过期事件处理</summary>
    private async void OnSessionExpiring(object? sender, SessionExpiringEventArgs e)
    {
        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            _userActivityTracker.StopTracking();

            try
            {
                var remainingMinutes = e.RemainingTime.TotalMinutes;
                var message = $"您已有一段时间未操作，会话将在约 {remainingMinutes:F0} 分钟后过期。\n\n是否继续当前会话？";

                var result = await ShowConfirmationAsync(message, "会话即将过期");

                if (result)
                {
                    // 用户选择继续，重启追踪并重置计时器
                    _userActivityTracker.StartTracking();
                    _userActivityTracker.ResetActivity();
                    Logger.LogInformation("用户选择继续会话，活动计时器已重置");
                }
                else
                {
                    // 用户选择不继续，立即执行登出
                    Logger.LogInformation("用户选择结束会话，执行登出");
                    _ = PerformLogoutAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "处理会话过期警告时出错");
                // 出错时恢复追踪
                _userActivityTracker.StartTracking();
            }
        });
    }

    /// <summary>会话已过期事件处理 - 执行自动登出</summary>
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
    /// </summary>
    private async void OnTokenLifecycleStateChanged(TokenLifecycleStateChangedEventArgs args)
    {
        Logger.LogDebug("Token生命周期状态变更: {Previous} -> {Current}", args.PreviousState, args.CurrentState);

        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            switch (args.CurrentState)
            {
                case TokenLifecycleState.Warning:
                    await HandleTokenWarningAsync(args);
                    break;

                case TokenLifecycleState.Expired:
                    await HandleTokenExpiredAsync();
                    break;
            }
        });
    }

    /// <summary>处理Token即将过期警告</summary>
    private async Task HandleTokenWarningAsync(TokenLifecycleStateChangedEventArgs args)
    {
        var remainingMinutes = args.RemainingTime?.TotalMinutes ?? 0;
        var message = $"您的登录凭证将在约 {remainingMinutes:F0} 分钟后过期。\n\n系统正在尝试自动刷新，如果刷新失败，您需要重新登录。";

        Logger.LogWarning("Token即将过期，剩余时间: {RemainingMinutes:F1} 分钟", remainingMinutes);

        // 显示提示信息（非阻塞）
        await ShowSuccessMessageAsync(message);
    }

    /// <summary>处理Token已过期</summary>
    private async Task HandleTokenExpiredAsync()
    {
        Logger.LogWarning("Token已过期，执行自动登出");

        await ShowSuccessMessageAsync("您的登录凭证已过期，请重新登录。");

        // 重置Token生命周期服务
        _tokenLifecycleService.Reset();

        // 执行登出
        await PerformLogoutAsync();
    }

    /// <summary>退出登录命令执行</summary>
    private async Task ExecuteLogoutAsync()
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
            await ShowErrorMessageAsync($"退出登录失败:{ex.Message}");
        }
    }

    /// <summary>执行实际的退出登录操作</summary>
    private Task PerformLogoutAsync()
    {
        _userActivityTracker.StopTracking();
        _tokenLifecycleService.Reset(); // Issue #1864: 重置Token生命周期
        CurrentUser = null;
        IsLoggedIn = false;
        Title = "凌隐宝堂中医诊所诊疗系统";

        _navigationManager.ClearContentRegion();
        _navigationManager.ShowLoginDialog();
        _ = Task.Run(async () =>
        {
            try
            {
                await _servicesFacade.AuthenticationService.LogoutAsync();
                EventAggregator.GetEvent<LogoutEvent>().Publish(new LogoutEventArgs { Reason = LogoutReason.UserInitiated, Message = "用户主动退出" });
            }
            catch (Exception ex) { Logger.LogWarning(ex, "后台登出处理异常"); }
        });
        return Task.CompletedTask;
    }

    /// <summary>检查登录状态</summary>
    private async Task CheckLoginStatusAsync()
    {
        try { _navigationManager.ShowLoginDialog(); }
        catch (Exception ex) { await ShowErrorMessageAsync($"初始化登录界面失败:{ex.Message}"); _navigationManager.ShowLoginDialog(); }
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
            _navigationManager.ClearLoginRegion();

            // 启动用户活动追踪
            _userActivityTracker.StartTracking();

            // Issue #1864: 启动Token生命周期监控
            _ = StartTokenLifecycleMonitoringAsync();

            Logger.LogInformation("登录成功UI更新完成 [用户: {Username}, 自动登录: {IsAutoLogin}]",
                user.UserName, args.IsAutoLogin);
        });
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

    /// <summary>密码修改成功事件处理</summary>
    private void OnPasswordChanged()
    {
        Logger.LogInformation("收到密码修改成功事件，导航到登录界面");
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            CurrentUser = null;
            IsLoggedIn = false;
            Title = "凌隐宝堂中医诊所诊疗系统";
            _navigationManager.ClearContentRegion();
            _navigationManager.ShowLoginDialog();
        });
    }

    /// <summary>窗口加载完成回调</summary>
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

    #region IDisposable

    /// <summary>重写OnDisposing方法，清理资源防止内存泄漏</summary>
    protected override void OnDisposing()
    {
        try
        {
            CleanupTickSubscription();
            UnsubscribeLoginEvent();
            UnsubscribeTokenLifecycleEvent();
            _navigationManager.UnsubscribeFromRegionCollection();
            _tokenLifecycleService.Dispose(); // Issue #1864: 释放Token生命周期服务
        }
        catch (Exception ex) { Logger.LogError(ex, "资源清理异常"); }
        finally { base.OnDisposing(); }
    }

    /// <summary>清理Tick订阅和用户活动追踪</summary>
    private void CleanupTickSubscription()
    {
        _tickService.Tick -= OnTick;
        _userActivityTracker.SessionExpiring -= OnSessionExpiring;
        _userActivityTracker.SessionExpired -= OnSessionExpired;
        _userActivityTracker.StopTracking();
    }

    /// <summary>取消登录事件订阅</summary>
    private void UnsubscribeLoginEvent()
    {
        try { _loginCoordinator.LoginSucceeded -= OnLoginCoordinatorSuccess; }
        catch (Exception ex) { Logger.LogError(ex, "取消LoginCoordinator事件订阅失败"); }
    }

    /// <summary>取消Token生命周期事件订阅</summary>
    private void UnsubscribeTokenLifecycleEvent()
    {
        try { EventAggregator.GetEvent<TokenLifecycleStateChangedEvent>().Unsubscribe(OnTokenLifecycleStateChanged); }
        catch (Exception ex) { Logger.LogError(ex, "取消TokenLifecycle事件订阅失败"); }
    }

    #endregion
}
