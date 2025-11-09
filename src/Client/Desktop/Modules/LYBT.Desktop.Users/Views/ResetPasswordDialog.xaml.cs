using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Users.Views
{

    /// <summary>
    /// ResetPasswordDialog.xaml 的交互逻辑
    /// </summary>
    [Obsolete("此Dialog已废弃，重置密码功能已迁移到列表直接操作。Epic #1926 Sprint 4。", true)]
    public partial class ResetPasswordDialog : UserControl
    {

        public ResetPasswordDialog()
        {
            InitializeComponent();
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                var passwordBox = sender as PasswordBox;
                if (passwordBox == NewPasswordBox)
                {
                    (DataContext as dynamic).NewPassword = passwordBox.Password;
                }
                else if (passwordBox == ConfirmPasswordBox)
                {
                    (DataContext as dynamic).ConfirmPassword = passwordBox.Password;
                }
            }
        }
    }
}
