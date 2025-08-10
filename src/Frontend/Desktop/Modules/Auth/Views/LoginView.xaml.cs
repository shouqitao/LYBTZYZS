using System.Windows;
using System.Windows.Controls;

namespace LYBT.WPF.Client.Modules.Auth.Views
{
    /// <summary>
    /// 登录视图
    /// </summary>
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            
            // 绑定密码框的密码变化事件
            PasswordBox.PasswordChanged += OnPasswordChanged;
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null && sender is PasswordBox passwordBox)
            {
                // 通过命令传递密码框对象到ViewModel
                var viewModel = DataContext as ViewModels.LoginViewModel;
                viewModel?.PasswordChangedCommand?.Execute(passwordBox);
            }
        }
    }
}