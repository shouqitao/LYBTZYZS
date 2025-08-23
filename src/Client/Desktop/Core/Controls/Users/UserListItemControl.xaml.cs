using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using System.Windows;
using System.Windows.Controls;

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
                typeof(UserDto),
                typeof(UserListItemControl),
                new PropertyMetadata(null));

        public UserDto Data
        {
            get => (UserDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public UserListItemControl()
        {
            InitializeComponent();
        }
    }
}