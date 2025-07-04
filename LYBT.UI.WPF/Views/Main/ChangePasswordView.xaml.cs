using System.Windows;
using System.Windows.Controls;

namespace LYBT.UI.WPF.Views.Main {
    public partial class ChangePasswordView : UserControl {
        public ChangePasswordView() {
            InitializeComponent();
        }

        private void OldPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            if (DataContext is ViewModels.Main.ChangePasswordViewModel vm) {
                vm.OldPassword = oldPasswordBox.Password;
            }
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            if (DataContext is ViewModels.Main.ChangePasswordViewModel vm) {
                vm.NewPassword = newPasswordBox.Password;
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            if (DataContext is ViewModels.Main.ChangePasswordViewModel vm) {
                vm.ConfirmPassword = confirmPasswordBox.Password;
            }
        }
    }
}
