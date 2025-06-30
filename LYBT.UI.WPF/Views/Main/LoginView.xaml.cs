using System.Windows.Controls;

namespace LYBT.UI.WPF.Views.Main {
    public partial class LoginView : UserControl {
        public LoginView() {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e) {
            if (DataContext is LYBT.UI.WPF.ViewModels.Main.LoginViewModel vm) {
                vm.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}
