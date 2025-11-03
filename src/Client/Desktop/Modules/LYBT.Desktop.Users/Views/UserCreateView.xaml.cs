using System.Windows.Controls;

namespace LYBT.Desktop.Users.Views
{
    /// <summary>
    /// UserCreateView.xaml 的交互逻辑
    /// Issue #1261: 移除密码框事件处理方法，新用户使用系统默认密码
    /// </summary>
    public partial class UserCreateView : UserControl
    {
        public UserCreateView()
        {
            InitializeComponent();
        }
    }
}
