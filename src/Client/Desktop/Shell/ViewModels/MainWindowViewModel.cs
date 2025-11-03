using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Modules;
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

/// <summary>
/// 主窗口视图模型 - WPF主界面核心控制器
/// Issue #1790: 拆分为3个文件，提取NavigationManager(~150行)和MenuManager(~100行)
/// 当前文件聚焦核心ViewModel逻辑（~350行）
/// 采用Prism 8.x最佳实践，使用构造函数注入模式
/// 提供用户登录状态管理、界面导航控制、键盘快捷键支持
/// 集成主题切换、时钟显示、角色基础的工作台切换功能
/// 支持企业级错误处理和异步操作，适配小型诊所使用需求
/// </summary>
public class MainWindowViewModel : UnifiedViewModelBase
{
    private readonly IMainWindowServicesFacade _servicesFacade;
    private readonly IRegionManager _regionManager;
    private readonly IModuleLoadingService _moduleLoadingService;
    private readonly IApiHealthCheckService _apiHealthCheckService;
    private readonly IRoleNavigationService _roleNavigationService;
    private readonly NavigationManager _navigationManager;
    private readonly MenuManager _menuManager;

    /// <summary>
    /// 构造函数 - Issue #1790: 注入NavigationManager和MenuManager
    /// </summary>
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
        MenuManager menuManager)
        : base(eventAggregator, loggerFactory, regionManager, null, userNotificationService)
    {
        _servicesFacade = servicesFacade ?? throw new ArgumentNullException(nameof(servicesFacade));
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _moduleLoadingService = moduleLoadingService ?? throw new ArgumentNullException(nameof(moduleLoadingService));
        _apiHealthCheckService = apiHealthCheckService ?? throw new ArgumentNullException(nameof(apiHealthCheckService));
        _roleNavigationService = roleNavigationService ?? throw new ArgumentNullException(nameof(roleNavigationService));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));

        InitializeViewModel();
    }

    // 私有字段
    private string _title = SystemConstants.SystemTitle;
    private UserDto? _currentUser;
    private bool _isLoggedIn = false;
    private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    private System.Windows.Threading.DispatcherTimer _clockTimer = null!;
    private System.Windows.Threading.DispatcherTimer _healthCheckTimer = null!;
    private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;

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
        set
        {
            System.Diagnostics.Debug.WriteLine($"🔐 MainWindow.IsLoggedIn设置为 {value} (之前: {_isLoggedIn})");
            SetProperty(ref _isLoggedIn, value);
            RaisePropertyChanged(nameof(IsNotLoggedIn));
        }
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

    #region 命令属性 - Issue #1790: 委托给MenuManager

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

    /// <summary>
    /// 初始化核心命令（登录/登出/API测试）
    /// Issue #1790: 其他命令已委托给MenuManager
    /// </summary>
    private new void InitializeCommands()
    {
        LogoutCommand = new DelegateCommand(async () => await ExecuteLogoutAsync().ConfigureAwait(false));
        TestApiCommand = new DelegateCommand(async () => await ExecuteTestApiAsync().ConfigureAwait(false))
            .ObservesProperty(() => IsLoggedIn);
        RetryHealthCheckCommand = new DelegateCommand(async () => await ExecuteRetryHealthCheckAsync().ConfigureAwait(false));

        Logger.LogDebug("核心命令已初始化");
    }

    /// <summary>
    /// 初始化时钟计时器
    /// </summary>
    private void InitializeClock()
    {
        _clockTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += OnClockTick;
        _clockTimer.Start();
    }

    /// <summary>
    /// 初始化 API 健康检查定时器
    /// </summary>
    private void InitializeHealthCheck()
    {
        _healthCheckTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _healthCheckTimer.Tick += async (s, e) => await OnHealthCheckTickAsync();
        _healthCheckTimer.Start();

        _ = Task.Run(async () => await OnHealthCheckTickAsync());
    }

    /// <summary>
    /// 健康检查定时器 Tick 事件处理
    /// </summary>
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

    /// <summary>
    /// 执行重试 API 健康检查
    /// </summary>
    private async Task ExecuteRetryHealthCheckAsync()
    {
        Logger.LogInformation("用户手动触发 API 健康检查");
        await OnHealthCheckTickAsync();
    }

    /// <summary>
    /// 初始化事件订阅
    /// Issue #1790: Region监控委托给NavigationManager
    /// </summary>
    private void InitializeEvents()
    {
        EventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);
        _navigationManager.SubscribeToRegionCollection();
    }

    /// <summary>
    /// 执行完整的ViewModel初始化
    /// </summary>
    private void InitializeViewModel()
    {
        InitializeClock();
        InitializeHealthCheck();
        InitializeCommands();
        InitializeEvents();
    }

    /// <summary>时钟计时器事件</summary>
    private void OnClockTick(object? sender, EventArgs e)
    {
        CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 退出登录命令执行
    /// Issue #1790: 使用NavigationManager处理导航
    /// </summary>
    private async Task ExecuteLogoutAsync()
    {
        var result = await ShowConfirmationAsync("确定要退出登录吗？");
        if (result)
        {
            try
            {
                // 立即更新UI状态
                CurrentUser = null;
                IsLoggedIn = false;
                Title = "凌隐宝堂中医诊所诊疗系统";

                // 清理界面并显示登录界面
                _navigationManager.ClearContentRegion();
                _navigationManager.ShowLoginDialog();

                // 后台异步处理
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _servicesFacade.AuthenticationService.LogoutAsync();
                        EventAggregator.GetEvent<LogoutEvent>().Publish(new LogoutEventArgs
                        {
                            Reason = LogoutReason.SessionTimeout,
                            Message = "Token已过期"
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"后台登出处理异常: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync($"退出登录失败:{ex.Message}");
            }
        }
    }

    /// <summary>
    /// 检查登录状态 - Issue #861: 始终显示登录窗口
    /// Issue #1790: 使用NavigationManager处理导航
    /// </summary>
    private async Task CheckLoginStatusAsync()
    {
        System.Diagnostics.Debug.WriteLine("📱 CheckLoginStatusAsync 开始 - Issue #861: 始终显示登录窗口");
        try
        {
            System.Diagnostics.Debug.WriteLine("📱 显示登录界面，等待用户手动登录");
            _navigationManager.ShowLoginDialog();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ CheckLoginStatusAsync 异常: {ex.Message}");
            await ShowErrorMessageAsync($"初始化登录界面失败:{ex.Message}");
            _navigationManager.ShowLoginDialog();
        }
    }

    /// <summary>
    /// 登录成功事件处理 - Issue #877
    /// </summary>
    private void OnLoginSuccess(UserDto user)
    {
        System.Diagnostics.Debug.WriteLine($"📢 OnLoginSuccess 收到事件: {user.UserName}");

        IsLoggedIn = true;
        CurrentUser = user;

        System.Diagnostics.Debug.WriteLine("✅ IsLoggedIn 已设置为 true，ContentRegion 应该已可见");

        _ = Task.Run(async () =>
        {
            await EnsureWorkstationModulesLoaded(user);
            await Application.Current.Dispatcher.InvokeAsync(() => LoadMainContent());
        });
    }

    /// <summary>
    /// 窗口加载完成回调 - UltraThink修复 Issue #856
    /// </summary>
    public async Task OnWindowLoadedAsync()
    {
        await Task.Delay(500);
        await CheckLoginStatusAsync();
    }

    /// <summary>
    /// 加载主界面内容 - Issue #1553 角色模块化重构
    /// </summary>
    private void LoadMainContent()
    {
        if (CurrentUser == null)
        {
            throw new InvalidOperationException("当前用户信息为空，无法加载主界面");
        }

        string roleName = CurrentUser.Role.ToString();
        bool isAdmin = CurrentUser.UserName?.Equals(SystemConstants.SuperAdminUsername, StringComparison.OrdinalIgnoreCase) == true ||
                       CurrentUser.Role == UserRole.Admin;
        string roleDisplay = isAdmin ? "管理员" : "医生";

        var userDisplayName = string.IsNullOrEmpty(CurrentUser.RealName) ? CurrentUser.UserName : CurrentUser.RealName;
        Title = $"凌隐宝堂中医诊所诊疗系统 - {userDisplayName} ({roleDisplay})";

        _navigationManager.ClearLoginRegion();

        System.Diagnostics.Debug.WriteLine($"📱 准备根据角色 {roleName} 导航（延迟到下一帧）");
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📱 UI更新完成，开始角色导航：{roleName}");
                _roleNavigationService.NavigateToRoleHome(roleName);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message ?? "未知导航错误";
                System.Diagnostics.Debug.WriteLine($"❌ 角色导航失败：{errorMessage}");
                Logger.LogError(ex, "角色导航失败");

                IsLoggedIn = false;
                CurrentUser = null;
                Title = "凌隐宝堂中医诊所诊疗系统";

                _ = Task.Run(async () =>
                {
                    await ShowErrorMessageAsync($"无法加载工作台：{errorMessage}");
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _navigationManager.ShowLoginDialog();
                    });
                });
            }
        }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    /// <summary>
    /// 确保工作台模块已加载
    /// </summary>
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

    /// <summary>
    /// 加载基础模块（患者管理等）
    /// </summary>
    private async Task LoadBasicModulesAsync()
    {
        await _moduleLoadingService.LoadModulesAsync(new[] { "PatientsModule" });
    }

    /// <summary>
    /// 加载管理员模块
    /// </summary>
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

    /// <summary>
    /// 用户点击药材管理时触发
    /// </summary>
    public async Task LoadHerbsManagementAsync()
    {
        await _moduleLoadingService.LoadModuleAsync("HerbsModule");
    }

    /// <summary>
    /// 用户点击方剂管理时触发
    /// </summary>
    public async Task LoadFormulaManagementAsync()
    {
        await _moduleLoadingService.LoadModuleAsync("FormulaModule");
    }

    /// <summary>
    /// 执行API测试
    /// </summary>
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

    #region IDisposable实现 - DT-013内存泄漏修复

    /// <summary>
    /// 重写OnDisposing方法，清理资源防止内存泄漏
    /// Issue #1790: NavigationManager清理委托给NavigationManager
    /// </summary>
    protected override void OnDisposing()
    {
        try
        {
            CleanupClockTimer();
            CleanupHealthCheckTimer();
            UnsubscribeLoginEvent();
            _navigationManager.UnsubscribeFromRegionCollection();

            System.Diagnostics.Debug.WriteLine("✅ [MainWindowViewModel] 资源清理完成 - 内存泄漏风险已消除");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [MainWindowViewModel] 资源清理异常: {ex.Message}");
        }
        finally
        {
            base.OnDisposing();
        }
    }

    /// <summary>
    /// 清理时钟定时器
    /// Issue #1794: 提取定时器清理逻辑
    /// </summary>
    private void CleanupClockTimer()
    {
        if (_clockTimer != null)
        {
            _clockTimer.Stop();
            _clockTimer.Tick -= OnClockTick;
            _clockTimer = null!;
            System.Diagnostics.Debug.WriteLine("✅ [MainWindowViewModel] DispatcherTimer已清理");
        }
    }

    /// <summary>
    /// 清理健康检查定时器
    /// Issue #1794: 提取定时器清理逻辑
    /// </summary>
    private void CleanupHealthCheckTimer()
    {
        if (_healthCheckTimer != null)
        {
            _healthCheckTimer.Stop();
            _healthCheckTimer = null!;
            System.Diagnostics.Debug.WriteLine("✅ [MainWindowViewModel] 健康检查定时器已清理");
        }
    }

    /// <summary>
    /// 取消登录事件订阅
    /// Issue #1794: 提取事件取消订阅逻辑
    /// </summary>
    private void UnsubscribeLoginEvent()
    {
        try
        {
            EventAggregator.GetEvent<LoginSuccessEvent>().Unsubscribe(OnLoginSuccess);
            System.Diagnostics.Debug.WriteLine("✅ [MainWindowViewModel] LoginSuccessEvent订阅已取消");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [MainWindowViewModel] 取消EventAggregator订阅失败: {ex.Message}");
        }
    }

    #endregion
}
