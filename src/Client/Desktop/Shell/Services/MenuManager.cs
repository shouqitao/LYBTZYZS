using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Contracts;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Commands;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;

namespace LYBT.Desktop.Shell.Services;

/// <summary>菜单命令管理器 - 负责快捷键命令、主题切换、帮助设置等功能</summary>
/// <remarks>
/// OpenSpec: unify-navigation-architecture - 使用INavigationCoordinator统一导航入口
/// S6-01: 根据 CurrentUser.Role 控制菜单可见性
/// S6-02: 根据 ConnectionMode 禁用本地模式不适用的菜单
/// </remarks>
public class MenuManager
{
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly ISessionManager _sessionManager;
    private readonly IConnectionModeProvider _connectionModeProvider;
    private readonly ILogger<MenuManager> _logger;
    private readonly IUserNotificationService _userNotificationService;
    private readonly IApplicationCommands _applicationCommands;

    public MenuManager(
        INavigationCoordinator navigationCoordinator,
        ISessionManager sessionManager,
        IConnectionModeProvider connectionModeProvider,
        ILogger<MenuManager> logger,
        IUserNotificationService userNotificationService,
        IApplicationCommands applicationCommands)
    {
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _connectionModeProvider = connectionModeProvider ?? throw new ArgumentNullException(nameof(connectionModeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userNotificationService = userNotificationService ?? throw new ArgumentNullException(nameof(userNotificationService));
        _applicationCommands = applicationCommands ?? throw new ArgumentNullException(nameof(applicationCommands));

        InitializeCommands();
    }

    #region S6-01/S6-02 菜单可见性

    /// <summary>S6-01: 用户管理菜单可见性 (仅 Admin/SuperAdmin + 远程模式)</summary>
    public bool IsUserManagementVisible =>
        _connectionModeProvider.IsRemote &&
        _sessionManager.CurrentUser?.Role is UserRole.Admin or UserRole.SuperAdmin;

    /// <summary>S6-02: 同步菜单可见性 (仅远程模式)</summary>
    public bool IsSyncVisible => _connectionModeProvider.IsRemote;

    /// <summary>S6-02: 系统设置可见性 (仅 Admin/SuperAdmin)</summary>
    public bool IsSystemSettingsVisible =>
        _sessionManager.CurrentUser?.Role is UserRole.Admin or UserRole.SuperAdmin;

    /// <summary>S6-04: 密码修改可见性 (仅远程模式)</summary>
    public bool IsPasswordChangeVisible => _connectionModeProvider.IsRemote;

    /// <summary>S6-04: 账户设置可见性 (远程模式显示完整，本地模式显示简化版)</summary>
    public bool IsAccountSettingsVisible => true;

    /// <summary>当前连接模式</summary>
    public ConnectionMode CurrentConnectionMode => _connectionModeProvider.CurrentMode;

    /// <summary>刷新菜单可见性 (登录后/角色变更时调用)</summary>
    public void RefreshMenuVisibility()
    {
        _logger.LogDebug(
            "菜单可见性刷新: ConnectionMode={Mode}, Role={Role}, UserManagement={UserMgmt}, Sync={Sync}, Settings={Settings}",
            _connectionModeProvider.CurrentMode,
            _sessionManager.CurrentUser?.Role,
            IsUserManagementVisible,
            IsSyncVisible,
            IsSystemSettingsVisible);
    }

    #endregion S6-01/S6-02 菜单可见性

    #region 命令属性

    /// <summary>显示控件示例命令</summary>
    public DelegateCommand ShowControlExamplesCommand { get; private set; } = null!;

    /// <summary>快速添加患者命令(Ctrl+N)</summary>
    public DelegateCommand QuickAddPatientCommand { get; private set; } = null!;

    /// <summary>快速开始看诊命令(Ctrl+Shift+C)</summary>
    public DelegateCommand QuickStartMedicalCaseCommand { get; private set; } = null!;

    /// <summary>显示帮助命令 (F1)</summary>
    public DelegateCommand ShowHelpCommand { get; private set; } = null!;

    /// <summary>显示设置命令 (Ctrl+,)</summary>
    public DelegateCommand ShowSettingsCommand { get; private set; } = null!;

    /// <summary>主题切换命令</summary>
    public DelegateCommand ToggleThemeCommand { get; private set; } = null!;

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

    /// <summary>账户设置命令 - OpenSpec: migrate-views-to-role-modules</summary>
    public DelegateCommand EditProfileCommand { get; private set; } = null!;

    /// <summary>导航到主页命令 - OpenSpec: fix-button-navigation-system</summary>
    public DelegateCommand NavigateToHomeCommand { get; private set; } = null!;

    /// <summary>导航到系统设置命令 - OpenSpec: unify-navigation-architecture (ADR-5修正: Sidebar全局入口)</summary>
    public DelegateCommand NavigateToSystemSettingsCommand { get; private set; } = null!;

    #endregion 命令属性

    /// <summary>初始化所有命令</summary>
    private void InitializeCommands()
    {
        ShowControlExamplesCommand = new DelegateCommand(ExecuteShowControlExamples);
        QuickAddPatientCommand = new DelegateCommand(async () => await ExecuteQuickAddPatientAsync().ConfigureAwait(false));
        QuickStartMedicalCaseCommand = new DelegateCommand(async () => await ExecuteQuickStartMedicalCaseAsync().ConfigureAwait(false));
        ShowHelpCommand = new DelegateCommand(ExecuteShowHelp);
        ShowSettingsCommand = new DelegateCommand(ExecuteShowSettings);
        ToggleThemeCommand = new DelegateCommand(async () => await ExecuteToggleThemeAsync().ConfigureAwait(false));

        // OpenSpec: migrate-views-to-role-modules - 账户设置命令
        EditProfileCommand = new DelegateCommand(ExecuteAccountSettings);

        // OpenSpec: fix-button-navigation-system - 导航到主页命令
        NavigateToHomeCommand = new DelegateCommand(ExecuteNavigateToHome);

        // OpenSpec: unify-navigation-architecture (ADR-5修正) - 导航到系统设置命令
        NavigateToSystemSettingsCommand = new DelegateCommand(ExecuteNavigateToSystemSettings);

        _logger.LogDebug("菜单命令系统已初始化");
    }

    /// <summary>OpenSpec: migrate-views-to-role-modules - 账户设置</summary>
    /// <remarks>OpenSpec: unify-navigation-architecture - 使用INavigationCoordinator</remarks>
    private void ExecuteAccountSettings()
    {
        _logger.LogInformation("导航到账户设置");
        _navigationCoordinator.NavigateTo(ViewNames.AccountSettings);
    }

    /// <summary>OpenSpec: fix-button-navigation-system - 导航到主页</summary>
    /// <remarks>OpenSpec: unify-navigation-architecture - 使用INavigationCoordinator.NavigateToHome()</remarks>
    private void ExecuteNavigateToHome()
    {
        _logger.LogInformation("导航到主页");
        _navigationCoordinator.NavigateToHome();
    }

    /// <summary>OpenSpec: unify-navigation-architecture (ADR-5修正) - 导航到系统设置</summary>
    /// <remarks>ADR-5修正: 系统设置从HomeView移至Sidebar全局入口，角色自适应内容</remarks>
    private void ExecuteNavigateToSystemSettings()
    {
        _logger.LogInformation("导航到系统设置");
        _navigationCoordinator.NavigateTo(ViewNames.SystemSettings);
    }

    /// <summary>显示控件示例</summary>
    /// <remarks>OpenSpec: unify-navigation-architecture - 使用INavigationCoordinator</remarks>
    private void ExecuteShowControlExamples() => _navigationCoordinator.NavigateTo(ViewNames.ControlExamples);

    /// <summary>快速添加患者(Ctrl+N)</summary>
    /// <remarks>OpenSpec: unify-navigation-architecture - 使用INavigationCoordinator</remarks>
    private async Task ExecuteQuickAddPatientAsync()
    {
        try
        {
            _navigationCoordinator.NavigateTo(ViewNames.PatientManagement, new Dictionary<string, object> { { "Action", "AddNew" } });
            await _userNotificationService.ShowSuccessAsync("已切换到患者管理页面，准备添加新患者");
        }
        catch (Exception ex) { await _userNotificationService.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("快速添加患者", ex)); }
    }

    /// <summary>快速开始看诊(Ctrl+Shift+C)</summary>
    /// <remarks>OpenSpec: unify-navigation-architecture - 使用INavigationCoordinator</remarks>
    private async Task ExecuteQuickStartMedicalCaseAsync()
    {
        try
        {
            _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseWorkspace);
            await _userNotificationService.ShowSuccessAsync("已开始诊疗流程，请选择患者");
        }
        catch (Exception ex) { await _userNotificationService.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("快速开始诊疗", ex)); }
    }

    /// <summary>显示帮助信息 (F1)</summary>
    private void ExecuteShowHelp()
    {
        var helpMessage = "系统快捷键说明：\n\n• Ctrl+N - 快速添加患者\n• Ctrl+Shift+C - 快速开始诊疗\n• F1 - 显示帮助\n• Alt+F4 - 退出系统\n• Ctrl+, - 打开设置\n\n更多功能正在开发中...";
        _ = _userNotificationService.ShowSuccessAsync(helpMessage);
    }

    /// <summary>显示设置页面 (Ctrl+,)</summary>
    private void ExecuteShowSettings() => _ = _userNotificationService.ShowSuccessAsync("用户设置功能将在未来版本中实现");

    /// <summary>主题切换功能</summary>
    private async Task ExecuteToggleThemeAsync()
    {
        try
        {
            var isDark = Application.Current.Resources.Contains("IsDarkTheme") &&
                (bool)Application.Current.Resources["IsDarkTheme"];

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (isDark)
                {
                    ApplyLightTheme();
                    Application.Current.Resources["IsDarkTheme"] = false;
                }
                else
                {
                    ApplyDarkTheme();
                    Application.Current.Resources["IsDarkTheme"] = true;
                }
            });

            await _userNotificationService.ShowSuccessAsync("主题已切换");
        }
        catch (Exception ex)
        {
            await _userNotificationService.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("主题切换", ex));
        }
    }

    /// <summary>应用浅色主题</summary>
    private void ApplyLightTheme()
    {
        var resources = Application.Current.Resources;
        UpdateThemeColor(resources, "BackgroundColor", "#FFF8F9FA");
        UpdateThemeColor(resources, "SurfaceColor", "#FFFFFFFF");
        UpdateThemeColor(resources, "TextPrimaryColor", "#FF1A1A1A");
    }

    /// <summary>应用深色主题</summary>
    private void ApplyDarkTheme()
    {
        var resources = Application.Current.Resources;
        UpdateThemeColor(resources, "BackgroundColor", "#FF1E1E1E");
        UpdateThemeColor(resources, "SurfaceColor", "#FF2D2D2D");
        UpdateThemeColor(resources, "TextPrimaryColor", "#FFFFFFFF");
    }

    /// <summary>更新主题颜色</summary>
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
        { /* 忽略主题更新错误 */ }
    }
}
