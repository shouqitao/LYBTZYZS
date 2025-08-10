using System.Windows;

namespace LYBT.WPF.Client.Modules.Auth.Views
{
    /// <summary>
    /// 修改密码对话框
    /// </summary>
    public partial class ChangePasswordDialog : Window
    {
        public ChangePasswordDialog()
        {
            InitializeComponent();
            
            // 绑定密码框的密码变化事件
            CurrentPasswordBox.PasswordChanged += OnCurrentPasswordChanged;
            NewPasswordBox.PasswordChanged += OnNewPasswordChanged;
            ConfirmPasswordBox.PasswordChanged += OnConfirmPasswordChanged;
        }

        private void OnCurrentPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null && sender is System.Windows.Controls.PasswordBox passwordBox)
            {
                var viewModel = DataContext as ViewModels.ChangePasswordDialogViewModel;
                viewModel?.CurrentPasswordChangedCommand?.Execute(passwordBox);
            }
        }

        private void OnNewPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null && sender is System.Windows.Controls.PasswordBox passwordBox)
            {
                var viewModel = DataContext as ViewModels.ChangePasswordDialogViewModel;
                viewModel?.NewPasswordChangedCommand?.Execute(passwordBox);
            }
        }

        private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null && sender is System.Windows.Controls.PasswordBox passwordBox)
            {
                var viewModel = DataContext as ViewModels.ChangePasswordDialogViewModel;
                viewModel?.ConfirmPasswordChangedCommand?.Execute(passwordBox);
            }
        }
    }
}