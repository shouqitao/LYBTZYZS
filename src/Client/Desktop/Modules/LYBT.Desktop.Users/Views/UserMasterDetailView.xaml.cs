using System.Windows.Controls;

namespace LYBT.Desktop.Users.Views
{
    /// <summary>
    /// 用户Master-Detail视图
    /// OpenSpec: refactor-master-detail-layout
    ///
    /// 合并UserManagementView和UserDetailView为单一视图
    /// </summary>
    public partial class UserMasterDetailView : UserControl
    {
        public UserMasterDetailView()
        {
            InitializeComponent();
        }
    }
}
