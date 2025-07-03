using LYBT.Common.Enums.Users;
using LYBT.Module.Users.Dtos;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LYBT.UI.WPF.Views {
    /// <summary>
    /// 类 UserEditWindow 的说明
    /// </summary>
    public partial class UserEditWindow : Window {
        private readonly UserDto? _origin;
        /// <summary>
        /// 属性 CreatedUser 的说明
        /// </summary>
        public UserCreateDto? CreatedUser { get; private set; }
        /// <summary>
        /// 属性 EditedUser 的说明
        /// </summary>
        public UserEditDto? EditedUser { get; private set; }

        public UserEditWindow(IEnumerable<UserRole> roles, UserDto? user = null) {
            InitializeComponent();
            RoleListBox.ItemsSource = roles;
            _origin = user;
            if (user != null) {
                Title = "编辑用户";
                UserNameTextBox.Text = user.UserName;
                UserNameTextBox.IsEnabled = false;
                RealNameTextBox.Text = user.RealName;
                foreach (var r in user.Roles)
                    RoleListBox.SelectedItems.Add(r);
                EmailTextBox.Text = user.Email ?? string.Empty;
                PhoneNumberTextBox.Text = user.PhoneNumber ?? string.Empty;
                IsActiveCheckBox.IsChecked = user.IsActive;
            } else {
                Title = "新增用户";
                if (roles.Any())
                    RoleListBox.SelectedItems.Add(roles.First());
                IsActiveCheckBox.IsChecked = true;
            }
        }

        /// <summary>
        /// 方法 Ok_Click 的说明
        /// </summary>
        private void Ok_Click(object sender, RoutedEventArgs e) {
            var selectedRoles = RoleListBox.SelectedItems.Cast<UserRole>().ToList();
            if (_origin == null) {
                CreatedUser = new UserCreateDto {
                    UserName = UserNameTextBox.Text,
                    RealName = RealNameTextBox.Text,
                    Roles = selectedRoles,
                    IsActive = IsActiveCheckBox.IsChecked == true,
                    Email = EmailTextBox.Text,
                    PhoneNumber = PhoneNumberTextBox.Text
                };
            } else {
                EditedUser = new UserEditDto {
                    Id = _origin.Id,
                    RealName = RealNameTextBox.Text,
                    Roles = selectedRoles,
                    IsActive = IsActiveCheckBox.IsChecked == true,
                    Email = EmailTextBox.Text,
                    PhoneNumber = PhoneNumberTextBox.Text,
                    Password = string.IsNullOrWhiteSpace(PasswordBox.Password) ? null : PasswordBox.Password
                };
            }
            DialogResult = true;
        }

        /// <summary>
        /// 方法 Cancel_Click 的说明
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }
    }
}
