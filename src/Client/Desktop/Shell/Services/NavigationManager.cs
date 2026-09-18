using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Controls.Models;
using LYBT.Desktop.Infrastructure.Constants;
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
    /// 构建侧边栏导航项 — C+ 角色矩阵（SSOT desktop-layout-framework §角色×菜单矩阵）
    /// 各角色 3 项：主页 + 2 业务入口；图标对齐 MaterialDesign PackIcon Kind
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

        // 主页（各角色 HomeViewName 已在 RoleDefinition 定义）
        items.Add(new NavigationItem
        {
            Title = "主页",
            ViewName = definition.HomeViewName,
            IconKind = "Home",
            Command = new RelayCommand(() => _ = _navigationCoordinator.NavigateTo(definition.HomeViewName)),
            Group = "临床"
        });

        // C+ 角色矩阵 2 业务入口（分组对齐设计稿：临床/目录/管理）
        // N3：Doctor 去掉无参医案工作台；主管侧栏含备份/部署
        switch (role)
        {
            case UserRole.Doctor:
                items.Add(new NavigationItem { Title = "患者选择", ViewName = ViewNames.PatientSelection, IconKind = "AccountSearch", Command = new RelayCommand(() => _ = _navigationCoordinator.NavigateTo(ViewNames.PatientSelection)), Group = "临床" });
                items.Add(new NavigationItem { Title = "挂号队列", ViewName = ViewNames.RegistrationList, IconKind = "CalendarClock", Command = new RelayCommand(() => _ = _navigationCoordinator.NavigateTo(ViewNames.RegistrationList)), Group = "临床" });
                break;
            case UserRole.Receptionist:
                items.Add(new NavigationItem { Title = "新建挂号", ViewName = ViewNames.RegistrationList, IconKind = "PlusCircle", Command = new RelayCommand(() => _ = _navigationCoordinator.NavigateTo(ViewNames.RegistrationList)), Group = "临床" });
                items.Add(new NavigationItem { Title = "患者管理", ViewName = ViewNames.PatientManagement, IconKind = "AccountGroup", Command = new RelayCommand(() => _ = _navigationCoordinator.NavigateTo(ViewNames.PatientManagement)), Group = "临床" });
                break;
            case UserRole.Admin:
                items.Add(new NavigationItem { Title = "用户管理", ViewName = ViewNames.UserManagement, IconKind = "AccountCog", Command = new RelayCommand(() => _ = _navigationCoordinator.NavigateTo(ViewNames.UserManagement)), Group = "管理" });
                items.Add(new NavigationItem { Title = "药材/验方", ViewName = ViewNames.HerbManagement, IconKind = "Leaf", Command = new RelayCommand(() => _ = _navigationCoordinator.NavigateTo(ViewNames.HerbManagement)), Group = "目录" });
                break;
            case UserRole.SuperAdmin:
                items.Add(new NavigationItem { Title = "备份管理", ViewName = ViewNames.BackupManagement, IconKind = "BackupRestore", Command = new RelayCommand(() => _ = _navigationCoordinator.NavigateTo(ViewNames.BackupManagement)), Group = "管理" });
                items.Add(new NavigationItem { Title = "部署管理", ViewName = ViewNames.Deployment, IconKind = "RocketLaunch", Command = new RelayCommand(() => _ = _navigationCoordinator.NavigateTo(ViewNames.Deployment)), Group = "管理" });
                break;
        }

        _logger.LogInformation("已为角色 {Role} 构建 {Count} 个侧边栏导航项（C+矩阵）", role, items.Count);
        return items;
    }
}
