using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Auth.ViewModels;

namespace LYBT.Desktop.Auth.Views
{
    public partial class LoginView : UserControl
    {
        private bool _isSyncingPassword;

        public LoginView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            PasswordBox.PasswordChanged += OnPasswordChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is LoginViewModel vm && !string.IsNullOrEmpty(vm.Password))
            {
                _isSyncingPassword = true;
                PasswordBox.Password = vm.Password;
                _isSyncingPassword = false;
            }
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingPassword) return;
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = PasswordBox.Password;
            }
        }
    }
}
