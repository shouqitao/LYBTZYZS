using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Controls.Models;
using LYBT.Desktop.Shell.Services;

namespace LYBT.Desktop.Shell.ViewModels;

/// <summary>
/// SideNavViewModel — 左侧导航 (240/64)
/// <para>状态来源（均为 SSOT，本 VM 不持有副本）：</para>
/// <list type="bullet">
/// <item>展开态/宽度 → <see cref="IShellServices.Sidebar"/>（与宿主 MainWindowViewModel 共用同一实例，
/// 保证 Ctrl+M 与汉堡按钮同步）</item>
/// <item>深色模式 → <see cref="IShellServices.Theme"/>（ThemeService 经 MaterialDesign PaletteHelper 应用主题）</item>
/// </list>
/// </summary>
public partial class SideNavViewModel : ObservableObject, IDisposable
{
    private readonly IShellServices _shell;
    private readonly INavigationManager _navigationManager;

    /// <summary>是否展开（代理共享状态——汉堡按钮双向绑定此处）</summary>
    public bool IsSidebarExpanded
    {
        get => _shell.Sidebar.IsSidebarExpanded;
        set
        {
            if (_shell.Sidebar.IsSidebarExpanded == value) return;
            _shell.Sidebar.IsSidebarExpanded = value;
        }
    }

    /// <summary>当前侧栏宽度（由共享状态 + ShellConstants 推导）</summary>
    public double SidebarWidth => _shell.Sidebar.SidebarWidth;

    /// <summary>是否显示导航文字（收拢态仅图标）</summary>
    public bool IsNavTextVisible => _shell.Sidebar.IsNavTextVisible;

    /// <summary>深色模式（代理 ThemeService.IsDarkMode——主题状态 SSOT）</summary>
    public bool IsDarkMode
    {
        get => _shell.Theme.IsDarkMode;
        set
        {
            if (_shell.Theme.IsDarkMode == value) return;
            _shell.Theme.ApplyTheme(value);
        }
    }

    /// <summary>导航项集合（NavigationManager 持有）</summary>
    public ObservableCollection<NavigationItem> NavigationItems => _navigationManager.NavigationItems;

    /// <summary>
    /// 按 Group 分组的导航项视图，供 SideNavControl ListBox 绑定。
    /// 避免 XAML 中 CollectionViewSource.Source 在 DataContext 为 null 时崩溃。
    /// </summary>
    public ICollectionView GroupedNavigationItems
    {
        get
        {
            var view = CollectionViewSource.GetDefaultView(NavigationItems);
            if (view.GroupDescriptions.Count == 0)
                view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(NavigationItem.Group)));
            return view;
        }
    }

    /// <summary>当前选中导航项（代理 NavigationManager——导航状态 SSOT）</summary>
    public NavigationItem? SelectedNavItem
    {
        get => _navigationManager.SelectedNavItem;
        set => _navigationManager.SelectedNavItem = value;
    }

    public SideNavViewModel(IShellServices shell, INavigationManager navigationManager)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));

        _shell.LoginState.LoginStateChanged += OnLoginStateChanged;
        _shell.Sidebar.PropertyChanged += OnSidebarStateChanged;
        if (_shell.Theme is INotifyPropertyChanged themeNotifier)
            themeNotifier.PropertyChanged += OnThemeChanged;
    }

    /// <summary>退出登录（XAML 绑定 <c>LogoutCommand</c>，由 [RelayCommand] 源生成）
    /// ——经 IShellLogoutService 统一守卫（活跃医案离开流程 + 确认），与宿主登出同源</summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (await _shell.Logout.RequestLogoutAsync() == LogoutOutcome.Failed)
            await _shell.Dialogs.ShowErrorMessageAsync("退出登录失败，请稍后重试");
    }

    private void OnSidebarStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsSidebarExpanded));
        OnPropertyChanged(nameof(SidebarWidth));
        OnPropertyChanged(nameof(IsNavTextVisible));
    }

    private void OnThemeChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IThemeService.IsDarkMode))
            OnPropertyChanged(nameof(IsDarkMode));
    }

    private void OnLoginStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(NavigationItems));
        OnPropertyChanged(nameof(GroupedNavigationItems));
        OnPropertyChanged(nameof(SelectedNavItem));
    }

    public void Dispose()
    {
        _shell.LoginState.LoginStateChanged -= OnLoginStateChanged;
        _shell.Sidebar.PropertyChanged -= OnSidebarStateChanged;
        if (_shell.Theme is INotifyPropertyChanged themeNotifier)
            themeNotifier.PropertyChanged -= OnThemeChanged;
    }
}
