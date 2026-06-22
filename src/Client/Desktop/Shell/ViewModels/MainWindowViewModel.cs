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
    #region 依赖服务

    private readonly IApiHealthMonitor _apiHealthMonitor;
    private readonly IApiRouter _apiRouter;
    private readonly IConnectionSettingsService _connectionSettings;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly MenuManager _menuManager;
    private readonly IActiveConsultationService _activeConsultationService;
    private readonly IApplicationTickService _tickService;
    private readonly IUserActivityTracker _userActivityTracker;
    private readonly ITokenLifecycleService _tokenLifecycleService;
    private readonly ILoginCoordinator _loginCoordinator;
    private readonly IConnectionModeService _connectionModeService;
    private readonly ThemeService _themeService;

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

    /// <summary>
    /// Toast消息服务 (Phase 2.2)
    /// </summary>
    protected IToastService? ToastService { get; }

    #endregion

    #region 可观察属性

    /// <summary>
    /// 窗口标题
    /// </summary>
    [ObservableProperty]
    private string _title = SystemConstants.SystemTitle;

    /// <summary>
    /// 当前登录用户
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentUserInitial))]
    [NotifyPropertyChangedFor(nameof(CurrentUserRoleDisplay))]
    [NotifyPropertyChangedFor(nameof(CurrentUserDisplayName))]
    private UserDetailDto? _currentUser;

    /// <summary>
    /// 用户登录状态
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoggedIn))]
    [NotifyPropertyChangedFor(nameof(CurrentUserDisplayName))]
    private bool _isLoggedIn;

    /// <summary>
    /// 当前系统时间
    /// </summary>
    [ObservableProperty]
    private DateTime _currentTime = DateTime.Now;

    /// <summary>
    /// 状态栏时间显示文本
    /// </summary>
    [ObservableProperty]
    private string _currentTimeDisplay = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// API健康状态
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ApiStatusIcon))]
    [NotifyPropertyChangedFor(nameof(ApiStatusColor))]
    private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;

    /// <summary>
    /// 当前连接地址
    /// </summary>
    [ObservableProperty]
    private string _connectionUrl = "http://127.0.0.1:5100";

    /// <summary>
    /// 是否连接本地服务
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoggedIn))]
    private bool _isLocal;

    /// <summary>
    /// 连接模式显示文本 (远程模式/本地模式) - 状态栏模式徽章
    /// </summary>
    [ObservableProperty]
    private string _connectionModeDisplay = string.Empty;

    /// <summary>
    /// 是否为远程模式 - 用于状态栏颜色编码 (true=绿色, false=橙色)
    /// </summary>
    [ObservableProperty]
    private bool _isRemoteMode;

    /// <summary>
    /// 连接地址变更命令（参数为新URL字符串）
    /// </summary>
    [RelayCommand]
    private async Task ConnectAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            await _connectionSettings.SetUrlAsync(url);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[UI] 连接切换失败: {Url}", url);
        }
    }

    /// <summary>
    /// 侧边栏宽度 (60=折叠/仅图标, 280=展开/图标+文字)
    /// </summary>
    [ObservableProperty]
    private double _sidebarWidth = 60;

    /// <summary>
    /// 侧边栏是否展开
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavTextVisibility))]
    private bool _isSidebarExpanded = false;

    /// <summary>
    /// 导航文字可见性 - 侧边栏折叠时隐藏文字
    /// </summary>
    public Visibility NavTextVisibility =>
        IsSidebarExpanded ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// 暗色模式开关
    /// </summary>
    [ObservableProperty]
    private bool _isDarkMode;

    /// <summary>
    /// 侧边栏导航项 - 根据当前用户角色构建 (UI Redesign 2026-06-21)
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<NavigationItem> _navigationItems = new();

    /// <summary>
    /// 当前选中的导航项 - 双向绑定到 Sidebar ListBox SelectedItem
    /// </summary>
    [ObservableProperty]
    private NavigationItem? _selectedNavItem;

    partial void OnSelectedNavItemChanged(NavigationItem? value)
    {
        if (value?.ViewName is string viewName && !string.IsNullOrEmpty(viewName))
        {
            _navigationCoordinator.NavigateTo(viewName);
        }
    }

    partial void OnIsDarkModeChanged(bool value) => _themeService.ToggleTheme();

    #endregion

    #region 计算属性

    /// <summary>
    /// 是否未登录状态，用于界面绑定
    /// </summary>
    public bool IsNotLoggedIn => !IsLoggedIn;

    /// <summary>
    /// 当前用户显示名称 - 状态栏使用
    /// 优先显示真实姓名，未设置则显示用户名
    /// US-SHELL-007 (CODE-21)
    /// </summary>
    public string CurrentUserDisplayName =>
        IsLoggedIn && CurrentUser != null
            ? (string.IsNullOrEmpty(CurrentUser.RealName) ? CurrentUser.UserName : CurrentUser.RealName)
            : string.Empty;

    /// <summary>S6-01: 用户管理菜单可见性 - 委托给 MenuManager</summary>
    public bool IsUserManagementVisible => _menuManager.IsUserManagementVisible;

    /// <summary>S6-01: 系统设置可见性 - 委托给 MenuManager</summary>
    public bool IsSystemSettingsVisible => _menuManager.IsSystemSettingsVisible;

    /// <summary>S6-04: 密码修改可见性 - 委托给 MenuManager</summary>
    public bool IsPasswordChangeVisible => _menuManager.IsPasswordChangeVisible;

    /// <summary>
    /// 分组后的导航项视图 - 供 Sidebar GroupStyle 绑定
    /// </summary>
    private ICollectionView? _groupedNavItems;

    public ICollectionView GroupedNavItems
    {
        get
        {
            if (_groupedNavItems == null)
            {
                _groupedNavItems = CollectionViewSource.GetDefaultView(NavigationItems);
                _groupedNavItems.GroupDescriptions.Add(new PropertyGroupDescription(nameof(NavigationItem.Group)));
            }
            return _groupedNavItems;
        }
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

    public PackIconKind ApiStatusIcon => ApiStatus switch
    {
        ApiHealthStatus.Healthy => PackIconKind.Wifi,
        ApiHealthStatus.Unhealthy => PackIconKind.WifiOff,
        _ => PackIconKind.WifiStrengthAlertOutline
    };

    public Brush ApiStatusColor => ApiStatus switch
    {
        ApiHealthStatus.Healthy => Brushes.Green,
        ApiHealthStatus.Unhealthy => Brushes.Orange,
        _ => Brushes.Gray
    };

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    public MainWindowViewModel(
        IViewModelServices services,
        IUserNotificationService userNotificationService,
        IApiHealthMonitor apiHealthMonitor,
        IApiRouter apiRouter,
        IConnectionSettingsService connectionSettings,
        INavigationCoordinator navigationCoordinator,
        MenuManager menuManager,
        IActiveConsultationService activeConsultationService,
        IApplicationTickService tickService,
        IUserActivityTracker userActivityTracker,
        ITokenLifecycleService tokenLifecycleService,
        ILoginCoordinator loginCoordinator,
        IConnectionModeService connectionModeService,
        ThemeService themeService)
        : base(services)
    {
        RegionManager = services.RegionManager;
        CommonDialogService = services.CommonDialogService;
        ToastService = services.ToastService;
        UserNotificationService = userNotificationService;

        _apiHealthMonitor = apiHealthMonitor ?? throw new ArgumentNullException(nameof(apiHealthMonitor));
        _apiRouter = apiRouter ?? throw new ArgumentNullException(nameof(apiRouter));
        _connectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
        _activeConsultationService = activeConsultationService ?? throw new ArgumentNullException(nameof(activeConsultationService));
        _tickService = tickService ?? throw new ArgumentNullException(nameof(tickService));
        _userActivityTracker = userActivityTracker ?? throw new ArgumentNullException(nameof(userActivityTracker));
        _tokenLifecycleService = tokenLifecycleService ?? throw new ArgumentNullException(nameof(tokenLifecycleService));
        _loginCoordinator = loginCoordinator ?? throw new ArgumentNullException(nameof(loginCoordinator));
        _connectionModeService = connectionModeService ?? throw new ArgumentNullException(nameof(connectionModeService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

        ConnectionUrl = _connectionSettings.CurrentUrl;
        IsLocal = _connectionSettings.IsLocal;

        // 初始化连接模式显示
        ConnectionModeDisplay = _connectionModeService.CurrentModeDisplay;
        IsRemoteMode = _connectionModeService.IsRemote;
        _connectionModeService.ModeChanged += OnConnectionModeChanged;

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
    /// 导航后退命令 — 导航架构改进方案 v1.0
    /// </summary>
    [ObservableProperty]
    private bool _canNavigateBack;

    /// <summary>
    /// 导航前进命令 — 导航架构改进方案 v1.0
    /// </summary>
    [ObservableProperty]
    private bool _canNavigateForward;

    /// <summary>
    /// 当前面包屑列表 — 导航架构改进方案 v1.0
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<LYBT.Desktop.Shared.UI.BreadcrumbItem> _breadcrumbs
        = Array.Empty<LYBT.Desktop.Shared.UI.BreadcrumbItem>();

    /// <summary>
    /// 导航后退命令属性
    /// </summary>
    public ICommand NavigateBackCommand => _menuManager.NavigateBackCommand;

    /// <summary>
    /// 导航前进命令属性
    /// </summary>
    public ICommand NavigateForwardCommand => _menuManager.NavigateForwardCommand;

    /// <summary>
    /// 面包屑跳转命令属性
    /// </summary>
    public ICommand NavigateToBreadcrumbCommand => _menuManager.NavigateToBreadcrumbCommand;

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
        Logger.LogInformation("用户手动触发 API 健康检查");
        await _apiHealthMonitor.ForceCheckAsync();
    }

    /// <summary>
    /// 切换侧边栏展开/折叠命令
    /// </summary>
    [RelayCommand]
    private void ToggleSidebar()
    {
        SidebarWidth = IsSidebarExpanded ? 60 : 280;
        IsSidebarExpanded = !IsSidebarExpanded;
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
        _userActivityTracker.SessionExpired += OnSessionExpired;
    }

    /// <summary>
    /// 初始化API健康检查
    /// </summary>
    private void InitializeHealthCheck()
    {
        _apiHealthMonitor.StatusChanged += OnHealthStatusChanged;
        _apiHealthMonitor.StartMonitoringAsync().SafeFireAndForget(ex => Logger.LogError(ex, "启动健康监控失败"));
        _connectionSettings.UrlChanged += OnConnectionUrlChanged;
    }

    /// <summary>
    /// 初始化事件订阅
    /// </summary>
    private void InitializeEvents()
    {
        // 订阅LoginCoordinator的登录成功事件（取代EventAggregator的LoginSuccessEvent）
        _loginCoordinator.LoginSucceeded += OnLoginCoordinatorSuccess;
        Events.Subscribe<AuthEvents.PasswordChangedEvent, PasswordChangedPayload>(OnPasswordChanged);
        Events.Subscribe<AuthEvents.ProfileUpdatedEvent, ProfileUpdatedPayload>(OnProfileUpdated);
        Events.Subscribe<TokenLifecycleStateChangedEvent, TokenLifecycleStateChangedEventArgs>(
            args => OnTokenLifecycleStateChangedAsync(args).SafeFireAndForget(ex => Logger.LogError(ex, "Token生命周期事件处理异常")));
        _navigationCoordinator.SubscribeToRegionCollection();

        // 导航架构改进方案 v1.0 — 订阅导航变更事件，更新面包屑和按钮状态
        _navigationCoordinator.NavigationChanged += OnNavigationStateChanged;
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 统一Tick处理 - 时钟更新
    /// </summary>
    private void OnTick(object? sender, ApplicationTickEventArgs e)
    {
        // UI线程更新时间显示（避免应用关闭时空引用）
        Services.UiThreadDispatcher.InvokeAsync(() =>
        {
            CurrentTime = DateTime.Now;
            CurrentTimeDisplay = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        });
    }

    /// <summary>
    /// 健康状态变更事件处理
    /// </summary>
    private void OnHealthStatusChanged(object? sender, ApiHealthMonitorChangedEventArgs e)
    {
        var apiStatus = e.NewStatus switch
        {
            ApiMonitorHealthStatus.Healthy => ApiHealthStatus.Healthy,
            ApiMonitorHealthStatus.Unhealthy => ApiHealthStatus.Unhealthy,
            _ => ApiHealthStatus.Checking
        };
        Services.UiThreadDispatcher.InvokeAsync(() => ApiStatus = apiStatus);
    }

    /// <summary>
    /// 连接地址变更事件处理
    /// </summary>
    private void OnConnectionUrlChanged(object? sender, string newUrl)
    {
        Services.UiThreadDispatcher.InvokeAsync(() =>
        {
            ConnectionUrl = newUrl;
            IsLocal = _connectionSettings.IsLocal;
            Logger.LogInformation("[UI] 连接地址变更: {Url}", newUrl);
        });
    }

    /// <summary>
    /// 连接模式变更事件处理 - 更新状态栏模式徽章
    /// </summary>
    private void OnConnectionModeChanged(object? sender, ConnectionMode e)
    {
        Services.UiThreadDispatcher.InvokeAsync(() =>
        {
            ConnectionModeDisplay = _connectionModeService.CurrentModeDisplay;
            IsRemoteMode = _connectionModeService.IsRemote;
            Logger.LogInformation("[UI] 连接模式变更: {Mode} ({Display})", e, _connectionModeService.CurrentModeDisplay);
        });
    }

    /// <summary>
    /// 导航架构改进方案 v1.0 — 导航状态变更事件处理
    /// 更新面包屑列表和后退/前进按钮状态
    /// </summary>
    private void OnNavigationStateChanged(object? sender, NavigationChangedEventArgs e)
    {
        Services.UiThreadDispatcher.InvokeAsync(() =>
        {
            Breadcrumbs = _navigationCoordinator.Breadcrumbs;
            CanNavigateBack = _navigationCoordinator.CanNavigateBack;
            CanNavigateForward = _navigationCoordinator.CanNavigateForward;

            // 刷新命令可执行状态
            _menuManager.RefreshNavigationCanExecute();

            Logger.LogDebug("导航状态已更新: 面包屑{Count}项, 可后退={Back}, 可前进={Forward}",
                Breadcrumbs.Count, CanNavigateBack, CanNavigateForward);
        });
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

    /// <summary>
    /// LoginCoordinator登录成功事件处理
    /// 负责更新UI状态（LoginCoordinator已处理模块加载和导航）
    /// </summary>
    private void OnLoginCoordinatorSuccess(object? sender, LoginSuccessEventArgs args)
    {
        var user = args.User;

        Services.UiThreadDispatcher.InvokeAsync(() =>
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
            _ = _tokenLifecycleService.StartMonitoringFromStorageAsync();

            // S6-01/S6-02: 刷新菜单可见性
            _menuManager.RefreshMenuVisibility();
            OnPropertyChanged(nameof(IsUserManagementVisible));
            OnPropertyChanged(nameof(IsSystemSettingsVisible));
            OnPropertyChanged(nameof(IsPasswordChangeVisible));

            // UI Redesign 2026-06-21: 构建角色自适应侧边栏导航项
            NavigationItems = BuildNavigationItems(user.Role);

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
    /// 根据用户角色构建侧边栏导航项 (UI Redesign 2026-06-21)
    /// </summary>
    private ObservableCollection<NavigationItem> BuildNavigationItems(UserRole role)
    {
        var definition = Services.RoleRegistry.GetDefinition(role);
        var items = new ObservableCollection<NavigationItem>();

        if (definition == null)
        {
            Logger.LogWarning("无法为角色 {Role} 找到定义，导航项为空", role);
            return items;
        }

        var modules = definition.RequiredModules;

        // 主页
        items.Add(new NavigationItem
        {
            Title = "主页",
            ViewName = definition.HomeViewName,
            IconKind = "Home",
            Command = new RelayCommand(() => _navigationCoordinator.NavigateTo(definition.HomeViewName)),
            Group = "主页"
        });

        // 业务组
        if (modules.Contains("PatientsModule"))
            items.Add(CreateNavItem("患者管理", ViewNames.PatientManagement, "AccountGroup", "业务"));
        if (modules.Contains("HerbsModule"))
            items.Add(CreateNavItem("药材管理", ViewNames.HerbManagement, "Leaf", "业务"));
        if (modules.Contains("FormulaModule"))
            items.Add(CreateNavItem("验方管理", ViewNames.FormulaManagement, "Notebook", "业务"));
        if (modules.Contains("MedicalCaseModule"))
            items.Add(CreateNavItem("医案管理", ViewNames.MedicalCaseManagement, "Folder", "业务"));
        if (modules.Contains("RegistrationModule"))
            items.Add(CreateNavItem("挂号管理", ViewNames.RegistrationList, "CalendarClock", "业务"));

        // 管理组
        if (modules.Contains("UsersModule") && role is UserRole.Admin or UserRole.SuperAdmin)
            items.Add(CreateNavItem("用户管理", ViewNames.UserManagement, "AccountTie", "管理"));
        if (definition.GetAllModules().Contains("ReportsModule"))
            items.Add(CreateNavItem("统计报表", ViewNames.ReportsHome, "ChartBar", "管理"));

        Logger.LogInformation("已为角色 {Role} 构建 {Count} 个导航项", role, items.Count);
        return items;
    }

    private NavigationItem CreateNavItem(string title, string viewName, string iconKind, string group = "业务") =>
        new()
        {
            Title = title,
            ViewName = viewName,
            IconKind = iconKind,
            Command = new RelayCommand(() => _navigationCoordinator.NavigateTo(viewName)),
            Group = group
        };

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

    /// <summary>
    /// 重写OnDisposing方法，清理资源防止内存泄漏
    /// </summary>
    protected override void OnDisposing()
    {
        try
        {
            CleanupTickSubscription();
            CleanupHealthMonitor();
            CleanupConnectionMode();
            UnsubscribeLoginEvent();
            _navigationCoordinator.UnsubscribeFromRegionCollection();
            _tokenLifecycleService.Dispose(); // Issue #1864: 释放Token生命周期服务
        }
        catch (Exception ex) { Logger.LogError(ex, "资源清理异常"); }
        finally { base.OnDisposing(); }
    }

    /// <summary>
    /// 清理连接模式服务订阅
    /// </summary>
    private void CleanupConnectionMode()
    {
        try
        {
            _connectionModeService.ModeChanged -= OnConnectionModeChanged;
        }
        catch (Exception ex) { Logger.LogError(ex, "清理连接模式服务订阅失败"); }
    }

    /// <summary>
    /// 清理Tick订阅和用户活动追踪
    /// </summary>
    private void CleanupTickSubscription()
    {
        _tickService.Tick -= OnTick;
        _userActivityTracker.SessionExpired -= OnSessionExpired;
        _userActivityTracker.StopTracking();
    }

    /// <summary>
    /// 清理健康检查协调器订阅
    /// </summary>
    private void CleanupHealthMonitor()
    {
        try
        {
            _apiHealthMonitor.StatusChanged -= OnHealthStatusChanged;
            _apiHealthMonitor.Dispose();
            _connectionSettings.UrlChanged -= OnConnectionUrlChanged;
            if (_apiRouter is IDisposable routerDisposable) routerDisposable.Dispose();
        }
        catch (Exception ex) { Logger.LogError(ex, "清理健康监控器失败"); }
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
