using System.Windows;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
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
public class MainWindowViewModel : ServiceViewModel
{
    private readonly IMainWindowServicesFacade _servicesFacade;
    private readonly IRegionManager _regionManager;

    /// <summary>
    /// 构造函数 - 按照Prism 8.x最佳实践，在构造函数中完成所有初始化
    /// </summary>
    /// <param name="regionManager">区域管理器，用于界面导航</param>
    /// <param name="eventAggregator">事件聚合器，用于模块间通信</param>
    /// <param name="servicesFacade">服务外观，统一访问各类服务</param>
    /// <param name="errorHandlingService">错误处理服务</param>
    /// <exception cref="ArgumentNullException">当任何参数为 null 时抛出</exception>
    public MainWindowViewModel(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        IMainWindowServicesFacade servicesFacade,
        IErrorHandlingService errorHandlingService) : base(eventAggregator, errorHandlingService)
    {
        _servicesFacade = servicesFacade ?? throw new ArgumentNullException(nameof(servicesFacade));
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

        // 按照Prism 8.x最佳实践，在构造函数中完成初始化
        InitializeViewModel();
    }

    // 私有字段
    private string _title = SystemConstants.SystemTitle;

    private UserDto? _currentUser;
    private bool _isLoggedIn = false;
    private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    private System.Windows.Threading.DispatcherTimer _clockTimer;

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
            System.Diagnostics.Debug.WriteLine($" MainWindow.IsLoggedIn设置为: {value} (之前: {_isLoggedIn})");
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
    public DelegateCommand LogoutCommand { get; set; }

    /// <summary>API测试命令</summary>
    public DelegateCommand TestApiCommand { get; set; }

    /// <summary>显示控件示例命令</summary>
    public DelegateCommand ShowControlExamplesCommand { get; set; }

    /// <summary>快速添加患者命令 (Ctrl+N)</summary>
    public DelegateCommand QuickAddPatientCommand { get; set; }

    /// <summary>快速开始诊疗命令 (Ctrl+Shift+C)</summary>
    public DelegateCommand QuickStartConsultationCommand { get; set; }

    /// <summary>显示帮助命令 (F1)</summary>
    public DelegateCommand ShowHelpCommand { get; set; }

    /// <summary>显示设置命令 (Ctrl+,)</summary>
    public DelegateCommand ShowSettingsCommand { get; set; }

    /// <summary>主题切换命令</summary>
    public DelegateCommand ToggleThemeCommand { get; set; }

    #endregion 命令属性

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
    private void InitializeCommands()
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

        // 延迟检查登录状态，等待主窗口完全加载
        Application.Current.Dispatcher.BeginInvoke(
            new Action(() =>
            {
                _ = CheckLoginStatusAsync();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
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

            await _servicesFacade.CustomDialogService.ShowInformationAsync("主题已切换", "提示");
        }
        catch (Exception ex)
        {
            await _servicesFacade.CustomDialogService.ShowErrorAsync($"主题切换失败：{ex.Message}", "错误");
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
        var result = await _servicesFacade.CustomDialogService.ShowConfirmationAsync("确定要退出登录吗？", "退出确认");
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
                        EventAggregator.GetEvent<LogoutEvent>().Publish(new LogoutEventArgs { Reason = "Token已过期" });
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
                await _servicesFacade.CustomDialogService.ShowErrorAsync($"退出登录失败：{ex.Message}", "错误");
            }
        }
    }

