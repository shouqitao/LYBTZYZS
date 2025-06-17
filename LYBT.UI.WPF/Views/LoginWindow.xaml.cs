using System.Windows;
using LYBT.UI.WPF.ViewModels;

namespace LYBT.UI.WPF.Views {
    /// <summary>
    /// Interaction logic for LoginWindow
    /// </summary>
    public partial class LoginWindow : Window {
        public LoginWindow() {
            InitializeComponent();
            DataContextChanged += LoginWindow_DataContextChanged;
        }

        private void LoginWindow_DataContextChanged(object? sender, DependencyPropertyChangedEventArgs e) {
            if (e.NewValue is LoginViewModel vm) {
                vm.LoginSucceeded += OnLoginSucceeded;
            }
        }

        private void OnLoginSucceeded() {
            DialogResult = true;
        }
    }
}
