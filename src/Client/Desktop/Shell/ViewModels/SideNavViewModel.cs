using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Controls.Models;
using LYBT.Desktop.Shell.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace LYBT.Desktop.Shell.ViewModels;

/// <summary>
/// SideNavViewModel — 左侧导航 (240/64)
/// 按 desktop-layout-framework §左侧导航 + §角色×菜单矩阵 + §底部固定区
/// </summary>
public partial class SideNavViewModel : ObservableObject, IDisposable
{
    private readonly IShellServices _shell;
    private readonly INavigationManager _navigationManager;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNavTextVisible))]
    private bool _isSidebarExpanded = false;

    [ObservableProperty]
    private double _sidebarWidth = ShellConstants.SidebarCollapsedWidth;

    public bool IsNavTextVisible => IsSidebarExpanded;

    [ObservableProperty]
    private bool _isDarkMode;

    partial void OnIsDarkModeChanged(bool value) => _shell.Theme.ApplyTheme(value);
    partial void OnIsSidebarExpandedChanged(bool value)
    {
        SidebarWidth = value ? ShellConstants.SidebarExpandedWidth : ShellConstants.SidebarCollapsedWidth;
    }

    public ObservableCollection<NavigationItem> NavigationItems => _navigationManager.NavigationItems;
    public NavigationItem? SelectedNavItem
    {
        get => _navigationManager.SelectedNavItem;
        set => _navigationManager.SelectedNavItem = value;
    }

    public ICommand LogoutCommand { get; }
    public ICommand EditProfileCommand => _shell.Menu.EditProfileCommand;

    public SideNavViewModel(IShellServices shell, INavigationManager navigationManager)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));

        // 初始同步宽度
        SidebarWidth = IsSidebarExpanded ? ShellConstants.SidebarExpandedWidth : ShellConstants.SidebarCollapsedWidth;

        _shell.LoginState.LoginStateChanged += OnLoginStateChanged;

        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    private async Task LogoutAsync()
    {
        // 复用 MainWindow 的登出流程（简化版：直接委托 LoginState）
        await _shell.LoginState.PerformLogoutAsync();
    }

    private void OnLoginStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(NavigationItems));
        OnPropertyChanged(nameof(SelectedNavItem));
    }

    public void Dispose()
    {
        _shell.LoginState.LoginStateChanged -= OnLoginStateChanged;
    }
}
