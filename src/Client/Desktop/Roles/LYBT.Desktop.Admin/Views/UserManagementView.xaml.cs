using System.Windows.Controls;

namespace LYBT.Desktop.Admin.Views
{
    /// <summary>
    /// 用户管理视图
    /// OpenSpec: refactor-admin-workspace
    ///
    /// 薄包装View，复用业务模块的UserMasterDetailControl
    /// View在角色台，Control在业务模块
    /// </summary>
    public partial class UserManagementView : UserControl
    {
        public UserManagementView()
        {
            InitializeComponent();
        }
    }
}
