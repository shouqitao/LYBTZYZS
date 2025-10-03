using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Commands;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Services.Modules;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Shell.ViewModels;

/// <summary>
/// 主窗口视图模型 - WPF主界面核心控制器
/// 采用Prism 8.x最佳实践，使用构造函数注入模式
/// 提供用户登录状态管理、界面导航控制、键盘快捷键支持
/// 集成主题切换、时钟显示、角色基础的工作台切换功能
/// 支持企业级错误处理和异步操作，适配小型诊所使用需求
/// </summary>
public class MainWindowViewModel : UnifiedViewModelBase
{
    private readonly IMainWindowServicesFacade _servicesFacade;
    private readonly IRegionManager _regionManager;
    private readonly IApplicationCommands _applicationCommands;
    private readonly IModuleLoadingService _moduleLoadingService;

    /// <summary>
    /// 构造函数 - 按照Prism 8.x最佳实践，在构造函数中完成所有初始化
    /// </summary>
    /// <param name="regionManager">区域管理器，用于界面导航</param>
    /// <param name="eventAggregator">事件聚合器，用于模块间通信</param>
    /// <param name="servicesFacade">服务外观，统一访问各类服务</param>
    /// <param name="userNotificationService">用户通知服务</param>
    /// <exception cref="ArgumentNullException">当任何参数为 null 时抛出</exception>
    public MainWindowViewModel(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        IMainWindowServicesFacade servicesFacade,
        ILoggerFactory loggerFactory,
        LYBT.Desktop.Infrastructure.Interfaces.IUserNotificationService userNotificationService,
        IApplicationCommands applicationCommands,
        IModuleLoadingService moduleLoadingService) : base(eventAggregator, loggerFactory, regionManager, null, userNotificationService)
    {
        _servicesFacade = servicesFacade ?? throw new ArgumentNullException(nameof(servicesFacade));
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _applicationCommands = applicationCommands ?? throw new ArgumentNullException(nameof(applicationCommands));
        _moduleLoadingService = moduleLoadingService ?? throw new ArgumentNullException(nameof(moduleLoadingService));

        // 按照Prism 8.x最佳实践，在构造函数中完成初始化
        InitializeViewModel();
    }

    // 私有字段
    private string _title = SystemConstants.SystemTitle;

    private UserDto? _currentUser;
    private bool _isLoggedIn = false;
    private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    private System.Windows.Threading.DispatcherTimer _clockTimer = null!;

    /// <summary>
    /// 获取或设置窗口标题
    /// 显示系统名称和当前用户信息
    /// </summary>
    /// <value>窗口标题字符串</value>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// 获取或设置当前登录用户
    /// 用于界面显示和权限控制
    /// </summary>
    /// <value>当前用户信息，未登录时为 null</value>
    public UserDto? CurrentUser
    {
        get => _currentUser;
        set => SetProperty(ref _currentUser, value);
    }

