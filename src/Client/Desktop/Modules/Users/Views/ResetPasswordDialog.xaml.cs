using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Users.Views
{

    /// <summary>
    /// ResetPasswordDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ResetPasswordDialog : Window
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
