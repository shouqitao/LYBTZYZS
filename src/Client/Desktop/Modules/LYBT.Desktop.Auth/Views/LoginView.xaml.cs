using System.Windows.Controls;

namespace LYBT.Desktop.Auth.Views
{
    /// <summary>
    /// LoginView.xaml 的交互逻辑 - 架构重构后简化版本
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is Auth.ViewModels.LoginViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.Password = passwordBox.Password;
            }
        }
    }
}
