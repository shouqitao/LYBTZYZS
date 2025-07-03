using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LYBT.Common.Enums.Users;
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
            DataContextChanged += UserManagementView_DataContextChanged;
        }

        private void UserManagementView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (e.OldValue is ViewModels.Admin.UserManagementViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is ViewModels.Admin.UserManagementViewModel newVm)
                newVm.PropertyChanged += Vm_PropertyChanged;
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (sender is ViewModels.Admin.UserManagementViewModel vm) {
                if (e.PropertyName == nameof(vm.EditingUser)) {
                    if (vm.EditingUser != null) {
                        RolesListBox.SelectedItems.Clear();
                        foreach (var r in vm.EditingUser.Roles)
                            RolesListBox.SelectedItems.Add(r);
                    }
                }
            }
        }


        private void RolesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (DataContext is ViewModels.Admin.UserManagementViewModel vm && vm.EditingUser != null) {
                vm.EditingUser.Roles = RolesListBox.SelectedItems.Cast<UserRole>().ToList();
            }
        }
    }
}
