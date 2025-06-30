using LYBT.Common.Enums.Users;
using LYBT.Module.Users.Dtos;
using System.Collections.Generic;
using System.Windows;

namespace LYBT.UI.WPF.Views {
    public partial class UserEditWindow : Window {
        private readonly UserDto? _origin;
        public UserCreateDto? CreatedUser { get; private set; }
        public UserEditDto? EditedUser { get; private set; }

        public UserEditWindow(IEnumerable<UserRole> roles, UserDto? user = null) {
            InitializeComponent();
            RoleComboBox.ItemsSource = roles;
            _origin = user;
            if (user != null) {
                Title = "编辑用户";
                UserNameTextBox.Text = user.UserName;
                UserNameTextBox.IsEnabled = false;
                RealNameTextBox.Text = user.RealName;
                RoleComboBox.SelectedItem = user.Role;
                EmailTextBox.Text = user.Email ?? string.Empty;
                PhoneNumberTextBox.Text = user.PhoneNumber ?? string.Empty;
                IsActiveCheckBox.IsChecked = user.IsActive;
            } else {
                Title = "新增用户";
                RoleComboBox.SelectedIndex = 0;
                IsActiveCheckBox.IsChecked = true;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e) {
            if (_origin == null) {
                CreatedUser = new UserCreateDto {
                    UserName = UserNameTextBox.Text,
                    RealName = RealNameTextBox.Text,
                    Role = (UserRole)RoleComboBox.SelectedItem!,
                    IsActive = IsActiveCheckBox.IsChecked == true,
                    Email = EmailTextBox.Text,
                    PhoneNumber = PhoneNumberTextBox.Text,
                    Password = PasswordBox.Password
                };
            } else {
                EditedUser = new UserEditDto {
                    Id = _origin.Id,
                    RealName = RealNameTextBox.Text,
                    Role = (UserRole)RoleComboBox.SelectedItem!,
                    IsActive = IsActiveCheckBox.IsChecked == true,
                    Email = EmailTextBox.Text,
                    PhoneNumber = PhoneNumberTextBox.Text,
                    Password = string.IsNullOrWhiteSpace(PasswordBox.Password) ? null : PasswordBox.Password
                };
            }
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }
    }
}
