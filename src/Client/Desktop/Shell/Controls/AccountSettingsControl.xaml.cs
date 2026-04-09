using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Shell.ViewModels;

namespace LYBT.Desktop.Shell.Controls
{
    /// <summary>
    /// 账户设置控件 - 合并个人资料和修改密码功能
    /// OpenSpec: migrate-views-to-role-modules - 从Users模块迁移到Shell
    /// </summary>
    public partial class AccountSettingsControl : UserControl
    {
        private bool _isSyncingPassword;

        public AccountSettingsControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            OldPasswordBox.PasswordChanged += OnOldPasswordChanged;
            NewPasswordBox.PasswordChanged += OnNewPasswordChanged;
            ConfirmPasswordBox.PasswordChanged += OnConfirmPasswordChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is AccountSettingsViewModel vm)
            {
                _isSyncingPassword = true;
                OldPasswordBox.Password = vm.OldPassword;
                NewPasswordBox.Password = vm.NewPassword;
                ConfirmPasswordBox.Password = vm.ConfirmPassword;
                _isSyncingPassword = false;
            }
        }

        private void OnOldPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingPassword) return;
            if (DataContext is AccountSettingsViewModel vm)
            {
                vm.OldPassword = OldPasswordBox.Password;
            }
        }

        private void OnNewPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingPassword) return;
            if (DataContext is AccountSettingsViewModel vm)
            {
                vm.NewPassword = NewPasswordBox.Password;
            }
        }

        private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingPassword) return;
            if (DataContext is AccountSettingsViewModel vm)
            {
                vm.ConfirmPassword = ConfirmPasswordBox.Password;
            }
        }
    }
}
