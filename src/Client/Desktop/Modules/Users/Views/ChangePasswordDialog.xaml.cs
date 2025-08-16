using LYBT.Shared.Models.Contracts.Common;
using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Users.Views
{
    /// <summary>
    /// ChangePasswordDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ChangePasswordDialog : Window
    {
        public ChangePasswordDialog()
        {
            InitializeComponent();
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                var passwordBox = sender as PasswordBox;
                if (passwordBox == CurrentPasswordBox)
                {
                    (DataContext as dynamic).CurrentPassword = passwordBox.Password;
                }
                else if (passwordBox == NewPasswordBox)
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