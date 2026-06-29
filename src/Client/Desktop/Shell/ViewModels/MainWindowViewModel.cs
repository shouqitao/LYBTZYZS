using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Controls.Models;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Services.Toast;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.HealthCheck;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Shell.ViewModels;

/// <summary>
/// 主窗口视图模型 - 用户登录状态管理、界面导航控制、键盘快捷键
/// </summary>
public partial class MainWindowViewModel : CoreViewModelBase
{
    #region 常量

    /// <summary>启动画面渲染等待时间（毫秒）</summary>
    private const int SplashRenderDelayMs = 500;

    #endregion

    #region 依赖服务

    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly MenuManager _menuManager;
    private readonly IActiveConsultationService _activeConsultationService;
    private readonly IApplicationTickService _tickService;
    private readonly IUserActivityTracker _userActivityTracker;
    private readonly ITokenLifecycleService _tokenLifecycleService;
    private readonly ILoginCoordinator _loginCoordinator;
    private readonly IThemeService _themeService;
    private readonly StatusBarManager _statusBarManager;
    private readonly NavigationManager _navigationManager;

    protected IRegionManager RegionManager { get; }
    protected ICommonDialogService? CommonDialogService { get; }
    protected IUserNotificationService? UserNotificationService { get; }
    protected IToastService? ToastService { get; }

    #endregion

    #region 可观察属性

