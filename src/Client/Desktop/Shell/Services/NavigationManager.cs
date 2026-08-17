using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Controls.Models;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 导航管理器 - 负责侧边栏导航项构建和管理
/// 从 MainWindowViewModel 提取，减少主 VM 行数
/// </summary>
public partial class NavigationManager : ObservableObject, INavigationManager
{
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly ILogger<NavigationManager> _logger;
    private readonly IRoleRegistry _roleRegistry;

    [ObservableProperty]
    private ObservableCollection<NavigationItem> _navigationItems = new();

    [ObservableProperty]
    private NavigationItem? _selectedNavItem;

    public NavigationManager(
        INavigationCoordinator navigationCoordinator,
        ILogger<NavigationManager> logger,
        IRoleRegistry roleRegistry)
    {
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _roleRegistry = roleRegistry ?? throw new ArgumentNullException(nameof(roleRegistry));
    }

    partial void OnSelectedNavItemChanged(NavigationItem? value)
    {
        if (value?.ViewName is string viewName && !string.IsNullOrEmpty(viewName))
        {
            _ = _navigationCoordinator.NavigateTo(viewName);
        }
    }

    /// <summary>
    /// 构建侧边栏导航项。
    /// 设计决策（2026-08-17）：侧边栏仅保留「主页」入口，
    /// 功能导航由各角色 Home View 的卡片网格承载。
    /// 账户/主题/退出由 MainWindow.xaml 底部按钮单独处理。
    /// </summary>
    public ObservableCollection<NavigationItem> BuildNavigationItems(UserRole role)
    {
        var definition = _roleRegistry.GetDefinition(role);
        var items = new ObservableCollection<NavigationItem>();

        if (definition == null)
        {
            _logger.LogWarning("无法为角色 {Role} 找到定义，导航项为空", role);
            return items;
        }

        // 侧边栏仅保留主页入口，功能导航由 Home View 卡片承载
        items.Add(new NavigationItem
        {
            Title = "主页",
            ViewName = definition.HomeViewName,
            IconKind = "Home",
            Command = new RelayCommand(() => _ = _navigationCoordinator.NavigateTo(definition.HomeViewName)),
            Group = "导航"
        });

        _logger.LogInformation("已为角色 {Role} 构建 {Count} 个侧边栏导航项（主页入口）", role, items.Count);
        return items;
    }
}
