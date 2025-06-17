using System.Windows.Controls;
using System.Windows;
using LYBT.UI.WPF.ViewModels;

namespace LYBT.UI.WPF.Views {
    /// <summary>
    /// Interaction logic for LoginView
    /// </summary>
    public partial class LoginView : UserControl {
        public LoginView() {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            if (DataContext is LoginViewModel vm && sender is PasswordBox pb) {
                vm.Password = pb.Password;
            }
        }
    }
}
