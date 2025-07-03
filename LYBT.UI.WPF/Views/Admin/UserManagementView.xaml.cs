using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.Specialized;
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
            if (e.OldValue is ViewModels.Admin.UserManagementViewModel oldVm) {
                oldVm.PropertyChanged -= Vm_PropertyChanged;
                oldVm.RoleList.CollectionChanged -= RoleList_CollectionChanged;
            }
            if (e.NewValue is ViewModels.Admin.UserManagementViewModel newVm) {
                newVm.PropertyChanged += Vm_PropertyChanged;
                newVm.RoleList.CollectionChanged += RoleList_CollectionChanged;
            }
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (sender is ViewModels.Admin.UserManagementViewModel vm) {
                if (e.PropertyName == nameof(vm.EditingUser)) {
                    UpdateRoleSelections(vm);
                }
            }
        }

        private void RoleList_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (DataContext is ViewModels.Admin.UserManagementViewModel vm) {
                UpdateRoleSelections(vm);
            }
        }

        private void UpdateRoleSelections(ViewModels.Admin.UserManagementViewModel vm) {
            RolesListBox.SelectedItems.Clear();
            if (vm.EditingUser != null) {
                foreach (var r in vm.EditingUser.Roles) {
                    var item = RolesListBox.Items.Cast<UserRole>().FirstOrDefault(x => x == r);
                    if (item != null)
                        RolesListBox.SelectedItems.Add(item);
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
