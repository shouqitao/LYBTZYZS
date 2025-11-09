using System.Windows.Controls;

namespace LYBT.Desktop.Users.Views
{
    /// <summary>
    /// UserFormDialog.xaml 的交互逻辑
    /// Issue #1798: 合并用户创建和编辑界面
    /// </summary>
    [Obsolete("此Dialog已废弃，请使用 UserCreateView/UserEditView 替代。Epic #1926 Sprint 4。", true)]
    public partial class UserFormDialog : UserControl
    {
        public UserFormDialog()
        {
            InitializeComponent();
        }
    }
}
