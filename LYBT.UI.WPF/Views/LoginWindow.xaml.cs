using System.Windows;
using LYBT.UI.WPF.ViewModels;

namespace LYBT.UI.WPF.Views {
    /// <summary>
    /// Interaction logic for LoginWindow
    /// </summary>
    public partial class LoginWindow : Window {
        public LoginWindow() {
            InitializeComponent();

            // subscribe to DataContext changes on the inner LoginView
            LoginViewControl.DataContextChanged += LoginView_DataContextChanged;
            if (LoginViewControl.DataContext is LoginViewModel vm) {
                vm.LoginSucceeded += OnLoginSucceeded;
            }
        }

        private void LoginView_DataContextChanged(object? sender, DependencyPropertyChangedEventArgs e) {
            if (e.NewValue is LoginViewModel vm) {
                vm.LoginSucceeded += OnLoginSucceeded;
            }
        }

        private void OnLoginSucceeded() {
            DialogResult = true;
        }
    }
}
