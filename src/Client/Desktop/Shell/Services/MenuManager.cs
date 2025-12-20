using System.Windows;
using System.Windows.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Commands;
using LYBT.Desktop.Infrastructure.Localization;
using Microsoft.Extensions.Logging;
using Prism.Commands;

namespace LYBT.Desktop.Shell.Services;

/// <summary>菜单命令管理器 - 负责快捷键命令、主题切换、帮助设置等功能</summary>
public class MenuManager
{
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<MenuManager> _logger;
    private readonly IUserNotificationService _userNotificationService;
    private readonly IApplicationCommands _applicationCommands;

    public MenuManager(
        NavigationManager navigationManager,
        ILogger<MenuManager> logger,
        IUserNotificationService userNotificationService,
        IApplicationCommands applicationCommands)
    {
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userNotificationService = userNotificationService ?? throw new ArgumentNullException(nameof(userNotificationService));
        _applicationCommands = applicationCommands ?? throw new ArgumentNullException(nameof(applicationCommands));

        InitializeCommands();
    }

    #region 命令属性

    /// <summary>显示控件示例命令</summary>
    public DelegateCommand ShowControlExamplesCommand { get; private set; } = null!;

    /// <summary>快速添加患者命令(Ctrl+N)</summary>
    public DelegateCommand QuickAddPatientCommand { get; private set; } = null!;

    /// <summary>快速开始诊疗命令(Ctrl+Shift+C)</summary>
    public DelegateCommand QuickStartConsultationCommand { get; private set; } = null!;

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

    /// <summary>poc-drawer-layout: 修改个人资料命令</summary>
    public DelegateCommand EditProfileCommand { get; private set; } = null!;

    /// <summary>poc-drawer-layout: 修改密码命令</summary>
    public DelegateCommand ChangePasswordCommand { get; private set; } = null!;

    #endregion 命令属性

    /// <summary>初始化所有命令</summary>
    private void InitializeCommands()
    {
        ShowControlExamplesCommand = new DelegateCommand(ExecuteShowControlExamples);
        QuickAddPatientCommand = new DelegateCommand(async () => await ExecuteQuickAddPatientAsync().ConfigureAwait(false));
        QuickStartConsultationCommand = new DelegateCommand(async () => await ExecuteQuickStartConsultationAsync().ConfigureAwait(false));
        ShowHelpCommand = new DelegateCommand(ExecuteShowHelp);
        ShowSettingsCommand = new DelegateCommand(ExecuteShowSettings);
        ToggleThemeCommand = new DelegateCommand(async () => await ExecuteToggleThemeAsync().ConfigureAwait(false));

        // poc-drawer-layout: 用户菜单命令
        EditProfileCommand = new DelegateCommand(ExecuteEditProfile);
        ChangePasswordCommand = new DelegateCommand(ExecuteChangePassword);

        _logger.LogDebug("菜单命令系统已初始化");
    }

    /// <summary>poc-drawer-layout: 修改个人资料</summary>
    private void ExecuteEditProfile()
    {
        _logger.LogInformation("导航到个人资料页面");
        _navigationManager.NavigateTo("UserProfileView");
    }

    /// <summary>poc-drawer-layout: 修改密码</summary>
    private void ExecuteChangePassword()
    {
        _logger.LogInformation("导航到修改密码页面");
        _navigationManager.NavigateTo("ChangePasswordView");
    }

    /// <summary>显示控件示例</summary>
    private void ExecuteShowControlExamples() => _navigationManager.NavigateToControlExamples();

    /// <summary>快速添加患者(Ctrl+N)</summary>
    private async Task ExecuteQuickAddPatientAsync()
    {
        try { _navigationManager.NavigateToAddPatient(); await _userNotificationService.ShowSuccessAsync("已切换到患者管理页面，准备添加新患者"); }
        catch (Exception ex) { await _userNotificationService.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("快速添加患者", ex)); }
    }

    /// <summary>快速开始诊疗(Ctrl+Shift+C)</summary>
    private async Task ExecuteQuickStartConsultationAsync()
    {
        try { _navigationManager.NavigateToMedicalCaseFlow(); await _userNotificationService.ShowSuccessAsync("已开始诊疗流程，请选择患者"); }
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
