using System.Windows.Controls;
using LYBT.Desktop.Users.ViewModels;

namespace LYBT.Desktop.Users.Views
{
    /// <summary>
    /// ChangePasswordView.xaml 的交互逻辑
    /// Issue #1929 (Sprint 3): 修改密码页面
    /// </summary>
    public partial class ChangePasswordView : UserControl
    {
        public ChangePasswordView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 当前密码变更事件处理
        /// </summary>
        private void OldPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ChangePasswordViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.OldPassword = passwordBox.Password;
            }
        }

        /// <summary>
        /// 新密码变更事件处理
        /// </summary>
        private void NewPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ChangePasswordViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.NewPassword = passwordBox.Password;
            }
        }

        /// <summary>
        /// 确认密码变更事件处理
        /// </summary>
        private void ConfirmPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ChangePasswordViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.ConfirmPassword = passwordBox.Password;
            }
        }
    }
}
