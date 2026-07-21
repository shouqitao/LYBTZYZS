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
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Login;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.ViewModels;

/// <summary>
/// 主窗口视图模型 - 界面导航控制、键盘快捷键、登录状态代理
/// 登录状态管理已提取至 ILoginStateManager，事件协调已提取至 ShellEventCoordinator
/// </summary>
public partial class MainWindowViewModel : NavigableViewModelBase
{
    #region 常量

    private const int SplashRenderDelayMs = 500;
    private const int SidebarCollapsedWidth = 60;
    private const int SidebarExpandedWidth = 140;

    #endregion

    #region 依赖服务

    private readonly IShellServices _shell;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly INavigationManager _navigationManager;

    #endregion

    #region 登录状态代理（委托给 ILoginStateManager）

    public string Title => _shell.LoginState.Title;
    public UserDetailDto? CurrentUser => _shell.LoginState.CurrentUser;
    public bool IsLoggedIn => _shell.LoginState.IsLoggedIn;
    public bool IsNotLoggedIn => _shell.LoginState.IsNotLoggedIn;
    public string CurrentUserDisplayName => _shell.LoginState.CurrentUserDisplayName;
    public string CurrentUserInitial => _shell.LoginState.CurrentUserInitial;
    public string CurrentUserRoleDisplay => _shell.LoginState.CurrentUserRoleDisplay;

    #endregion

    #region 可观察属性

