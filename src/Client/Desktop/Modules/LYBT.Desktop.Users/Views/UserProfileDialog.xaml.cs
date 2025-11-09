using LYBT.Desktop.Users.ViewModels;
using System.Windows.Controls;

namespace LYBT.Desktop.Users.Views
{
    /// <summary>
    /// UserProfileDialog.xaml 的交互逻辑
    /// Issue #1887-1892: 独立的个人资料编辑对话框（密码修改已拆分）
    /// </summary>
    [Obsolete("此Dialog已废弃，请使用 UserProfileView 替代。Epic #1926 Sprint 4。", true)]
    public partial class UserProfileDialog : UserControl
    {
        public UserProfileDialog()
        {
            InitializeComponent();
        }
    }
}
