using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Contracts.UI;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Commands;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;

namespace LYBT.Desktop.Shell.Services;

/// <summary>菜单命令管理器 - 负责快捷键命令、主题切换、帮助设置等功能</summary>
/// <remarks>
/// S6-01: 根据 CurrentUser.Role 控制菜单可见性
/// </remarks>
public class MenuManager : IMenuManager
{
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly ISessionManager _sessionManager;
    private readonly IRoleRegistry _roleRegistry;
    private readonly ILogger<MenuManager> _logger;
    private readonly IUserNotificationService _userNotificationService;
    private readonly IApplicationCommands _applicationCommands;
    private readonly IThemeService _themeService;

    public MenuManager(
        INavigationCoordinator navigationCoordinator,
        ISessionManager sessionManager,
        IRoleRegistry roleRegistry,
        ILogger<MenuManager> logger,
        IUserNotificationService userNotificationService,
        IApplicationCommands applicationCommands,
        IThemeService themeService)
    {
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _roleRegistry = roleRegistry ?? throw new ArgumentNullException(nameof(roleRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userNotificationService = userNotificationService ?? throw new ArgumentNullException(nameof(userNotificationService));
        _applicationCommands = applicationCommands ?? throw new ArgumentNullException(nameof(applicationCommands));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

        InitializeCommands();
    }

    #region 命令属性

    /// <summary>快速添加患者命令(Ctrl+N)</summary>
    public ICommand QuickAddPatientCommand { get; private set; } = null!;

    /// <summary>快速开始看诊命令(Ctrl+Shift+C)</summary>
    public ICommand QuickStartMedicalCaseCommand { get; private set; } = null!;

    /// <summary>显示帮助命令 (F1)</summary>
    public ICommand ShowHelpCommand { get; private set; } = null!;

    /// <summary>显示设置命令 (Ctrl+,)</summary>
    public ICommand ShowSettingsCommand { get; private set; } = null!;

    /// <summary>主题切换命令</summary>
    public ICommand ToggleThemeCommand { get; private set; } = null!;

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

    /// <summary>账户设置命令</summary>
    public ICommand EditProfileCommand { get; private set; } = null!;

    /// <summary>导航到主页命令</summary>
    public ICommand NavigateToHomeCommand { get; private set; } = null!;

    /// <summary>导航到系统设置命令</summary>
    public ICommand NavigateToSystemSettingsCommand { get; private set; } = null!;

    /// <summary>导航后退命令 — 导航架构改进方案 v1.0</summary>
    public DelegateCommand NavigateBackCommand { get; private set; } = null!;

    /// <summary>导航前进命令 — 导航架构改进方案 v1.0</summary>
    public DelegateCommand NavigateForwardCommand { get; private set; } = null!;

    #endregion 命令属性

    /// <summary>初始化所有命令</summary>
    private void InitializeCommands()
    {
        QuickAddPatientCommand = new DelegateCommand(() => _ = ExecuteQuickAddPatientAsync());
        QuickStartMedicalCaseCommand = new DelegateCommand(() => _ = ExecuteQuickStartMedicalCaseAsync());
        ShowHelpCommand = new DelegateCommand(ExecuteShowHelp);
        ShowSettingsCommand = new DelegateCommand(ExecuteShowSettings);
        ToggleThemeCommand = new DelegateCommand(() => _ = ExecuteToggleThemeAsync());
        EditProfileCommand = new DelegateCommand(ExecuteAccountSettings);
        NavigateToHomeCommand = new DelegateCommand(ExecuteNavigateToHome);
        NavigateToSystemSettingsCommand = new DelegateCommand(ExecuteNavigateToSystemSettings);

        NavigateBackCommand = new DelegateCommand(ExecuteNavigateBack, () => _navigationCoordinator.CanNavigateBack);
        NavigateForwardCommand = new DelegateCommand(ExecuteNavigateForward, () => _navigationCoordinator.CanNavigateForward);

        _logger.LogDebug("菜单命令系统已初始化");
    }
    private void ExecuteAccountSettings()
    {
        _logger.LogInformation("导航到账户设置");
        _navigationCoordinator.NavigateTo(ViewNames.AccountSettings);
    }
    private void ExecuteNavigateToHome()
    {
        _logger.LogInformation("导航到主页");
        _navigationCoordinator.NavigateToHome();
    }
    /// <remarks>ADR-5修正: 系统设置从HomeView移至Sidebar全局入口，角色自适应内容</remarks>
    private void ExecuteNavigateToSystemSettings()
    {
        _logger.LogInformation("导航到系统设置");
        _navigationCoordinator.NavigateTo(ViewNames.SystemSettings);
    }

    /// <summary>导航架构改进方案 v1.0 — 后退命令</summary>
    private void ExecuteNavigateBack()
    {
        _logger.LogInformation("导航后退");
        _navigationCoordinator.NavigateBack();
        RaiseNavigationCanExecuteChanged();
    }

    /// <summary>导航架构改进方案 v1.0 — 前进命令</summary>
    private void ExecuteNavigateForward()
    {
        _logger.LogInformation("导航前进");
        _navigationCoordinator.NavigateForward();
        RaiseNavigationCanExecuteChanged();
    }

    private void RaiseNavigationCanExecuteChanged()
    {
        NavigateBackCommand.RaiseCanExecuteChanged();
        NavigateForwardCommand.RaiseCanExecuteChanged();
    }

    /// <summary>快速添加患者(Ctrl+N)</summary>
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
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _themeService.ToggleTheme();
            });

            await _userNotificationService.ShowSuccessAsync("主题已切换");
        }
        catch (Exception ex)
        {
            await _userNotificationService.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("主题切换", ex));
        }
    }
}
