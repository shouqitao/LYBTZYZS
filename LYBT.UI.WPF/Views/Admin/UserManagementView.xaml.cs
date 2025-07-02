using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace LYBT.UI.WPF.Views.Admin {
    /// <summary>
    /// Interaction logic for UserManagementView.xaml
    /// </summary>
    public partial class UserManagementView : UserControl {
        public UserManagementView() {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) {
            if (DataContext is ViewModels.Admin.UserManagementViewModel vm) {
                var pb = (PasswordBox)sender;
                if (pb.Password != vm.Password)
                    vm.Password = pb.Password;
            }
        }
    }
}
