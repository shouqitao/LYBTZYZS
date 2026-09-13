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
    // 侧栏宽度常量已抽至 ShellConstants (240/64)，此处保留仅为 AppShell 绑定兼容，值引用 ShellConstants

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

    /// <summary>
    /// 侧栏状态代理 — 单一真相源 <see cref="IShellServices.Sidebar"/>（与 SideNavViewModel 共用同一实例）。
    /// 宿主侧仅暴露绑定面：Ctrl+M 命令、AppShell 列宽。宽度由 ShellConstants 推导，不在此另存状态。
    /// </summary>
    public bool IsSidebarExpanded
    {
        get => _shell.Sidebar.IsSidebarExpanded;
        set
        {
            if (_shell.Sidebar.IsSidebarExpanded == value) return;
            _shell.Sidebar.IsSidebarExpanded = value;
        }
    }

    public double SidebarWidth => _shell.Sidebar.SidebarWidth;

    public bool IsNavTextVisible => _shell.Sidebar.IsNavTextVisible;

    private void OnSidebarStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsSidebarExpanded));
        OnPropertyChanged(nameof(SidebarWidth));
        OnPropertyChanged(nameof(IsNavTextVisible));
    }

    #region 计算属性（委托给 NavigationManager / StatusBarManager）

    public ObservableCollection<NavigationItem> NavigationItems => _navigationManager.NavigationItems;

    public NavigationItem? SelectedNavItem
    {
        get => _navigationManager.SelectedNavItem;
        set => _navigationManager.SelectedNavItem = value;
    }

    // 状态栏属性已迁至 FooterViewModel (StatusBarManager)，此处保留 Navigation 委托
    public ApiHealthStatus ApiStatus => _shell.StatusBar.ApiStatus;

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

        _shell.LoginState.LoginStateChanged += OnLoginStateChanged;
        _shell.Events.LoginSuccessHandled += OnLoginSuccessHandled;
        // 侧栏状态 SSOT 变更 → 重新广播宿主绑定面（IsSidebarExpanded/SidebarWidth/IsNavTextVisible）
        _shell.Sidebar.PropertyChanged += OnSidebarStateChanged;
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
    public new ICommand NavigateToHomeCommand => _shell.Menu.NavigateToHomeCommand;
    public ICommand NavigateToSystemSettingsCommand => _shell.Menu.NavigateToSystemSettingsCommand;
    public ICommand NavigateBackCommand => _shell.Menu.NavigateBackCommand;
    public ICommand NavigateForwardCommand => _shell.Menu.NavigateForwardCommand;

    #endregion

    #region RelayCommand

    [RelayCommand]
    private async Task LogoutAsync()
    {
        // 守卫 + 确认 + 执行统一在 IShellLogoutService（侧栏退出按钮同源，避免绕过活跃医案守卫）
        if (await _shell.Logout.RequestLogoutAsync() == LogoutOutcome.Failed)
            await ShowErrorMessageAsync("退出登录失败，请稍后重试");
    }

    [RelayCommand]
    private async Task RetryHealthCheckAsync()
    {
        await _shell.StatusBar.ForceCheckAsync();
    }

    /// <summary>切换侧栏展开/收拢（Ctrl+M 绑定）——委托共享状态，与侧栏汉堡按钮同源</summary>
    [RelayCommand]
    private void ToggleSidebar() => _shell.Sidebar.Toggle();

    #endregion

    #region 事件处理

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
            _shell.LoginState.LoginStateChanged -= OnLoginStateChanged;
            _shell.Events.LoginSuccessHandled -= OnLoginSuccessHandled;
            _shell.Sidebar.PropertyChanged -= OnSidebarStateChanged;
            _shell.Events.Dispose();
            _shell.LoginState.Dispose();
            _shell.StatusBar.Dispose();
        }
        catch (Exception ex) { Logger.LogError(ex, "资源清理异常"); }
        finally { base.OnDisposing(); }
    }

    #endregion
}
