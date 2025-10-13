using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Users.ViewModels;

namespace LYBT.Desktop.Users.Views
{
    /// <summary>
    /// UserCreateView.xaml 的交互逻辑
    /// </summary>
    public partial class UserCreateView : UserControl
    {
        public UserCreateView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 密码框值变化时同步到 ViewModel
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserCreateViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.Password = passwordBox.Password;
            }
        }

        /// <summary>
        /// 确认密码框值变化时同步到 ViewModel
        /// </summary>
        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserCreateViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.ConfirmPassword = passwordBox.Password;
            }
        }
    }
}
