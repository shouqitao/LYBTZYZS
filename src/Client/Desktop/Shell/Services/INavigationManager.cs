namespace LYBT.Desktop.Shell.Services;

using System.Collections.ObjectModel;
using LYBT.Desktop.Controls.Models;
using LYBT.Shared.Models.Enums;

/// <summary>
/// 导航管理器接口 — 管理侧边栏导航项
/// </summary>
public interface INavigationManager
{
    ObservableCollection<NavigationItem> NavigationItems { get; set; }
    NavigationItem? SelectedNavItem { get; set; }
    ObservableCollection<NavigationItem> BuildNavigationItems(UserRole role);
}
