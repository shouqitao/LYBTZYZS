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
using LYBT.Desktop.Infrastructure.Services.Toast;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Login;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Shell.ViewModels;

/// <summary>
/// 主窗口视图模型 - 界面导航控制、键盘快捷键、登录状态代理
/// 登录状态管理已提取至 ILoginStateManager，事件协调已提取至 ShellEventCoordinator
/// </summary>
public partial class MainWindowViewModel : CoreViewModelBase
{
    #region 常量

    private const int SplashRenderDelayMs = 500;
    private const int SidebarCollapsedWidth = 60;
    private const int SidebarExpandedWidth = 140;

    #endregion

    #region 依赖服务

    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly MenuManager _menuManager;
    private readonly IActiveConsultationService _activeConsultationService;
    private readonly IApplicationTickService _tickService;
    private readonly IThemeService _themeService;
    private readonly StatusBarManager _statusBarManager;
    private readonly NavigationManager _navigationManager;
    private readonly ILoginStateManager _loginStateManager;
    private readonly ShellEventCoordinator _shellEventCoordinator;

    protected IRegionManager RegionManager { get; }
    protected ICommonDialogService? CommonDialogService { get; }
    protected IToastService? ToastService { get; }

    #endregion

    #region 登录状态代理（委托给 ILoginStateManager）

    public string Title => _loginStateManager.Title;
    public UserDetailDto? CurrentUser => _loginStateManager.CurrentUser;
    public bool IsLoggedIn => _loginStateManager.IsLoggedIn;
    public bool IsNotLoggedIn => _loginStateManager.IsNotLoggedIn;
    public string CurrentUserDisplayName => _loginStateManager.CurrentUserDisplayName;
    public string CurrentUserInitial => _loginStateManager.CurrentUserInitial;
    public string CurrentUserRoleDisplay => _loginStateManager.CurrentUserRoleDisplay;

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

    partial void OnIsDarkModeChanged(bool value) => _themeService.ApplyTheme(value);

    #endregion

    #region 计算属性（委托给 MenuManager / NavigationManager / StatusBarManager）

    public bool IsUserManagementVisible => _menuManager.IsUserManagementVisible;
    public bool IsSystemSettingsVisible => _menuManager.IsSystemSettingsVisible;
    public bool IsPasswordChangeVisible => _menuManager.IsPasswordChangeVisible;

    public ICollectionView GroupedNavItems => _navigationManager.GroupedNavItems;
    public ObservableCollection<NavigationItem> NavigationItems => _navigationManager.NavigationItems;

    public NavigationItem? SelectedNavItem
    {
        get => _navigationManager.SelectedNavItem;
        set => _navigationManager.SelectedNavItem = value;
    }

    public ApiHealthStatus ApiStatus => _statusBarManager.ApiStatus;
    public string ConnectionUrl => _statusBarManager.ConnectionUrl;
    public bool IsLocal => _statusBarManager.IsLocal;
    public string ConnectionModeDisplay => _statusBarManager.ConnectionModeDisplay;
    public bool IsRemoteMode => _statusBarManager.IsRemoteMode;
    public string CurrentTimeDisplay => _statusBarManager.CurrentTimeDisplay;
    public PackIconKind ApiStatusIcon => _statusBarManager.ApiStatusIcon;
    public Brush ApiStatusColor => _statusBarManager.ApiStatusColor;

    #endregion

    #region 构造函数

    public MainWindowViewModel(
        IViewModelServices services,
        INavigationCoordinator navigationCoordinator,
        MenuManager menuManager,
        IActiveConsultationService activeConsultationService,
        IApplicationTickService tickService,
        IThemeService themeService,
        StatusBarManager statusBarManager,
        NavigationManager navigationManager,
        ILoginStateManager loginStateManager,
        ShellEventCoordinator shellEventCoordinator)
        : base(services)
    {
        RegionManager = services.RegionManager;
        CommonDialogService = services.CommonDialogService;
        ToastService = services.ToastService;

        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
        _activeConsultationService = activeConsultationService ?? throw new ArgumentNullException(nameof(activeConsultationService));
        _tickService = tickService ?? throw new ArgumentNullException(nameof(tickService));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _statusBarManager = statusBarManager ?? throw new ArgumentNullException(nameof(statusBarManager));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _loginStateManager = loginStateManager ?? throw new ArgumentNullException(nameof(loginStateManager));
        _shellEventCoordinator = shellEventCoordinator ?? throw new ArgumentNullException(nameof(shellEventCoordinator));

        _tickService.Tick += OnTick;
        _tickService.Start();

        _loginStateManager.LoginStateChanged += OnLoginStateChanged;
        _shellEventCoordinator.LoginSuccessHandled += OnLoginSuccessHandled;
    }

