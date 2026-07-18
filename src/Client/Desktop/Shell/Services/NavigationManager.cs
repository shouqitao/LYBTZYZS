using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Controls.Models;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Navigation.NavigationArgs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 导航管理器 - 负责侧边栏导航项构建和管理
/// 从 MainWindowViewModel 提取，减少主 VM 行数
/// </summary>
public partial class NavigationManager : ObservableObject
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
            _navigationCoordinator.NavigateTo(viewName);
        }
    }

    public ObservableCollection<NavigationItem> BuildNavigationItems(UserRole role)
    {
        var definition = _roleRegistry.GetDefinition(role);
        var items = new ObservableCollection<NavigationItem>();

        if (definition == null)
        {
            _logger.LogWarning("无法为角色 {Role} 找到定义，导航项为空", role);
            return items;
        }

        var modules = definition.RequiredModules;

        items.Add(new NavigationItem
        {
            Title = "主页",
            ViewName = definition.HomeViewName,
            IconKind = "Home",
            Command = new RelayCommand(() => _navigationCoordinator.NavigateTo(definition.HomeViewName)),
            Group = "主页"
        });

        if (modules.Contains("PatientsModule"))
            items.Add(CreateNavItem("患者管理", ViewNames.PatientManagement, "AccountGroup", "业务"));
        if (modules.Contains("HerbsModule"))
            items.Add(CreateNavItem("药材管理", ViewNames.HerbManagement, "Leaf", "业务"));
        if (modules.Contains("FormulaModule"))
            items.Add(CreateNavItem("验方管理", ViewNames.FormulaManagement, "Notebook", "业务"));
        if (modules.Contains("MedicalCaseModule"))
            items.Add(CreateNavItem("医案管理", ViewNames.MedicalCaseManagement, "Folder", "业务"));
        if (modules.Contains("RegistrationModule"))
            items.Add(CreateNavItem("挂号管理", ViewNames.RegistrationList, "CalendarClock", "业务"));

        if (modules.Contains("UsersModule") && role is UserRole.Admin or UserRole.SuperAdmin)
        {
            if (role == UserRole.SuperAdmin)
            {
                items.Add(new NavigationItem
                {
                    Title = "用户管理",
                    ViewName = ViewNames.UserManagement,
                    IconKind = "AccountTie",
                    Command = new RelayCommand(() => _navigationCoordinator.NavigateTo(
                        ViewNames.UserManagement,
                        new UserManagementNavParams(DefaultRoleFilter: UserRole.Admin))),
                    Group = "管理"
                });
            }
            else
            {
                items.Add(CreateNavItem("用户管理", ViewNames.UserManagement, "AccountTie", "管理"));
            }
        }
        if (modules.Contains("ReportsModule"))
            items.Add(CreateNavItem("统计报表", ViewNames.ReportsHome, "ChartBar", "管理"));

        if (role == UserRole.SuperAdmin)
        {
            items.Add(CreateNavItem("诊所信息", ViewNames.SystemSettings, "Domain", "管理"));
            items.Add(CreateNavItem("日志控制", ViewNames.LogLevelControl, "Tune", "管理"));
            items.Add(CreateNavItem("部署管理", ViewNames.Deployment, "Upload", "管理"));
        }

        _logger.LogInformation("已为角色 {Role} 构建 {Count} 个导航项", role, items.Count);
        return items;
    }

    private NavigationItem CreateNavItem(string title, string viewName, string iconKind, string group = "业务") =>
        new()
        {
            Title = title,
            ViewName = viewName,
            IconKind = iconKind,
            Command = new RelayCommand(() => _navigationCoordinator.NavigateTo(viewName)),
            Group = group
        };
}
