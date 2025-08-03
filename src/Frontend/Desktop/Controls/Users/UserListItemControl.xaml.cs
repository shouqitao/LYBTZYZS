using LYBT.Shared.Models.Contracts.Users;
using LYBT.WPF.Client.Controls.Base;

namespace LYBT.WPF.Client.Controls.Users
{
    /// <summary>
    /// UserListItemControl.xaml 的交互逻辑
    /// 用户列表项控件
    /// </summary>
    public partial class UserListItemControl : BaseDisplayControl<UserDto>
    {
        public UserListItemControl()
        {
            InitializeComponent();
        }
    }
}