using System.Windows.Controls;
using LYBT.Shared.Models.Enums;
using Prism.Regions;

namespace LYBT.Desktop.Sysadmin.Views;

/// <summary>
/// 管理员账号管理视图 - 复用 Users 模块的 UserMasterDetailControl
/// </summary>
public partial class AdminUserManagementView : UserControl, INavigationAware
{
    public AdminUserManagementView()
    {
        InitializeComponent();
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        if (navigationContext.Parameters.ContainsKey("DefaultRoleFilter"))
        {
            var role = (UserRole)navigationContext.Parameters["DefaultRoleFilter"];
            UserMasterDetailControl.SetDefaultRoleFilter(role);
        }
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
