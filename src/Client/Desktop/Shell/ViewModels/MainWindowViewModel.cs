using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Modules;
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
    private readonly IRegionManager _regionManager;
    private readonly IModuleLoadingService _moduleLoadingService;
    private readonly IApiHealthCheckService _apiHealthCheckService;
    private readonly IRoleNavigationService _roleNavigationService;
    private readonly NavigationManager _navigationManager;
    private readonly MenuManager _menuManager;
    private readonly IActiveConsultationService _activeConsultationService;
    private readonly IApplicationTickService _tickService;
    private readonly IUserActivityTracker _userActivityTracker;
    private readonly IAuthenticationService _authenticationService;

    /// <summary>构造函数</summary>
    public MainWindowViewModel(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        IMainWindowServicesFacade servicesFacade,
        ILoggerFactory loggerFactory,
        LYBT.Desktop.Infrastructure.Interfaces.IUserNotificationService userNotificationService,
        IModuleLoadingService moduleLoadingService,
        IApiHealthCheckService apiHealthCheckService,
        IRoleNavigationService roleNavigationService,
        NavigationManager navigationManager,
        MenuManager menuManager,
        IActiveConsultationService activeConsultationService,
        IApplicationTickService tickService,
        IUserActivityTracker userActivityTracker,
        IAuthenticationService authenticationService,
        ICommonDialogService commonDialogService)
        : base(eventAggregator, loggerFactory, regionManager, null, userNotificationService, commonDialogService)
    {
        _servicesFacade = servicesFacade ?? throw new ArgumentNullException(nameof(servicesFacade));
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _moduleLoadingService = moduleLoadingService ?? throw new ArgumentNullException(nameof(moduleLoadingService));
        _apiHealthCheckService = apiHealthCheckService ?? throw new ArgumentNullException(nameof(apiHealthCheckService));
        _roleNavigationService = roleNavigationService ?? throw new ArgumentNullException(nameof(roleNavigationService));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
        _activeConsultationService = activeConsultationService ?? throw new ArgumentNullException(nameof(activeConsultationService));
        _tickService = tickService ?? throw new ArgumentNullException(nameof(tickService));
        _userActivityTracker = userActivityTracker ?? throw new ArgumentNullException(nameof(userActivityTracker));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));

        InitializeViewModel();
    }

    private string _title = SystemConstants.SystemTitle;
    private UserDto? _currentUser;
    private bool _isLoggedIn = false;
    private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;
    private long _lastHealthCheckTick;
    private const int HealthCheckIntervalSeconds = 10;

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

    #region 命令属性

    /// <summary>退出登录命令</summary>
    public DelegateCommand LogoutCommand { get; set; } = null!;

    /// <summary>API测试命令</summary>
    public DelegateCommand TestApiCommand { get; set; } = null!;

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

    #endregion

    /// <summary>初始化核心命令</summary>
    private new void InitializeCommands()
    {
        LogoutCommand = new DelegateCommand(async () => await ExecuteLogoutAsync().ConfigureAwait(false));
        TestApiCommand = new DelegateCommand(async () => await ExecuteTestApiAsync().ConfigureAwait(false))
            .ObservesProperty(() => IsLoggedIn);
        RetryHealthCheckCommand = new DelegateCommand(async () => await ExecuteRetryHealthCheckAsync().ConfigureAwait(false));

        Logger.LogDebug("核心命令已初始化");
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
        EventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);
        EventAggregator.GetEvent<PasswordChangedEvent>().Subscribe(OnPasswordChanged);
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

    /// <summary>登录成功事件处理</summary>
    private void OnLoginSuccess(UserDto user)
    {
        IsLoggedIn = true;
        CurrentUser = user;
        _userActivityTracker.StartTracking();
        _ = Task.Run(async () =>
        {
            await EnsureWorkstationModulesLoaded(user);
            await Application.Current.Dispatcher.InvokeAsync(() => LoadMainContent());
        });
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

    /// <summary>加载主界面内容</summary>
    private void LoadMainContent()
    {
        if (CurrentUser == null) throw new InvalidOperationException("当前用户信息为空，无法加载主界面");

        string roleName = CurrentUser.Role.ToString();
        bool isAdmin = CurrentUser.UserName?.Equals(SystemConstants.SuperAdminUsername, StringComparison.OrdinalIgnoreCase) == true || CurrentUser.Role == UserRole.Admin;
        var userDisplayName = string.IsNullOrEmpty(CurrentUser.RealName) ? CurrentUser.UserName : CurrentUser.RealName;
        Title = $"凌隐宝堂中医诊所诊疗系统 - {userDisplayName} ({(isAdmin ? "管理员" : "医生")})";

        _navigationManager.ClearLoginRegion();
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try { _roleNavigationService.NavigateToRoleHome(roleName); }
            catch (Exception ex)
            {
                Logger.LogError(ex, "角色导航失败");
                IsLoggedIn = false; CurrentUser = null; Title = "凌隐宝堂中医诊所诊疗系统";
                _ = Task.Run(async () => { await ShowErrorMessageAsync($"无法加载工作台：{ex.Message}"); await Application.Current.Dispatcher.InvokeAsync(() => _navigationManager.ShowLoginDialog()); });
            }
        }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    /// <summary>确保工作台模块已加载</summary>
    private async Task EnsureWorkstationModulesLoaded(UserDto user)
    {
        try
        {
            bool isAdmin = user.UserName?.Equals(SystemConstants.SuperAdminUsername, StringComparison.OrdinalIgnoreCase) == true ||
                           user.Role == UserRole.Admin;

            await LoadBasicModulesAsync();

            if (isAdmin)
            {
                Logger.LogInformation("管理员登录，加载管理工作台模块");
                await LoadAdminModulesAsync();
            }
            else if (user.Role == UserRole.Doctor)
            {
                Logger.LogInformation("医生登录，加载诊疗模块");
                await LoadBasicModulesAsync();
            }

            Logger.LogInformation("角色模块加载完成");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "工作台模块加载失败");
        }
    }

    /// <summary>加载基础模块</summary>
    private async Task LoadBasicModulesAsync()
    {
        await _moduleLoadingService.LoadModulesAsync(new[] { "PatientsModule" });
    }

    /// <summary>加载管理员模块</summary>
    private async Task LoadAdminModulesAsync()
    {
        await _moduleLoadingService.LoadModulesAsync(new[]
        {
            "UsersModule",
            "HerbsModule",
            "FormulaModule",
            "ConsultationModule",
            "MedicalCaseModule",
            "PrescriptionsModule"
        });
    }

    /// <summary>用户点击药材管理时触发</summary>
    public async Task LoadHerbsManagementAsync()
    {
        await _moduleLoadingService.LoadModuleAsync("HerbsModule");
    }

    /// <summary>用户点击方剂管理时触发</summary>
    public async Task LoadFormulaManagementAsync()
    {
        await _moduleLoadingService.LoadModuleAsync("FormulaModule");
    }

    /// <summary>执行API测试</summary>
    private async Task ExecuteTestApiAsync()
    {
        try
        {
            await ShowSuccessMessageAsync("API测试功能将在未来版本中实现");
        }
        catch (Exception ex)
        {
            await ShowErrorMessageAsync($"API测试失败: {ex.Message}");
        }
    }

    #region IDisposable

    /// <summary>重写OnDisposing方法，清理资源防止内存泄漏</summary>
    protected override void OnDisposing()
    {
        try { CleanupTickSubscription(); UnsubscribeLoginEvent(); _navigationManager.UnsubscribeFromRegionCollection(); }
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
        try { EventAggregator.GetEvent<LoginSuccessEvent>().Unsubscribe(OnLoginSuccess); }
        catch (Exception ex) { Logger.LogError(ex, "取消EventAggregator订阅失败"); }
    }

    #endregion
}
