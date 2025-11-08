using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Users.ViewModels;

namespace LYBT.Desktop.Users.Views
{
    /// <summary>
    /// ChangePasswordDialog.xaml 的交互逻辑
    /// Issue #1887-1892: 独立的密码修改对话框
    /// </summary>
    public partial class ChangePasswordDialog : UserControl
    {
        public ChangePasswordDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 旧密码变更事件
        /// </summary>
        private void OldPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChangePasswordDialogViewModel viewModel)
            {
                viewModel.OldPassword = OldPasswordBox.Password;
            }
        }

        /// <summary>
        /// 新密码变更事件
        /// </summary>
        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChangePasswordDialogViewModel viewModel)
            {
                viewModel.NewPassword = NewPasswordBox.Password;
            }
        }

        /// <summary>
        /// 确认密码变更事件
        /// </summary>
        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChangePasswordDialogViewModel viewModel)
            {
                viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
            }
        }
    }
}