    [ObservableProperty]
    private double _sidebarWidth = 60;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavTextVisibility))]
    private bool _isSidebarExpanded = false;

    public Visibility NavTextVisibility =>
        IsSidebarExpanded ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    private bool _isDarkMode;

    partial void OnIsDarkModeChanged(bool value) => _shell.Theme.ApplyTheme(value);

    #endregion

    #region 计算属性（委托给 NavigationManager / StatusBarManager）

    public ObservableCollection<NavigationItem> NavigationItems => _navigationManager.NavigationItems;

    public NavigationItem? SelectedNavItem
    {
        get => _navigationManager.SelectedNavItem;
        set => _navigationManager.SelectedNavItem = value;
    }

    public ApiHealthStatus ApiStatus => _shell.StatusBar.ApiStatus;
    public string ConnectionUrl => _shell.StatusBar.ConnectionUrl;
    public bool IsLocal => _shell.StatusBar.IsLocal;
    public string ConnectionModeDisplay => _shell.StatusBar.ConnectionModeDisplay;
    public bool IsRemoteMode => _shell.StatusBar.IsRemoteMode;
    public string CurrentTimeDisplay => _shell.StatusBar.CurrentTimeDisplay;
    public PackIconKind ApiStatusIcon => _shell.StatusBar.ApiStatusIcon;
    public Brush ApiStatusColor => _shell.StatusBar.ApiStatusColor;

    #endregion

    #region 构造函数

    public MainWindowViewModel(
        IViewModelServices services,
        IShellServices shell,
        INavigationCoordinator navigationCoordinator,
        INavigationManager navigationManager)
        : base(services)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));

        _shell.Tick.Tick += OnTick;
        _shell.Tick.Start();

        _shell.LoginState.LoginStateChanged += OnLoginStateChanged;
        _shell.Events.LoginSuccessHandled += OnLoginSuccessHandled;
    }

    #endregion

    #region 委托命令属性

    public ICommand QuickAddPatientCommand => _shell.Menu.QuickAddPatientCommand;
    public ICommand QuickStartMedicalCaseCommand => _shell.Menu.QuickStartMedicalCaseCommand;
    public ICommand ShowHelpCommand => _shell.Menu.ShowHelpCommand;
    public ICommand ShowSettingsCommand => _shell.Menu.ShowSettingsCommand;
    public ICommand ToggleThemeCommand => _shell.Menu.ToggleThemeCommand;
    public ICommand SaveAllCommand => _shell.Menu.SaveAllCommand;
    public ICommand RefreshAllCommand => _shell.Menu.RefreshAllCommand;
    public ICommand PrintCommand => _shell.Menu.PrintCommand;
    public ICommand ExportCommand => _shell.Menu.ExportCommand;
    public ICommand UndoCommand => _shell.Menu.UndoCommand;
    public ICommand RedoCommand => _shell.Menu.RedoCommand;
    public ICommand EditProfileCommand => _shell.Menu.EditProfileCommand;
    public new ICommand NavigateToHomeCommand => _shell.Menu.NavigateToHomeCommand;
    public ICommand NavigateToSystemSettingsCommand => _shell.Menu.NavigateToSystemSettingsCommand;
    public ICommand NavigateBackCommand => _shell.Menu.NavigateBackCommand;
    public ICommand NavigateForwardCommand => _shell.Menu.NavigateForwardCommand;

    #endregion

    #region RelayCommand

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            if (_shell.ActiveConsultation.HasActiveConsultation)
            {
                var leaveResult = await _shell.ActiveConsultation.RequestLeaveAsync();
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
                    return;
            }

            await _shell.LoginState.PerformLogoutAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "退出登录时发生异常");
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("退出登录", ex));
        }
    }

    [RelayCommand]
    private async Task RetryHealthCheckAsync()
    {
        await _shell.StatusBar.ForceCheckAsync();
    }

    partial void OnIsSidebarExpandedChanged(bool value)
    {
        SidebarWidth = value ? SidebarExpandedWidth : SidebarCollapsedWidth;
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    #endregion

    #region 事件处理

    private void OnTick(object? sender, ApplicationTickEventArgs e)
    {
        _shell.StatusBar.UpdateTime();
    }

    private void OnLoginStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(IsNotLoggedIn));
        OnPropertyChanged(nameof(CurrentUser));
        OnPropertyChanged(nameof(CurrentUserDisplayName));
        OnPropertyChanged(nameof(CurrentUserInitial));
        OnPropertyChanged(nameof(CurrentUserRoleDisplay));
    }

    private void OnLoginSuccessHandled(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(NavigationItems));
    }

    #endregion

    #region 公共方法

    public async Task OnWindowLoadedAsync()
    {
        await Task.Delay(SplashRenderDelayMs);
        try
        {
            _navigationCoordinator.ShowLoginDialog();
        }
        catch (Exception ex)
        {
            await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("初始化登录界面", ex));
        }
    }

    public async Task<bool> RequestCloseApplicationAsync()
    {
        var confirmed = await ShowConfirmationAsync("确定要退出程序吗？", "退出确认");
        if (confirmed)
            Application.Current.Shutdown();
        return confirmed;
    }

    #endregion

    #region 对话框辅助方法

    protected override async Task ShowSuccessMessageAsync(string message) =>
        await _shell.Dialogs.ShowSuccessMessageAsync(message);

    protected override async Task ShowErrorMessageAsync(string message) =>
        await _shell.Dialogs.ShowErrorMessageAsync(message);

    protected override async Task ShowWarningMessageAsync(string message) =>
        await _shell.Dialogs.ShowWarningMessageAsync(message);

    protected virtual async Task<bool> ShowConfirmationAsync(string message, string title = "确认") =>
        await _shell.Dialogs.ShowConfirmationAsync(message, title);

    #endregion

    #region IDisposable

    protected override void OnDisposing()
    {
        try
        {
            _shell.Tick.Tick -= OnTick;
            _shell.LoginState.LoginStateChanged -= OnLoginStateChanged;
            _shell.Events.LoginSuccessHandled -= OnLoginSuccessHandled;
            _shell.Events.Dispose();
            _shell.LoginState.Dispose();
            _shell.StatusBar.Dispose();
        }
        catch (Exception ex) { Logger.LogError(ex, "资源清理异常"); }
        finally { base.OnDisposing(); }
    }

    #endregion
}