    #endregion

    #region 委托命令属性

    public ICommand QuickAddPatientCommand => _menuManager.QuickAddPatientCommand;
    public ICommand QuickStartMedicalCaseCommand => _menuManager.QuickStartMedicalCaseCommand;
    public ICommand ShowHelpCommand => _menuManager.ShowHelpCommand;
    public ICommand ShowSettingsCommand => _menuManager.ShowSettingsCommand;
    public ICommand ToggleThemeCommand => _menuManager.ToggleThemeCommand;
    public ICommand SaveAllCommand => _menuManager.SaveAllCommand;
    public ICommand RefreshAllCommand => _menuManager.RefreshAllCommand;
    public ICommand PrintCommand => _menuManager.PrintCommand;
    public ICommand ExportCommand => _menuManager.ExportCommand;
    public ICommand UndoCommand => _menuManager.UndoCommand;
    public ICommand RedoCommand => _menuManager.RedoCommand;
    public ICommand EditProfileCommand => _menuManager.EditProfileCommand;
    public ICommand NavigateToHomeCommand => _menuManager.NavigateToHomeCommand;
    public ICommand NavigateToSystemSettingsCommand => _menuManager.NavigateToSystemSettingsCommand;
    public ICommand NavigateBackCommand => _menuManager.NavigateBackCommand;
    public ICommand NavigateForwardCommand => _menuManager.NavigateForwardCommand;

    #endregion

    #region RelayCommand

    [RelayCommand]
    private async Task LogoutAsync()
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
                    return;
            }

            await _loginStateManager.PerformLogoutAsync();
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
        await _statusBarManager.ForceCheckAsync();
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
        _statusBarManager.UpdateTime();
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
        OnPropertyChanged(nameof(IsUserManagementVisible));
        OnPropertyChanged(nameof(IsSystemSettingsVisible));
        OnPropertyChanged(nameof(IsPasswordChangeVisible));
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

    protected virtual async Task ShowSuccessMessageAsync(string message)
    {
        if (ToastService != null)
        {
            await Task.Run(() => ToastService.ShowSuccess(message));
            return;
        }
        Logger.LogWarning("ToastService不可用，成功消息未显示: {Message}", message);
    }

    protected virtual async Task ShowErrorMessageAsync(string message)
    {
        if (ToastService != null)
        {
            await Task.Run(() => ToastService.ShowError(message));
            return;
        }
        Logger.LogError("ToastService不可用，错误消息未显示: {Message}", message);
    }

    protected virtual async Task ShowWarningMessageAsync(string message)
    {
        if (CommonDialogService != null)
        {
            await CommonDialogService.ShowWarningAsync(message, "警告");
            return;
        }
        Logger.LogWarning("CommonDialogService不可用，警告消息未显示: {Message}", message);
    }

    protected virtual async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
    {
        if (CommonDialogService != null)
            return await CommonDialogService.ShowConfirmAsync(message, title);
        Logger.LogWarning("CommonDialogService不可用，确认对话框未显示: {Message}，默认返回false", message);
        return false;
    }

    #endregion

    #region IDisposable

    protected override void OnDisposing()
    {
        try
        {
            _tickService.Tick -= OnTick;
            _loginStateManager.LoginStateChanged -= OnLoginStateChanged;
            _shellEventCoordinator.LoginSuccessHandled -= OnLoginSuccessHandled;
            _shellEventCoordinator.Dispose();
            _loginStateManager.Dispose();
            _statusBarManager.Dispose();
        }
        catch (Exception ex) { Logger.LogError(ex, "资源清理异常"); }
        finally { base.OnDisposing(); }
    }

    #endregion
}