    [ObservableProperty]
    private string _title = SystemConstants.SystemTitle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentUserInitial))]
    [NotifyPropertyChangedFor(nameof(CurrentUserRoleDisplay))]
    [NotifyPropertyChangedFor(nameof(CurrentUserDisplayName))]
    private UserDetailDto? _currentUser;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoggedIn))]
    [NotifyPropertyChangedFor(nameof(CurrentUserDisplayName))]
    private bool _isLoggedIn;

    [ObservableProperty]
    private double _sidebarWidth = 60;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavTextVisibility))]
    private bool _isSidebarExpanded = false;

    public Visibility NavTextVisibility =>
        IsSidebarExpanded ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    private bool _isDarkMode;

    partial void OnIsDarkModeChanged(bool value) => _themeService.ApplyTheme(value);

    #endregion

    #region 计算属性

    public bool IsNotLoggedIn => !IsLoggedIn;

    public string CurrentUserDisplayName =>
        IsLoggedIn && CurrentUser != null
            ? (string.IsNullOrEmpty(CurrentUser.RealName) ? CurrentUser.UserName : CurrentUser.RealName)
            : string.Empty;

    public bool IsUserManagementVisible => _menuManager.IsUserManagementVisible;
    public bool IsSystemSettingsVisible => _menuManager.IsSystemSettingsVisible;
    public bool IsPasswordChangeVisible => _menuManager.IsPasswordChangeVisible;

    public ICollectionView GroupedNavItems => _navigationManager.GroupedNavItems;
    public ObservableCollection<NavigationItem> NavigationItems => _navigationManager.NavigationItems;

    public NavigationItem? SelectedNavItem
    {
        get => _navigationManager.SelectedNavItem;
        set => _navigationManager.SelectedNavItem = value;
    }

    public string CurrentUserInitial =>
        CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.UserName)
            ? CurrentUser.UserName[..1].ToUpper()
            : "?";

    public string CurrentUserRoleDisplay =>
        CurrentUser?.Role switch
        {
            UserRole.SuperAdmin => "超级管理员",
            UserRole.Admin => "管理员",
            UserRole.Doctor => "医生",
            UserRole.Receptionist => "前台",
            _ => string.Empty
        };

    // 状态栏属性委托给 StatusBarManager
    public ApiHealthStatus ApiStatus => _statusBarManager.ApiStatus;
    public string ConnectionUrl => _statusBarManager.ConnectionUrl;
    public bool IsLocal => _statusBarManager.IsLocal;
    public string ConnectionModeDisplay => _statusBarManager.ConnectionModeDisplay;
    public bool IsRemoteMode => _statusBarManager.IsRemoteMode;
    public string CurrentTimeDisplay => _statusBarManager.CurrentTimeDisplay;
    public PackIconKind ApiStatusIcon => _statusBarManager.ApiStatusIcon;
    public Brush ApiStatusColor => _statusBarManager.ApiStatusColor;

    #endregion

    #region 构造函数

    public MainWindowViewModel(
        IViewModelServices services,
        IUserNotificationService userNotificationService,
        INavigationCoordinator navigationCoordinator,
        MenuManager menuManager,
        IActiveConsultationService activeConsultationService,
        IApplicationTickService tickService,
        IUserActivityTracker userActivityTracker,
        ITokenLifecycleService tokenLifecycleService,
        ILoginCoordinator loginCoordinator,
        IThemeService themeService,
        StatusBarManager statusBarManager,
        NavigationManager navigationManager)
        : base(services)
    {
        RegionManager = services.RegionManager;
        CommonDialogService = services.CommonDialogService;
        ToastService = services.ToastService;
        UserNotificationService = userNotificationService;

        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
        _activeConsultationService = activeConsultationService ?? throw new ArgumentNullException(nameof(activeConsultationService));
        _tickService = tickService ?? throw new ArgumentNullException(nameof(tickService));
        _userActivityTracker = userActivityTracker ?? throw new ArgumentNullException(nameof(userActivityTracker));
        _tokenLifecycleService = tokenLifecycleService ?? throw new ArgumentNullException(nameof(tokenLifecycleService));
        _loginCoordinator = loginCoordinator ?? throw new ArgumentNullException(nameof(loginCoordinator));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _statusBarManager = statusBarManager ?? throw new ArgumentNullException(nameof(statusBarManager));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));

        InitializeViewModel();
    }

    #endregion

    #region 委托命令属性

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
    /// 账户设置命令</summary>
    public ICommand EditProfileCommand => _menuManager.EditProfileCommand;

    /// <summary>
    /// 导航到主页命令</summary>
    public ICommand NavigateToHomeCommand => _menuManager.NavigateToHomeCommand;

    /// <summary>
    /// 导航到系统设置命令</summary>
    public ICommand NavigateToSystemSettingsCommand => _menuManager.NavigateToSystemSettingsCommand;

    /// <summary>
    /// 导航后退命令属性
    /// </summary>
    public ICommand NavigateBackCommand => _menuManager.NavigateBackCommand;

    /// <summary>
    /// 导航前进命令属性
    /// </summary>
    public ICommand NavigateForwardCommand => _menuManager.NavigateForwardCommand;

    /// <summary>
    /// 显示导航历史命令 (Ctrl+Shift+H) — Phase 2-3
    /// </summary>
    public ICommand ShowHistoryCommand => _menuManager.ShowHistoryCommand;

    /// <summary>
    /// 循环切换区域焦点命令 (F6) — Phase 2-3
    /// </summary>
    public ICommand CycleRegionsCommand => _menuManager.CycleRegionsCommand;

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
        await _statusBarManager.ForceCheckAsync();
    }

    /// <summary>
    /// 侧边栏展开/折叠时自动更新宽度
    /// </summary>
    partial void OnIsSidebarExpandedChanged(bool value)
    {
        SidebarWidth = value ? 140 : 60;
    }

    /// <summary>
    /// 切换侧边栏展开/折叠命令 (Ctrl+M)
    /// </summary>
    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    #endregion

    #region 初始化

    private void InitializeViewModel()
    {
        _tickService.Tick += OnTick;
        _tickService.Start();
        _userActivityTracker.SessionExpired += OnSessionExpired;

        _loginCoordinator.LoginSucceeded += OnLoginCoordinatorSuccess;
        Events.Subscribe<AuthEvents.PasswordChangedEvent, PasswordChangedPayload>(OnPasswordChanged);
        Events.Subscribe<AuthEvents.ProfileUpdatedEvent, ProfileUpdatedPayload>(OnProfileUpdated);
        Events.Subscribe<TokenLifecycleStateChangedEvent, TokenLifecycleStateChangedEventArgs>(
            args => OnTokenLifecycleStateChangedAsync(args).SafeFireAndForget(ex => Logger.LogError(ex, "Token生命周期事件处理异常")));
    }

    #endregion

    #region 事件处理

    private void OnTick(object? sender, ApplicationTickEventArgs e)
    {
        _statusBarManager.UpdateTime();
    }

    /// <summary>
    /// 会话已过期事件处理 - 执行自动登出
    /// </summary>
    private void OnSessionExpired(object? sender, EventArgs e)
    {
        OnSessionExpiredAsync().SafeFireAndForget(ex => Logger.LogError(ex, "会话过期处理异常"));
    }

    private async Task OnSessionExpiredAsync()
    {
        Logger.LogWarning("用户会话因不活跃已过期，执行自动登出");

        await Services.UiThreadDispatcher.InvokeAsync(async () =>
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
    private async Task OnTokenLifecycleStateChangedAsync(TokenLifecycleStateChangedEventArgs args)
    {
        Logger.LogDebug("Token生命周期状态变更: {Previous} -> {Current}", args.PreviousState, args.CurrentState);

        await Services.UiThreadDispatcher.InvokeAsync(async () =>
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

    private void OnLoginCoordinatorSuccess(object? sender, LoginSuccessEventArgs args)
    {
        var user = args.User;

        Services.UiThreadDispatcher.InvokeAsync(() =>
        {
            IsLoggedIn = true;
            CurrentUser = user;

            bool isAdmin = user.UserName?.Equals(SystemConstants.SuperAdminUsername, StringComparison.OrdinalIgnoreCase) == true
                           || user.Role == UserRole.Admin;
            var userDisplayName = string.IsNullOrEmpty(user.RealName) ? user.UserName : user.RealName;
            Title = $"凌隐宝堂中医诊所诊疗系统 - {userDisplayName} ({(isAdmin ? "管理员" : "医生")})";

            _navigationCoordinator.ClearLoginRegion();
            _userActivityTracker.StartTracking();
            _ = _tokenLifecycleService.StartMonitoringFromStorageAsync();

            _menuManager.RefreshMenuVisibility();
            OnPropertyChanged(nameof(IsUserManagementVisible));
            OnPropertyChanged(nameof(IsSystemSettingsVisible));
            OnPropertyChanged(nameof(IsPasswordChangeVisible));

            _navigationManager.BuildNavigationItems(user.Role);

            Logger.LogInformation("登录成功UI更新完成 [用户: {Username}]", user.UserName);
        });
    }

    /// <summary>
    /// 密码修改成功事件处理
    /// </summary>
    private void OnPasswordChanged(PasswordChangedPayload payload)
    {
        Logger.LogInformation("收到密码修改成功事件 [用户: {UserName}]，导航到登录界面", payload.UserName);
        Services.UiThreadDispatcher.InvokeAsync(() =>
        {
            CurrentUser = null;
            IsLoggedIn = false;
            Title = "凌隐宝堂中医诊所诊疗系统";
            _navigationCoordinator.ClearContentRegion();
            _navigationCoordinator.ShowLoginDialog();
        });
    }

    /// <summary>
    /// 用户资料更新事件处理 - 同步 CurrentUser
    /// </summary>
    private void OnProfileUpdated(ProfileUpdatedPayload payload)
    {
        Services.UiThreadDispatcher.InvokeAsync(() =>
        {
            CurrentUser = payload.UpdatedUser;
            Logger.LogInformation("已同步用户资料更新 [用户: {UserName}]", payload.UpdatedUser.UserName);
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
    private async Task PerformLogoutAsync()
    {
        _userActivityTracker.StopTracking();
        _tokenLifecycleService.Reset(); // Issue #1864: 重置Token生命周期
        CurrentUser = null;
        IsLoggedIn = false;
        Title = "凌隐宝堂中医诊所诊疗系统";
        NavigationItems.Clear(); // UI Redesign: 清空导航项

        try
        {
            await _loginCoordinator.LogoutAsync();
            EventAggregator.GetEvent<AuthEvents.LogoutCompletedEvent>().Publish(new LogoutCompletedPayload
            {
                LocalLogoutCompleted = true,
                ServerLogoutCompleted = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "登出处理异常");
        }
        
        // 登出完成后清除导航历史并导航到登录页
        _navigationCoordinator.ClearHistory();
        _navigationCoordinator.ClearContentRegion();
        _navigationCoordinator.ShowLoginDialog();
    }

    /// <summary>
    /// 检查登录状态
    /// </summary>
    private async Task CheckLoginStatusAsync()
    {
        try
        {
            _navigationCoordinator.ShowLoginDialog();
        }
        catch (Exception ex)
        {
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("初始化登录界面", ex));
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 窗口加载完成回调
    /// </summary>
    public async Task OnWindowLoadedAsync()
    {
        await Task.Delay(SplashRenderDelayMs);
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
    /// Phase 2.2: 使用ToastService替代CommonDialogService（非阻塞通知）
    /// </summary>
    protected virtual async Task ShowSuccessMessageAsync(string message)
    {
        if (ToastService != null)
        {
            await Task.Run(() => ToastService.ShowSuccess(message));
            return;
        }
        Logger.LogWarning("ToastService不可用，成功消息未显示: {Message}", message);
    }

    /// <summary>
    /// 显示错误消息
    /// Phase 2.2: 使用ToastService替代CommonDialogService（非阻塞通知）
    /// </summary>
    protected virtual async Task ShowErrorMessageAsync(string message)
    {
        if (ToastService != null)
        {
            await Task.Run(() => ToastService.ShowError(message));
            return;
        }
        Logger.LogError("ToastService不可用，错误消息未显示: {Message}", message);
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

    protected override void OnDisposing()
    {
        try
        {
            _tickService.Tick -= OnTick;
            _userActivityTracker.SessionExpired -= OnSessionExpired;
            _userActivityTracker.StopTracking();
            _loginCoordinator.LoginSucceeded -= OnLoginCoordinatorSuccess;
            _statusBarManager.Dispose();
            _tokenLifecycleService.Dispose();
        }
        catch (Exception ex) { Logger.LogError(ex, "资源清理异常"); }
        finally { base.OnDisposing(); }
    }

    #endregion
}
