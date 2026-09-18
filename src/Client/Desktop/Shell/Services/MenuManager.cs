using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Contracts.UI;
using LYBT.Desktop.Contracts.Models.Navigation;
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
/// N3: 命令 CanExecute 按角色矩阵收紧；无权入口不可用（非点击后 toast）
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

        // N4: 导航变更 → 刷新 Back/Forward CanExecute（Header 与快捷键同源）
        _navigationCoordinator.NavigationChanged += OnNavigationChanged;
    }

    #region 命令属性

    /// <summary>快速添加患者命令(Ctrl+N) — Doctor/Receptionist/Admin</summary>
    public ICommand QuickAddPatientCommand { get; private set; } = null!;

    /// <summary>快速开始看诊命令(Ctrl+Shift+C) — 仅 Doctor</summary>
    public ICommand QuickStartMedicalCaseCommand { get; private set; } = null!;

    /// <summary>显示帮助命令 (F1)</summary>
    public ICommand ShowHelpCommand { get; private set; } = null!;

    /// <summary>显示设置命令 (Ctrl+,) — Admin/SuperAdmin→SystemSettings，其他→AccountSettings</summary>
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

    /// <summary>导航到系统设置命令 — 仅 Admin/SuperAdmin</summary>
    public ICommand NavigateToSystemSettingsCommand { get; private set; } = null!;

    /// <summary>导航后退命令 — 导航架构改进方案 v1.0</summary>
    public DelegateCommand NavigateBackCommand { get; private set; } = null!;

    /// <summary>导航前进命令 — 导航架构改进方案 v1.0</summary>
    public DelegateCommand NavigateForwardCommand { get; private set; } = null!;

    #endregion 命令属性

    /// <summary>初始化所有命令（CanExecute 绑定角色矩阵）</summary>
    private void InitializeCommands()
    {
        QuickAddPatientCommand = new DelegateCommand(
            () => _ = ExecuteQuickAddPatientAsync(),
            CanQuickAddPatient);
        QuickStartMedicalCaseCommand = new DelegateCommand(
            () => _ = ExecuteQuickStartMedicalCaseAsync(),
            CanQuickStartMedicalCase);
        ShowHelpCommand = new DelegateCommand(ExecuteShowHelp);
        ShowSettingsCommand = new DelegateCommand(ExecuteShowSettings);
        ToggleThemeCommand = new DelegateCommand(() => _ = ExecuteToggleThemeAsync());
        EditProfileCommand = new DelegateCommand(ExecuteAccountSettings);
        NavigateToHomeCommand = new DelegateCommand(ExecuteNavigateToHome);
        NavigateToSystemSettingsCommand = new DelegateCommand(
            ExecuteNavigateToSystemSettings,
            CanNavigateToSystemSettings);

        NavigateBackCommand = new DelegateCommand(ExecuteNavigateBack, () => _navigationCoordinator.CanNavigateBack);
        NavigateForwardCommand = new DelegateCommand(ExecuteNavigateForward, () => _navigationCoordinator.CanNavigateForward);

        _logger.LogDebug("菜单命令系统已初始化（角色矩阵 CanExecute）");
    }

    #region 角色 CanExecute

    /// <summary>Ctrl+N：Doctor/Receptionist/Admin 可见</summary>
    private bool CanQuickAddPatient()
    {
        var role = _sessionManager.CurrentUser?.Role;
        return role is UserRole.Doctor or UserRole.Receptionist or UserRole.Admin;
    }

    /// <summary>Ctrl+Shift+C：仅 Doctor</summary>
    private bool CanQuickStartMedicalCase()
        => _sessionManager.CurrentUser?.Role == UserRole.Doctor;

    /// <summary>SystemSettings：仅 Admin/SuperAdmin</summary>
    private bool CanNavigateToSystemSettings()
    {
        var role = _sessionManager.CurrentUser?.Role;
        return role is UserRole.Admin or UserRole.SuperAdmin;
    }

    #endregion 角色 CanExecute

    private void OnNavigationChanged(object? sender, NavigationChangedEventArgs e)
        => RaiseNavigationCanExecuteChanged();

    private void ExecuteAccountSettings()
    {
        _logger.LogInformation("导航到账户设置");
        _ = _navigationCoordinator.NavigateTo(ViewNames.AccountSettings);
    }
    private void ExecuteNavigateToHome()
    {
        _logger.LogInformation("导航到主页");
        _ = _navigationCoordinator.NavigateToHome();
    }
    /// <remarks>ADR-5修正: 系统设置从HomeView移至Sidebar全局入口，角色自适应内容</remarks>
    private void ExecuteNavigateToSystemSettings()
    {
        if (!CanNavigateToSystemSettings())
        {
            _logger.LogWarning("当前角色无 SystemSettings 权限，命令已拦截");
            return;
        }

        _logger.LogInformation("导航到系统设置");
        _ = _navigationCoordinator.NavigateTo(ViewNames.SystemSettings);
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

    /// <summary>快速添加患者(Ctrl+N) — Doctor/Receptionist/Admin</summary>
    private async Task ExecuteQuickAddPatientAsync()
    {
        if (!CanQuickAddPatient())
        {
            _logger.LogWarning("当前角色无权使用 Ctrl+N 快速添加患者");
            return;
        }

        try
        {
            _ = _navigationCoordinator.NavigateTo(ViewNames.PatientManagement, PatientManagementNav.AddNew());
            await _userNotificationService.ShowSuccessAsync("已切换到患者管理页面，准备添加新患者");
        }
        catch (Exception ex) { await _userNotificationService.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("快速添加患者", ex)); }
    }

    /// <summary>快速开始看诊(Ctrl+Shift+C) — 仅 Doctor，目标 ClinicalWorkspace（选患者后开始看诊）</summary>
    private async Task ExecuteQuickStartMedicalCaseAsync()
    {
        if (!CanQuickStartMedicalCase())
        {
            _logger.LogWarning("当前角色无权使用 Ctrl+Shift+C 快速开始看诊");
            return;
        }

        try
        {
            // 设计 N3：医生快捷键进入临床工作台（非无参 MedicalCaseWorkspace）
            _ = _navigationCoordinator.NavigateTo(ViewNames.ClinicalWorkspace);
            await _userNotificationService.ShowSuccessAsync("已进入临床工作台，请选择患者后开始看诊");
        }
        catch (Exception ex) { await _userNotificationService.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("快速开始诊疗", ex)); }
    }

    /// <summary>显示帮助信息 (F1)</summary>
    private void ExecuteShowHelp()
    {
        var helpMessage = "系统快捷键说明：\n\n• Ctrl+N - 快速添加患者（医生/前台/管理员）\n• Ctrl+Shift+C - 进入临床工作台（仅医生）\n• F1 - 显示帮助\n• Alt+Left/Right - 后退/前进\n• Alt+Home - 返回主页\n• Alt+F4 - 退出系统\n• Ctrl+, - 系统设置（管理员）/ 个人资料（其他角色）\n\n更多功能正在开发中...";
        _ = _userNotificationService.ShowSuccessAsync(helpMessage);
    }

    /// <summary>显示设置页面 (Ctrl+,) — Admin/SuperAdmin→SystemSettings，其他角色→AccountSettings</summary>
    private void ExecuteShowSettings()
    {
        var role = _sessionManager.CurrentUser?.Role;
        if (role is UserRole.Admin or UserRole.SuperAdmin)
        {
            _logger.LogInformation("Ctrl+, → 系统设置");
            _ = _navigationCoordinator.NavigateTo(ViewNames.SystemSettings);
        }
        else
        {
            _logger.LogInformation("Ctrl+, → 账户设置");
            _ = _navigationCoordinator.NavigateTo(ViewNames.AccountSettings);
        }
    }

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
