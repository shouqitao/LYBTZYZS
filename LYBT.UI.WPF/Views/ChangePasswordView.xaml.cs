using LYBT.UI.WPF.Services;
using LYBT.UI.WPF.ViewModels.Main;
using System;
using System.Windows;
using System.Windows.Controls;

namespace LYBT.UI.WPF.Views {
    public partial class ChangePasswordView : UserControl {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        public ChangePasswordView(IUserService userService, IAuthService authService) {
            InitializeComponent();
            _userService = userService;
            _authService = authService;
        }

        private void OldPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            if (DataContext is ChangePasswordViewModel vm) {
                vm.OldPassword = OldPasswordBox.Password;
            }
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            if (DataContext is ChangePasswordViewModel vm) {
                vm.NewPassword = NewPasswordBox.Password;
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            if (DataContext is ChangePasswordViewModel vm) {
                vm.ConfirmPassword = ConfirmPasswordBox.Password;
            }
        }

        private async void Ok_Click(object sender, RoutedEventArgs e) {
            if (DataContext is not ChangePasswordViewModel vm) return;
            if (vm.NewPassword != vm.ConfirmPassword) {
                MessageBox.Show("两次输入的新密码不一致！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var userId = _authService.CurrentUserId;
            if (userId == Guid.Empty) {
                MessageBox.Show("无法获取当前用户信息！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var ok = await _userService.ChangePasswordAsync(userId, vm.OldPassword, vm.NewPassword);
            if (!ok) {
                MessageBox.Show("修改密码失败！", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            MessageBox.Show("密码修改成功，请使用新密码重新登录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            if (Application.Current.MainWindow.DataContext is MainWindowViewModel mainVm) {
                mainVm.Logout();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            if (Application.Current.MainWindow.DataContext is MainWindowViewModel mainVm) {
                mainVm.ShowHomeView();
            }
        }
    }
}
