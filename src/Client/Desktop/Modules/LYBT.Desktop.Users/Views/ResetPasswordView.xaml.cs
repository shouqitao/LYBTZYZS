using System.Windows.Controls;
using LYBT.Desktop.Users.ViewModels;

namespace LYBT.Desktop.Users.Views
{
    /// <summary>
    /// ResetPasswordView.xaml 的交互逻辑
    /// </summary>
    public partial class ResetPasswordView : UserControl
    {
        public ResetPasswordView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 新密码变更事件处理
        /// </summary>
        private void NewPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ResetPasswordViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.NewPassword = passwordBox.Password;
            }
        }

        /// <summary>
        /// 确认密码变更事件处理
        /// </summary>
        private void ConfirmPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ResetPasswordViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.ConfirmPassword = passwordBox.Password;
            }
        }
    }
}
