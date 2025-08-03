using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.WPF.Client.Controls.Auth
{
    /// <summary>
    /// LoginStatusControl.xaml 的交互逻辑
    /// 登录状态控件
    /// </summary>
    public partial class LoginStatusControl : UserControl
    {
        public static readonly DependencyProperty UserProperty =
            DependencyProperty.Register(
                nameof(User),
                typeof(LoginResponseDto),
                typeof(LoginStatusControl),
                new PropertyMetadata(null));

        public LoginResponseDto User
        {
            get => (LoginResponseDto)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        public LoginStatusControl()
        {
            InitializeComponent();
        }
    }
}