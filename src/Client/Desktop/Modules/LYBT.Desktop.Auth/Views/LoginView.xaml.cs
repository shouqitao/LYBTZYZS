using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Auth.ViewModels;

namespace LYBT.Desktop.Auth.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void OnClearUsernameClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Username = string.Empty;
            }
        }
    }
}
