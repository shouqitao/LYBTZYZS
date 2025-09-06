using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Auth.ViewModels;

namespace LYBT.Desktop.Auth.Views {

    /// <summary>
    /// LoginView.xaml 的交互逻辑
    /// </summary>
    public partial class LoginView : UserControl {
        private bool _isPasswordSavedFromViewModel = false;

        public LoginView() {
            InitializeComponent();

            // 当控件加载完成后设置密码
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            if (DataContext is LoginViewModel viewModel) {
                // 监听ViewModel的密码属性变化
                viewModel.PropertyChanged += ViewModel_PropertyChanged;

                // 初始设置密码
                if (!string.IsNullOrEmpty(viewModel.Password)) {
                    _isPasswordSavedFromViewModel = true;
                    PasswordBox.Password = viewModel.Password;
                }
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(LoginViewModel.Password) && DataContext is LoginViewModel viewModel) {
                if (!string.IsNullOrEmpty(viewModel.Password) && PasswordBox.Password != viewModel.Password) {
                    _isPasswordSavedFromViewModel = true;
                    PasswordBox.Password = viewModel.Password;
                }
            }
        }

        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e) {
            if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox) {
                // 如果密码是从ViewModel加载的，不要再更新回去
                if (_isPasswordSavedFromViewModel) {
                    _isPasswordSavedFromViewModel = false;
                    return;
                }

                // 防止循环更新
                if (viewModel.Password != passwordBox.Password) {
                    viewModel.Password = passwordBox.Password;
                }
            }
        }
    }
}