    /// <summary>
    /// 检查登录状态 - UltraThink性能优化版
    /// </summary>
    private async Task CheckLoginStatusAsync()
    {
        System.Diagnostics.Debug.WriteLine(" CheckLoginStatusAsync 开始");
        try
        {
            // UltraThink修复: 使用AuthenticationService进行状态检查，确保与登录流程一致
            var isLoggedIn = _servicesFacade.AuthenticationService.IsLoggedIn;
            System.Diagnostics.Debug.WriteLine($" AuthenticationService.IsLoggedIn = {isLoggedIn}");

            if (isLoggedIn)
            {
                System.Diagnostics.Debug.WriteLine(" 尝试通过AuthenticationService获取当前用户...");
                var user = await _servicesFacade.AuthenticationService.GetCurrentUserAsync();

                if (user != null)
                {
                    System.Diagnostics.Debug.WriteLine($" 获取到当前用户: {user.Username} - {user.RealName}");
                    CurrentUser = user;
                    IsLoggedIn = true;

                    // 更新命令状态
                    TestApiCommand.RaiseCanExecuteChanged();
                    ShowControlExamplesCommand.RaiseCanExecuteChanged();
                    UpdateKeyboardShortcutCommands();

                    System.Diagnostics.Debug.WriteLine(" 准备加载主界面内容...");

                    // 加载工作台模块
                    await EnsureWorkbenchModulesLoaded(user);

                    LoadMainContent();
                    return;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($" AuthenticationService.GetCurrentUserAsync 返回null");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(" 用户未登录，显示登录界面");
            }

            ShowLoginDialog();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($" CheckLoginStatusAsync 异常: {ex.Message}");
            await _servicesFacade.CustomDialogService.ShowErrorAsync($"检查登录状态失败：{ex.Message}", "错误");
            ShowLoginDialog();
        }
    }

    /// <summary>
    /// 登录成功事件处理
    /// </summary>
    private void OnLoginSuccess(LoginSuccessEventArgs args)
    {
        // 重新检查登录状态
        _ = CheckLoginStatusAsync();
    }

    /// <summary>
    /// 显示登录界面
    /// </summary>
    private void ShowLoginDialog()
    {
        // 在单窗口模式下，导航到登录视图
        if (_regionManager != null)
        {
            _regionManager.RequestNavigate(RegionNames.LoginRegion, "LoginView");
        }
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
        bool isAdmin = CurrentUser.Username?.Equals(SystemConstants.SuperAdminUsername, StringComparison.OrdinalIgnoreCase) == true ||
        CurrentUser.Role == UserRole.Admin;

        if (isAdmin)
        {
            workbenchView = "SystemWorkbenchMainView";
            roleDisplay = "管理员";
        }
        else
        {
            // 其他角色默认为医生工作台
            workbenchView = "MedicalWorkbenchMainView";
            roleDisplay = "医生";
        }

        // 更新标题和清理登录区域
        var userDisplayName = string.IsNullOrEmpty(CurrentUser.RealName) ? CurrentUser.Username : CurrentUser.RealName;
        Title = $"凌隐宝堂中医诊所诊疗系统 - {userDisplayName} ({roleDisplay})";

        // 清除登录区域
        if (_regionManager.Regions.ContainsRegionWithName(RegionNames.LoginRegion))
        {
            _regionManager.Regions[RegionNames.LoginRegion].RemoveAll();
        }

        // 导航到对应的工作台
        System.Diagnostics.Debug.WriteLine($" 导航到: {workbenchView}");
        _regionManager.RequestNavigate(RegionNames.ContentRegion, workbenchView, navigationResult =>
        {
            if (navigationResult.Result != true)
            {
                // 导航失败时显示错误信息
                var errorMessage = navigationResult.Error?.Message ?? "未知导航错误";
                System.Diagnostics.Debug.WriteLine($" 工作台导航失败: {errorMessage}");

                // 异步显示错误对话框
                _ = Task.Run(async () =>
        {
            await _servicesFacade.CustomDialogService.ShowErrorAsync(
    $"无法加载工作台: {errorMessage}", "系统错误");
        });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($" 成功导航到: {workbenchView}");
            }
        });
    }

    /// <summary>
    /// 确保工作台模块已加载
    /// </summary>
    private async Task EnsureWorkbenchModulesLoaded(LYBT.Shared.Models.Contracts.Users.UserDto user)
    {
        try
        {
            var app = (App)Application.Current;

            // 管理员加载SystemWorkbenchModule
            bool isAdmin = user.Username?.Equals(SystemConstants.SuperAdminUsername, StringComparison.OrdinalIgnoreCase) == true ||
            user.Role == UserRole.Admin;

            if (isAdmin)
            {
                System.Diagnostics.Debug.WriteLine(" 加载SystemWorkbenchModule模块...");
                await app.LoadRoleBasedModulesAsync(SystemConstants.AdminRole);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(" 加载MedicalWorkbenchModule模块...");
                await app.LoadRoleBasedModulesAsync(SystemConstants.DoctorRole);
            }

            System.Diagnostics.Debug.WriteLine(" 工作台模块加载完成");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($" 工作台模块加载失败: {ex.Message}");

            // 模块加载失败不应阻塞界面显示
        }
    }

    /// <summary>
    /// 执行API测试
    /// </summary>
    private async Task ExecuteTestApiAsync()
    {
        try
        {
            await _servicesFacade.CustomDialogService.ShowInformationAsync("API测试功能将在未来版本中实现", "提示");
        }
        catch (Exception ex)
        {
            await _servicesFacade.CustomDialogService.ShowErrorAsync($"API测试失败: {ex.Message}", "错误");
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
    /// 快速添加患者 (Ctrl+N)
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
            await _servicesFacade.CustomDialogService.ShowInformationAsync("已切换到患者管理页面，准备添加新患者", "快速操作");
        }
        catch (Exception ex)
        {
            await _servicesFacade.CustomDialogService.ShowErrorAsync($"快速添加患者失败：{ex.Message}", "错误");
        }
    }

    /// <summary>
    /// 快速开始诊疗 (Ctrl+Shift+C)
    /// </summary>
    private async Task ExecuteQuickStartConsultationAsync()
    {
        try
        {
            // 导航到诊疗工作台
            _regionManager.RequestNavigate(RegionNames.ContentRegion, "MedicalWorkbenchMainView", navigationResult =>
            {
                if (navigationResult.Result == true)
                {
                    // 成功导航后，可以发送事件触发快速开始诊疗流程
                    // TODO: QuickStartConsultationEvent 已移除，需要使用新的事件机制
                }
            });

            await _servicesFacade.CustomDialogService.ShowInformationAsync("已切换到诊疗工作台，准备开始诊疗", "快速操作");
        }
        catch (Exception ex)
        {
            await _servicesFacade.CustomDialogService.ShowErrorAsync($"快速开始诊疗失败：{ex.Message}", "错误");
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

            _ = _servicesFacade.CustomDialogService.ShowInformationAsync(helpMessage, "系统帮助");
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
            _ = _servicesFacade.CustomDialogService.ShowInformationAsync("用户设置功能将在未来版本中实现", "设置");
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