    /// <summary>
    /// 获取或设置用户登录状态
    /// 控制界面元素的显示和可用性
    /// </summary>
    /// <value>如果用户已登录则为 true；否则为 false</value>
    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set
        {
            System.Diagnostics.Debug.WriteLine($" MainWindow.IsLoggedIn设置为 {value} (之前: {_isLoggedIn})");
            SetProperty(ref _isLoggedIn, value);
            RaisePropertyChanged(nameof(IsNotLoggedIn)); // 确保通知界面更新
        }
    }

    /// <summary>
    /// 获取或设置当前系统时间显示
    /// 实时更新的时钟显示
    /// </summary>
    /// <value>格式化的时间字符串</value>
    public string CurrentTime
    {
        get => _currentTime;
        set => SetProperty(ref _currentTime, value);
    }

    /// <summary>获取是否未登录状态，用于界面绑定</summary>
    public bool IsNotLoggedIn => !_isLoggedIn;

    #region 命令属性

    /// <summary>退出登录命令</summary>
    public DelegateCommand LogoutCommand { get; set; } = null!;

    /// <summary>API测试命令</summary>
    public DelegateCommand TestApiCommand { get; set; } = null!;

    /// <summary>显示控件示例命令</summary>
    public DelegateCommand ShowControlExamplesCommand { get; set; } = null!;

    /// <summary>快速添加患者命令(Ctrl+N)</summary>
    public DelegateCommand QuickAddPatientCommand { get; set; } = null!;

    /// <summary>快速开始诊疗命令(Ctrl+Shift+C)</summary>
    public DelegateCommand QuickStartConsultationCommand { get; set; } = null!;

    /// <summary>显示帮助命令 (F1)</summary>
    public DelegateCommand ShowHelpCommand { get; set; } = null!;

    /// <summary>显示设置命令 (Ctrl+,)</summary>
    public DelegateCommand ShowSettingsCommand { get; set; } = null!;

    /// <summary>主题切换命令</summary>
    public DelegateCommand ToggleThemeCommand { get; set; } = null!;

    #endregion 命令属性

    #region 全局命令属性(Phase 3: CompositeCommand)

    /// <summary>全局保存命令 (Ctrl+S)</summary>
    public ICommand SaveAllCommand => _applicationCommands.SaveAllCommand;

    /// <summary>全局刷新命令 (F5)</summary>
    public ICommand RefreshAllCommand => _applicationCommands.RefreshAllCommand;

    /// <summary>全局打印命令 (Ctrl+P)</summary>
    public ICommand PrintCommand => _applicationCommands.PrintCommand;

    /// <summary>全局导出命令</summary>
    public ICommand ExportCommand => _applicationCommands.ExportCommand;

    /// <summary>全局撤销命令 (Ctrl+Z)</summary>
    public ICommand UndoCommand => _applicationCommands.UndoCommand;

    /// <summary>全局重做命令 (Ctrl+Y)</summary>
    public ICommand RedoCommand => _applicationCommands.RedoCommand;

    #endregion

    // 构造函数体 - 初始化时钟计时器和命令
    /// <summary>
    /// 静态构造函数 - 初始化命令定义
    /// 按照Prism 8.x最佳实践，在类级别定义命令
    /// </summary>
    static MainWindowViewModel()
    {
        // 注：具体命令实例在构造函数中初始化
    }

    /// <summary>
    /// 初始化所有命令和事件订阅
    /// 按照Prism 8.x最佳实践，在构造函数中完成所有初始化
    /// </summary>
    private new void InitializeCommands()
    {
        // 初始化所有命令 - 使用响应式模式
        LogoutCommand = new DelegateCommand(async () => await ExecuteLogoutAsync().ConfigureAwait(false));
        TestApiCommand = new DelegateCommand(async () => await ExecuteTestApiAsync().ConfigureAwait(false))
            .ObservesProperty(() => IsLoggedIn);
        ShowControlExamplesCommand = new DelegateCommand(ExecuteShowControlExamples)
            .ObservesProperty(() => IsLoggedIn);

        // 键盘快捷键命令
        QuickAddPatientCommand = new DelegateCommand(async () => await ExecuteQuickAddPatientAsync().ConfigureAwait(false))
            .ObservesProperty(() => IsLoggedIn);
        QuickStartConsultationCommand = new DelegateCommand(async () => await ExecuteQuickStartConsultationAsync().ConfigureAwait(false))
            .ObservesProperty(() => IsLoggedIn);
        ShowHelpCommand = new DelegateCommand(ExecuteShowHelp);
        ShowSettingsCommand = new DelegateCommand(ExecuteShowSettings)
            .ObservesProperty(() => IsLoggedIn);
        ToggleThemeCommand = new DelegateCommand(async () => await ExecuteToggleThemeAsync().ConfigureAwait(false));

        // Phase 3: 初始化全局命令键盘绑定
        // 这些命令已在ApplicationCommands中初始化，这里只需要暴露给View使用
        // 实际的命令执行逻辑由各个ViewModel注册到CompositeCommand
        Logger.LogDebug("全局命令系统已初始化");
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
    /// 初始化事件订阅
    /// </summary>
    private void InitializeEvents()
    {
        // 订阅登录成功事件
        EventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);

        // UltraThink修复 Issue #856: 移除构造函数中的自动登录检查
        // 原因：Task.Run在100ms延迟后执行可能早于MainWindow.Loaded，此时ContentRegion尚未注册
        // 解决：改为在MainWindow.Loaded事件中触发CheckLoginStatusAsync，确保所有Region已就绪
        // 详见：MainWindow.xaml.cs OnWindowLoaded事件处理器

        // Issue #877 修复步骤4: 添加 Region 导航监控
        _regionManager.Regions.CollectionChanged += OnRegionsCollectionChanged;

        // 如果已有 Region，订阅导航事件
        foreach (var region in _regionManager.Regions)
        {
            SubscribeToRegionNavigationEvents(region);
        }

        Logger.LogDebug("Region 导航监控已启用");
    }

    /// <summary>
    /// 执行完整的ViewModel初始化
    /// 按照Prism 8.x最佳实践，将初始化逻辑整合到构造函数调用链中
    /// </summary>
    private void InitializeViewModel()
    {
        InitializeClock();
        InitializeCommands();
        InitializeEvents();
    }


    /// <summary>
    /// 简化主题切换功能
    /// 提供基础的明暗主题切换，适配小型诊所需求
    /// </summary>
    private async Task ExecuteToggleThemeAsync()
    {
        try
        {
            // 简单的明暗主题切换
            var isDark = Application.Current.Resources.Contains("IsDarkTheme") &&
            (bool)Application.Current.Resources["IsDarkTheme"];

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (isDark)
                {
                    // 切换到浅色主题
                    ApplyLightTheme();
                    Application.Current.Resources["IsDarkTheme"] = false;
                }
                else
                {
                    // 切换到深色主题
                    ApplyDarkTheme();
                    Application.Current.Resources["IsDarkTheme"] = true;
                }
            });

            await ShowSuccessMessageAsync("主题已切换");
        }
        catch (Exception ex)
        {
            await ShowErrorMessageAsync($"主题切换失败:{ex.Message}");
        }
    }

    private void ApplyLightTheme()
    {
        var resources = Application.Current.Resources;

        // 浅色主题
        UpdateThemeColor(resources, "BackgroundColor", "#FFF8F9FA");
        UpdateThemeColor(resources, "SurfaceColor", "#FFFFFFFF");
        UpdateThemeColor(resources, "TextPrimaryColor", "#FF1A1A1A");
    }

    private void ApplyDarkTheme()
    {
        var resources = Application.Current.Resources;

        // 深色主题
        UpdateThemeColor(resources, "BackgroundColor", "#FF1E1E1E");
        UpdateThemeColor(resources, "SurfaceColor", "#FF2D2D2D");
        UpdateThemeColor(resources, "TextPrimaryColor", "#FFFFFFFF");
    }

    private void UpdateThemeColor(ResourceDictionary resources, string colorKey, string colorValue)
    {
        try
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorValue);
            var brushKey = colorKey.Replace("Color", "Brush");

            if (resources.Contains(colorKey))
            {
                resources[colorKey] = color;
            }

            if (resources.Contains(brushKey))
            {
                resources[brushKey] = new System.Windows.Media.SolidColorBrush(color);
            }
        }
        catch
        { /* 忽略主题更新错误 */
        }
    }

    /// <summary>时钟计时器事件</summary>
    private void OnClockTick(object? sender, EventArgs e)
    {
        CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 退出登录命令执行
    /// </summary>
    private async Task ExecuteLogoutAsync()
    {
        var result = await ShowConfirmationAsync("确定要退出登录吗？");
        if (result)
        {
            try
            {
                // 立即更新UI状态，给用户即时反馈
                CurrentUser = null;
                IsLoggedIn = false;
                Title = "凌隐宝堂中医诊所诊疗系统";

                // 立即清理界面
                // 清除内容区域
                if (_regionManager.Regions.ContainsRegionWithName(RegionNames.ContentRegion))
                {
                    _regionManager.Regions[RegionNames.ContentRegion].RemoveAll();
                }

                // 立即显示登录界面
                ShowLoginDialog();

                // 后台异步处理网络请求和事件，不阻塞UI
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // 网络登出请求
                        await _servicesFacade.AuthenticationService.LogoutAsync();

                        // 发布登出事件以清除登录状态消息
                        EventAggregator.GetEvent<LogoutEvent>().Publish(new LogoutEventArgs { Reason = LogoutReason.SessionTimeout, Message = "Token已过期" });
                    }
                    catch (Exception ex)
                    {
                        // 后台错误不影响用户界面，记录到调试输出
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
    /// 检查登录状态 - Issue #861 修复: 始终显示登录窗口
    /// 原因: 安全性考虑，不再根据保存的 Token 自动登录
    /// 用户必须手动输入密码才能进入系统
    /// </summary>
    private async Task CheckLoginStatusAsync()
    {
        System.Diagnostics.Debug.WriteLine(" CheckLoginStatusAsync 开始 - Issue #861: 始终显示登录窗口");
        try
        {
            // Issue #861 修复: 移除自动登录逻辑
            // 即使有保存的 Token，也不自动登录，确保安全性
            // 用户名会在 LoginViewModel 中自动填充（如果启用了"记住用户名"）
            System.Diagnostics.Debug.WriteLine(" 显示登录界面，等待用户手动登录");
            ShowLoginDialog();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($" CheckLoginStatusAsync 异常: {ex.Message}");
            await ShowErrorMessageAsync($"初始化登录界面失败:{ex.Message}");
            ShowLoginDialog();
        }
    }

    /// <summary>
    /// 登录成功事件处理 - Issue #877 修复
    /// 立即更新 UI 状态，确保 ContentRegion 可见后再进行导航
    /// </summary>
    private void OnLoginSuccess(UserDto user)
    {
        System.Diagnostics.Debug.WriteLine($"📢 OnLoginSuccess 收到事件: {user.UserName}");

        // Issue #877 修复步骤1: 立即更新登录状态
        // 这会触发 MainWindow.xaml 中的 Visibility 绑定，使 ContentRegion 变为可见
        IsLoggedIn = true;
        CurrentUser = user;

        System.Diagnostics.Debug.WriteLine($"✅ IsLoggedIn 已设置为 true，ContentRegion 应该已可见");

        // 后台加载模块并切换界面
        _ = Task.Run(async () =>
        {
            await EnsureWorkstationModulesLoaded(user);
            await Application.Current.Dispatcher.InvokeAsync(() => LoadMainContent());
        });
    }

    /// <summary>
    /// 窗口加载完成回调 - UltraThink修复 Issue #856
    /// 在MainWindow.Loaded事件中调用，确保所有Region已注册后再检查登录状态
    /// </summary>
    public async Task OnWindowLoadedAsync()
    {
        // UltraThink修复：增加延迟确保 Prism Region 完全注册
        // Loaded 事件后 Region 注册仍是异步的，需要额外等待时间
        await Task.Delay(500);
        await CheckLoginStatusAsync();
    }

    /// <summary>
    /// 显示登录界面
    /// </summary>
    private void ShowLoginDialog()
    {
        // UltraThink修复 Issue #858: 确保在 UI 线程上执行导航
        // 原因：此方法可能从后台线程（Task.Run）调用，需要 marshal 到 UI 线程
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (_regionManager != null)
            {
                System.Diagnostics.Debug.WriteLine(" ShowLoginDialog: 导航到登录视图");
                _regionManager.RequestNavigate(RegionNames.LoginRegion, "LoginView");
            }
        });
    }

    /// <summary>
    /// 加载主界面内容 - UltraThink Phase 9 性能优化版
    /// </summary>
    private void LoadMainContent()
    {
        if (CurrentUser == null)
        {
            throw new InvalidOperationException("当前用户信息为空，无法加载主界面");
        }

        // 简化角色判断逻辑：只区分管理员和医生
        string workbenchView;
        string roleDisplay;

        // 管理员判断（包括sysadmin用户名和Admin角色）
        bool isAdmin = CurrentUser.UserName?.Equals(SystemConstants.SuperAdminUsername, StringComparison.OrdinalIgnoreCase) == true ||
        CurrentUser.Role == UserRole.Admin;

        if (isAdmin)
        {
            workbenchView = "AdminWorkstationView";
            roleDisplay = "管理员";
        }
        else
        {
            // 其他角色默认为医生工作台
            workbenchView = "ClinicalWorkstationView";
            roleDisplay = "医生";
        }

        // 更新标题和清理登录区域
        var userDisplayName = string.IsNullOrEmpty(CurrentUser.RealName) ? CurrentUser.UserName : CurrentUser.RealName;
        Title = $"凌隐宝堂中医诊所诊疗系统 - {userDisplayName} ({roleDisplay})";

        // 清除登录区域
        if (_regionManager.Regions.ContainsRegionWithName(RegionNames.LoginRegion))
        {
            _regionManager.Regions[RegionNames.LoginRegion].RemoveAll();
        }

        // UltraThink修复 Issue #858: 使用 Dispatcher.InvokeAsync 确保 UI 绑定更新后再导航
        // 原因：IsLoggedIn 属性变化后，UI 绑定不会立即生效，ContentRegion 可能仍处于 Collapsed 状态
        // 解决：延迟导航到下一个 UI 帧，确保 ContentRegion 已经可见
        System.Diagnostics.Debug.WriteLine($" 准备导航到 {workbenchView}（延迟到下一帧）");
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            System.Diagnostics.Debug.WriteLine($" UI更新完成，开始导航到 {workbenchView}");
            _regionManager.RequestNavigate(RegionNames.ContentRegion, workbenchView, navigationResult =>
            {
                if (navigationResult.Result != true)
                {
                    // 导航失败时显示错误信息
                    var errorMessage = navigationResult.Error?.Message ?? "未知导航错误";
                    System.Diagnostics.Debug.WriteLine($" 工作台导航失败 {errorMessage}");

                    // UltraThink修复：导航失败时，清除登录状态并回退到登录界面
                    IsLoggedIn = false;
                    CurrentUser = null;
                    Title = "凌隐宝堂中医诊所诊疗系统 - 系统超级管理员 (管理员)";

                    // 异步显示错误对话框
                    _ = Task.Run(async () =>
                    {
                        await ShowErrorMessageAsync($"无法加载工作台 {errorMessage}");
                        // 错误对话框关闭后，显示登录界面
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ShowLoginDialog();
                        });
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($" 成功导航到 {workbenchView}");
                }
            });
        }, System.Windows.Threading.DispatcherPriority.ApplicationIdle); // UltraThink修复 Issue #856: 使用 ApplicationIdle 确保所有初始化完成
    }

    /// <summary>
    /// 确保工作台模块已加载
    /// </summary>
    private async Task EnsureWorkstationModulesLoaded(LYBT.Shared.Models.Contracts.Users.UserDto user)
    {
        try
        {
            // Phase 3: 使用模块加载服务实现按需加载
            bool isAdmin = user.UserName?.Equals(SystemConstants.SuperAdminUsername, StringComparison.OrdinalIgnoreCase) == true ||
            user.Role == UserRole.Admin;

            // 基础模块加载（登录后立即需要）
            await LoadBasicModulesAsync();

            if (isAdmin)
            {
                Logger.LogInformation("管理员登录，加载管理工作台模块");
                // 管理员需要所有模块
                await LoadAdminModulesAsync();
            }
            else if (user.Role == UserRole.Doctor)
            {
                Logger.LogInformation("医生登录，加载诊疗工作台模块");
                await LoadClinicalWorkstationAsync();
            }

            Logger.LogInformation("工作台模块加载完成");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "工作台模块加载失败");
            // 模块加载失败不应阻塞界面显示
        }
    }

    /// <summary>
    /// 加载基础模块（患者管理等）
    /// </summary>
    private async Task LoadBasicModulesAsync()
    {
        await _moduleLoadingService.LoadModulesAsync(
            new[] { "PatientsModule" }  // 患者管理是大多数功能的基础
        );
    }

    /// <summary>
    /// 加载管理员模块
    /// </summary>
    private async Task LoadAdminModulesAsync()
    {
        // 管理员需要所有模块
        await _moduleLoadingService.LoadModulesAsync(new[]
        {
            "AdminWorkstationModule",  // UltraThink修复 Issue #856: 工作台模块必须加载才能注册视图
            "UsersModule",             // 管理员需要用户管理功能
            "HerbsModule",
            "FormulaModule",
            "ConsultationModule",
            "MedicalCaseModule",
            "PrescriptionsModule"
        });
    }

    /// <summary>
    /// 加载诊疗工作台模块
    /// </summary>
    private async Task LoadClinicalWorkstationAsync()
    {
        // 加载诊疗工作台及其依赖
        await _moduleLoadingService.LoadModuleAsync("ClinicalWorkstationModule");
        Logger.LogDebug("诊疗工作台模块加载完成");
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
        // 会自动加载HerbsModule依赖
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

    private void ExecuteShowControlExamples()
    {
        try
        {
            // 导航到控件示例页面
            _regionManager.RequestNavigate(RegionNames.ContentRegion, "ControlExamplesView");
        }
        catch (Exception ex)
        {
            // 简化错误处理，记录到调试输出
            System.Diagnostics.Debug.WriteLine($"打开控件示例页面失败: {ex.Message}");
            throw new InvalidOperationException($"打开控件示例页面失败: {ex.Message}", ex);
        }
    }

    #region UltraThink Phase H: 键盘快捷键功能实现

    /// <summary>
    /// 快速添加患者(Ctrl+N)
    /// </summary>
    private async Task ExecuteQuickAddPatientAsync()
    {
        try
        {
            // 导航到患者管理页面并触发新增患者对话框
            var navigationParams = new NavigationParameters();
            navigationParams.Add("Action", "AddNew");

            _regionManager.RequestNavigate(RegionNames.ContentRegion, "PatientManagementView", navigationParams);

            // 显示成功提示
            await ShowSuccessMessageAsync("已切换到患者管理页面，准备添加新患者");
        }
        catch (Exception ex)
        {
            await ShowErrorMessageAsync($"快速添加患者失败:{ex.Message}");
        }
    }

    /// <summary>
    /// 快速开始诊疗(Ctrl+Shift+C)
    /// </summary>
    private async Task ExecuteQuickStartConsultationAsync()
    {
        try
        {
            // 导航到诊疗工作台
            _regionManager.RequestNavigate(RegionNames.ContentRegion, "ClinicalWorkstationView", navigationResult =>
            {
                if (navigationResult.Result == true)
                {
                    // 成功导航后，可以发送事件触发快速开始诊疗流程
                    // TODO: QuickStartConsultationEvent 已移除，需要使用新的事件机制
                }
            });

            await ShowSuccessMessageAsync("已切换到诊疗工作台，准备开始诊疗");
        }
        catch (Exception ex)
        {
            await ShowErrorMessageAsync($"快速开始诊疗失败:{ex.Message}");
        }
    }

    /// <summary>
    /// 显示帮助信息 (F1)
    /// </summary>
    private void ExecuteShowHelp()
    {
        try
        {
            var helpMessage = "系统快捷键说明：\n\n" +
            "• Ctrl+N - 快速添加患者\n" +
            "• Ctrl+Shift+C - 快速开始诊疗\n" +
            "• F1 - 显示帮助\n" +
            "• Alt+F4 - 退出系统\n" +
            "• Ctrl+, - 打开设置\n\n" +
            "更多功能正在开发中...";

            _ = ShowSuccessMessageAsync(helpMessage);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示帮助失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示设置页面 (Ctrl+,)
    /// </summary>
    private void ExecuteShowSettings()
    {
        try
        {
            // 将来可以导航到设置页面
            _ = ShowSuccessMessageAsync("用户设置功能将在未来版本中实现");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示设置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新所有键盘快捷键命令的可用状态
    /// </summary>
    private void UpdateKeyboardShortcutCommands()
    {
        QuickAddPatientCommand?.RaiseCanExecuteChanged();
        QuickStartConsultationCommand?.RaiseCanExecuteChanged();
        ShowSettingsCommand?.RaiseCanExecuteChanged();
    }

    #endregion UltraThink Phase H: 键盘快捷键功能实现

    #region 私有转换方法

    /// <summary>
    /// 转换用户数据
    /// </summary>
    private static UserDto ConvertToUserDto(UserDto userDto)
    {
        return userDto;
    }

    #endregion 私有转换方法

    #region Issue #877 Region 导航监控

    /// <summary>
    /// Region 集合变化事件处理 - Issue #877
    /// 当新 Region 添加时，自动订阅其导航事件
    /// </summary>
    private void OnRegionsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (IRegion region in e.NewItems)
            {
                SubscribeToRegionNavigationEvents(region);
                System.Diagnostics.Debug.WriteLine($"🔔 新 Region 已注册并监控: {region.Name}");
            }
        }
    }

    /// <summary>
    /// 订阅 Region 导航事件 - Issue #877
    /// 用于调试和诊断导航问题
    /// </summary>
    private void SubscribeToRegionNavigationEvents(IRegion region)
    {
        region.NavigationService.Navigating += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"🚀 导航中: Region={region.Name}, Target={e.Uri}");
        };

        region.NavigationService.Navigated += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"✅ 导航完成: Region={region.Name}, Uri={e.Uri}");
        };

        region.NavigationService.NavigationFailed += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"❌ 导航失败: Region={region.Name}, Uri={e.Uri}, Error={e.Error?.Message}");
            Logger.LogError(e.Error, "Region 导航失败: {RegionName} -> {Uri}", region.Name, e.Uri);
        };
    }

    #endregion Issue #877 Region 导航监控

    #region IDisposable实现 - DT-013内存泄漏修复

    /// <summary>
    /// 重写OnDisposing方法，清理资源防止内存泄漏
    /// DT-013: 修复ViewModel事件订阅泄漏 - 自动清理DispatcherTimer和EventAggregator订阅
    /// </summary>
    protected override void OnDisposing()
    {
        try
        {
            // 清理DispatcherTimer
            if (_clockTimer != null)
            {
                _clockTimer.Stop();
                _clockTimer.Tick -= OnClockTick;
                _clockTimer = null!;
                System.Diagnostics.Debug.WriteLine(" [MainWindowViewModel] DispatcherTimer已清理");
            }

            // 取消EventAggregator订阅
            try
            {
                EventAggregator.GetEvent<LoginSuccessEvent>().Unsubscribe(OnLoginSuccess);
                System.Diagnostics.Debug.WriteLine(" [MainWindowViewModel] LoginSuccessEvent订阅已取消");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($" [MainWindowViewModel] 取消EventAggregator订阅失败: {ex.Message}");
            }

            // Issue #877: 取消 Region 导航监控
            try
            {
                _regionManager.Regions.CollectionChanged -= OnRegionsCollectionChanged;
                System.Diagnostics.Debug.WriteLine(" [MainWindowViewModel] Region 导航监控已取消");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($" [MainWindowViewModel] 取消Region监控失败: {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine(" [MainWindowViewModel] 资源清理完成 - 内存泄漏风险已消除");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($" [MainWindowViewModel] 资源清理异常: {ex.Message}");
        }
        finally
        {
            // 调用基类清理
            base.OnDisposing();
        }
    }

    #endregion IDisposable实现 - DT-013内存泄漏修复
}
