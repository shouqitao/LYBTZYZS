using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.UI.WPF.Views.Main {
    public partial class LoginView : UserControl {
        public LoginView() {
            InitializeComponent();

            // 自动填充密码
            this.Loaded += (s, e) => {
                if (DataContext is LYBT.UI.WPF.ViewModels.Main.LoginViewModel vm) {
                    if (!string.IsNullOrEmpty(vm.Password))
                        passwordBox.Password = vm.Password;
                }
                // 用户体验：默认聚焦用户名输入框
                userNameBox.Focus();
            };
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            if (DataContext is ViewModels.Main.LoginViewModel vm) {
                if (passwordBox.Password != vm.Password)
                    vm.Password = passwordBox.Password;
            }
        }

        // 支持在TextBox、PasswordBox按下回车即登录
        private void UserControl_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                if (DataContext is ViewModels.Main.LoginViewModel vm) {
                    if (vm.LoginCommand.CanExecute())
                        vm.LoginCommand.Execute();
                }
            }
        }
    }
}
