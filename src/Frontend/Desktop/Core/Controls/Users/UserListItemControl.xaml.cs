using System.Windows;
using System.Windows.Controls;
using LYBT.WPF.Client.Core.Models.Users;

namespace LYBT.WPF.Client.Controls.Users
{
    /// <summary>
    /// UserListItemControl.xaml 的交互逻辑
    /// 用户列表项控件
    /// </summary>
    public partial class UserListItemControl : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(UserInfo),
                typeof(UserListItemControl),
                new PropertyMetadata(null));

        public UserInfo Data
        {
            get => (UserInfo)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public UserListItemControl()
        {
            InitializeComponent();
        }
    }
}