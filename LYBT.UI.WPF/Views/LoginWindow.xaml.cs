using System.Windows;
using LYBT.UI.WPF.ViewModels;

namespace LYBT.UI.WPF.Views {
    /// <summary>
    /// Interaction logic for LoginWindow
    /// </summary>
    public partial class LoginWindow : Window {
        public LoginWindow() {
            DataContextChanged += LoginWindow_DataContextChanged;
            InitializeComponent();

            // AutoWireViewModel sets DataContext inside InitializeComponent,
            // so subscribe to LoginSucceeded if it has already been set
            if (DataContext is LoginViewModel vm) {
                vm.LoginSucceeded += OnLoginSucceeded;
            }
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
