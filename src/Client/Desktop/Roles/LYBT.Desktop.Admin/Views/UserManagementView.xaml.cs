using System.Windows.Controls;
using LYBT.Shared.Models.Enums;
using Prism.Regions;

namespace LYBT.Desktop.Admin.Views
{
    /// <summary>
    /// 用户管理视图
    ///
    /// 薄包装View，复用业务模块的UserMasterDetailControl
    /// 支持 DefaultRoleFilter 导航参数
    /// </summary>
    public partial class UserManagementView : UserControl, INavigationAware
    {
        public UserManagementView()
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
}
