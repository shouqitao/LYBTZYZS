using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Core;

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
                typeof(BaseUser),
                typeof(UserListItemControl),
                new PropertyMetadata(null));

        public BaseUser Data
        {
            get => (BaseUser)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public UserListItemControl()
        {
            InitializeComponent();
        }
    }
}